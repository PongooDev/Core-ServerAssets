using System.Collections.Concurrent;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Versions;

namespace ServerAssetsExtractor;

/// <summary>A provider over exactly the pak files given (so Core's own installed pak is never read).</summary>
public sealed class PakListProvider(IEnumerable<FileInfo> paks, VersionContainer versions)
    : AbstractVfsFileProvider(versions, StringComparer.OrdinalIgnoreCase)
{
    private readonly List<FileInfo> _paks = paks.ToList();

    public override void Initialize()
    {
        foreach (var pak in _paks) RegisterVfs(pak);
    }
}

public sealed record NavExport(string ExportName, string Class, long SerialSize);

public sealed class PackageInfo
{
    public required string Path;          // pak path without extension, e.g. FortniteGame/Content/Athena/Maps/Athena_Terrain
    public required GameFile Main;        // the .uasset or .umap
    public GameFile? Uexp;
    public List<GameFile> Bulk = [];      // .ubulk / .uptnl
    public string Class = "";
    public string? Error;
    public bool IsMap => Main.Extension.Equals("umap", StringComparison.OrdinalIgnoreCase);
    public List<NavExport> Nav = [];
    /// <summary>The class of every export in the package (levels and Blueprints hold many).</summary>
    public HashSet<string> ExportClasses = new(StringComparer.OrdinalIgnoreCase);
    public bool Keep;
    public string Why = "";
    public int GameplayExports;           // exports of gameplay classes (for levels)
    public string? GameplayExample;       // one of them, for the report
    public string? HlodImport;            // the first imported package path that marks the package as HLOD content

    /// <summary>A navmesh actor (RecastNavMesh or a subclass such as FortNavMesh); its export holds the tile data.</summary>
    public bool HasNavMesh => Nav.Any(n => IsNavMeshClass(n.Class) && !n.ExportName.StartsWith("Default__"));

    public static bool IsNavMeshClass(string cls) =>
        cls.Contains("RecastNavMesh", StringComparison.OrdinalIgnoreCase)
        || cls.EndsWith("NavMesh", StringComparison.OrdinalIgnoreCase)
        || cls.EndsWith("NavMesh_C", StringComparison.OrdinalIgnoreCase);

    public long HeaderSize => Main.Size + (Uexp?.Size ?? 0);
    public long BulkSize => Bulk.Sum(b => b.Size);

    /// <summary>/Game/... style name, as in manifest.json's contents.</summary>
    public string GamePath => ToGamePath(Path);

    public static string ToGamePath(string pakPath)
    {
        var i = pakPath.IndexOf("/Content/", StringComparison.OrdinalIgnoreCase);
        if (i < 0) return "/" + pakPath;
        var root = pakPath[..i];
        var rest = pakPath[(i + "/Content/".Length)..];
        if (root.Equals("Engine", StringComparison.OrdinalIgnoreCase)) return "/Engine/" + rest;
        if (root.EndsWith("Game", StringComparison.OrdinalIgnoreCase) && !root.Contains('/')) return "/Game/" + rest;
        // Plugins: .../Plugins/<...>/<Name>/Content/x -> /<Name>/x
        var name = root[(root.LastIndexOf('/') + 1)..];
        return "/" + name + "/" + rest;
    }
}

public static class Scanner
{
    /// <summary>Groups the mounted files into packages and reads each package's header to find its main class.</summary>
    public static List<PackageInfo> Scan(AbstractVfsFileProvider provider, Rules rules, Action<int, int> progress)
    {
        var byPath = new Dictionary<string, PackageInfo>(StringComparer.OrdinalIgnoreCase);
        var payloads = new List<GameFile>();
        foreach (var file in provider.Files.Values)
        {
            if (file.IsUePackage)
            {
                byPath[file.PathWithoutExtension] = new PackageInfo { Path = file.PathWithoutExtension, Main = file };
            }
            else if (file.IsUePackagePayload)
            {
                payloads.Add(file);
            }
        }

        foreach (var p in payloads)
        {
            if (!byPath.TryGetValue(p.PathWithoutExtension, out var pkg)) continue;
            if (p.Extension.Equals("uexp", StringComparison.OrdinalIgnoreCase)) pkg.Uexp = p;
            else pkg.Bulk.Add(p);
        }

        var list = byPath.Values.OrderBy(p => p.Path, StringComparer.OrdinalIgnoreCase).ToList();
        var done = 0;
        Parallel.ForEach(list, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) }, pkg =>
        {
            Classify(provider, rules, pkg);
            var n = Interlocked.Increment(ref done);
            if (n % 500 == 0 || n == list.Count) progress(n, list.Count);
        });
        return list;
    }

    private static void Classify(AbstractVfsFileProvider provider, Rules rules, PackageInfo pkg)
    {
        try
        {
            // Only the .uasset/.umap header is read: the export table says each export's class. Exports themselves
            // (and the .uexp) are not loaded.
            var package = new Package(pkg.Main.CreateReader(), null, (CUE4Parse.UE4.Readers.FArchive?) null, null, provider, true);
            var name = pkg.Main.NameWithoutExtension;
            var exports = package.ExportMap;

            CUE4Parse.UE4.Objects.UObject.FObjectExport? main =
                exports.FirstOrDefault(e => e.ObjectName.Text.Equals(name, StringComparison.OrdinalIgnoreCase))
                ?? exports.FirstOrDefault(e => e.ObjectName.Text.Equals(name + "_C", StringComparison.OrdinalIgnoreCase))
                ?? exports.FirstOrDefault(e => e.IsAsset)
                ?? exports.FirstOrDefault(e => (e.OuterIndex == null || e.OuterIndex.IsNull) && !e.ObjectName.Text.StartsWith("Default__"))
                ?? exports.FirstOrDefault();
            pkg.Class = main == null ? "<no exports>" : ClassOf(package, main);

            foreach (var e in exports)
            {
                var cls = ClassOf(package, e);
                pkg.ExportClasses.Add(cls);
                if (rules.IsNavigationClass(cls)) pkg.Nav.Add(new NavExport(e.ObjectName.Text, cls, e.SerialSize));
                if (pkg.IsMap && rules.IsGameplayLevelClass(cls)) { pkg.GameplayExports++; pkg.GameplayExample ??= cls; }
            }

            // Imported packages (outermost imports) that mark this one as HLOD content.
            foreach (var imp in package.ImportMap)
            {
                if (imp.OuterIndex is { IsNull: false }) continue;
                if (rules.ImportExclusion(imp.ObjectName.Text) is { } hit) { pkg.HlodImport = imp.ObjectName.Text + " (" + hit + ")"; break; }
            }
        }
        catch (Exception ex)
        {
            pkg.Class = "<unreadable>";
            pkg.Error = ex.GetType().Name + ": " + ex.Message;
        }
    }

    /// <summary>The export's class name, read straight from the import/export tables (no other package is loaded).</summary>
    private static string ClassOf(Package package, CUE4Parse.UE4.Objects.UObject.FObjectExport e)
    {
        var idx = e.ClassIndex;
        if (idx == null || idx.IsNull) return "Class";
        if (idx.IsImport && -idx.Index - 1 < package.ImportMap.Length) return package.ImportMap[-idx.Index - 1].ObjectName.Text;
        if (idx.IsExport && idx.Index - 1 < package.ExportMap.Length) return package.ExportMap[idx.Index - 1].ObjectName.Text;
        return "<unknown>";
    }
}