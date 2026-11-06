using System.Diagnostics;
using System.Globalization;
using System.Text;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Versions;

namespace ServerAssetsExtractor;

public static partial class Program
{
    private const string Usage = """
        ServerAssetsExtractor - makes a Core server-assets pak of every data asset in a Fortnite build.

        Easiest: run it with no arguments (or drop the build folder on the exe) and answer the questions.

        Usage:
          ServerAssetsExtractor [<build folder or Paks folder>] [options]
          ServerAssetsExtractor verify <pak> [--ue 4.20]
          ServerAssetsExtractor add <existing.pak> <files or folders...> [--out new.pak] [--force] [--ue 4.20]
                                add loose .uasset/.uexp/.umap/.ubulk files to a copy of a pak (a folder laid out
                                as FortniteGame/Content/..., like FModel's Export Raw Data, or <file>=<pak path>)

        Options:
          --out <folder>        where the pak, sig, contents.txt and report.txt go
                                (default: a new ServerAssets-<version>-CL-<cl> folder in the current folder,
                                or next to the exe when it is started without options)
          --ue <version>        engine version: 4.19 (Fortnite 1.7.2-2.4.2) or 4.20 (2.5-4.x). Found by itself
                                from the changelist or FModel's settings when left out
          --aes <hex>           the build's AES key. By default it is read from FModel's settings
                                (%APPDATA%\FModel\AppSettings.json). The key is never printed or saved
          --cl <changelist>     the build's changelist, if it can't be found (it names the output files)
          --include <prefix>    only pak paths starting with this, e.g. FortniteGame/Content/ (repeatable)
          --exclude <prefix>    leave out pak paths starting with this (repeatable)
          --max-size <size>     leave out any package bigger than this, e.g. 200MB
          --include-maps        keep every map too (by default maps are left out, except the levels in
                                rules.json's keepLevelPaths and levels that hold a navmesh)
          --max-map-size <size> leave out maps (.umap) bigger than this, unless they hold navigation data
          --rules <file>        a rules.json other than the one next to the tool
          --dry-run             only list and count; writes nothing unless --out is given (then only the lists)
          --yes                 don't ask anything (fail instead)
          -h, --help            this text
        """;

    private static bool _interactive;

    public static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        _interactive = args.Length == 0 || (args.Length == 1 && !args[0].StartsWith('-'));
        try
        {
            if (args.Length > 0 && args[0] is "-h" or "--help" or "/?")
            {
                Console.WriteLine(Usage);
                return 0;
            }
            if (args.Length > 0 && args[0].Equals("verify", StringComparison.OrdinalIgnoreCase))
                return RunVerify(args[1..]);
            if (args.Length > 0 && args[0].Equals("add", StringComparison.OrdinalIgnoreCase))
                return RunAdd(args[1..]);
            var code = RunExtract(args);
            PauseIfInteractive();
            return code;
        }
        catch (Exception e) when (e is UserError or DirectoryNotFoundException or FileNotFoundException)
        {
            Error(e.Message);
            PauseIfInteractive();
            return 2;
        }
        catch (Exception e)
        {
            Error("Something went wrong: " + e.Message);
            Console.Error.WriteLine(e);
            PauseIfInteractive();
            return 1;
        }
    }

    private sealed class UserError(string message) : Exception(message);

    private sealed class Options
    {
        public string? Path, Out, Aes, Rules;
        public EGame? Ue;
        public int? Cl;
        public List<string> Include = [], Exclude = [];
        public long? MaxSize, MaxMapSize;
        public bool DryRun, Yes, IncludeMaps;
    }

    private static Options ParseArgs(string[] args)
    {
        var o = new Options();
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            string Next() => i + 1 < args.Length ? args[++i] : throw new UserError($"{a} needs a value");
            switch (a)
            {
                case "--out": o.Out = Next(); break;
                case "--ue": o.Ue = ParseUeOrThrow(Next()); break;
                case "--aes": o.Aes = Next(); break;
                case "--cl": o.Cl = int.TryParse(Next(), out var cl) ? cl : throw new UserError("--cl takes a number"); break;
                case "--include": o.Include.Add(NormPrefix(Next())); break;
                case "--exclude": o.Exclude.Add(NormPrefix(Next())); break;
                case "--max-size": o.MaxSize = ParseSize(Next()); break;
                case "--include-maps": o.IncludeMaps = true; break;
                case "--max-map-size": o.MaxMapSize = ParseSize(Next()); break;
                case "--rules": o.Rules = Next(); break;
                case "--dry-run": o.DryRun = true; break;
                case "--yes": case "-y": o.Yes = true; break;
                default:
                    if (a.StartsWith('-')) throw new UserError($"unknown option {a} (see --help)");
                    if (o.Path != null) throw new UserError($"only one build folder can be given (got \"{o.Path}\" and \"{a}\")");
                    o.Path = a;
                    break;
            }
        }
        return o;
    }

    private static int RunExtract(string[] args)
    {
        var o = ParseArgs(args);
        if (o.Yes) _interactive = false;
        var rules = Rules.Load(o.Rules);

        Header("ServerAssetsExtractor " + typeof(Program).Assembly.GetName().Version?.ToString(3));

        // 1. The build.
        var root = o.Path ?? Ask("Drag the build's folder (the one with FortniteGame in it) into this window, then press Enter:");
        root = Path.GetFullPath(root.Trim().Trim('\uFEFF', '"', ' '));
        var paks = BuildInfo.FindPaks(root, out var paksDir);
        Info($"Paks folder: {paksDir} ({paks.Count} paks, {Size(paks.Sum(p => p.Length))})");

        // 2. Changelist, keys and engine version.
        var cl = o.Cl ?? BuildInfo.FindChangelistOnDisk(root, paksDir);
        KeySet? keys = null;
        if (o.Aes != null)
        {
            keys = new KeySet { MainKey = ParseKeyOrThrow(o.Aes), Origin = "--aes" };
        }
        else
        {
            keys = Keys.FromFModel(root, paksDir);
        }

        var ueFrom = "";
        EGame ue;
        if (o.Ue is { } forced) { ue = forced; ueFrom = "--ue"; }
        else if (keys?.FModelUe is { } fm && fm.ToString().StartsWith("GAME_UE")) { ue = fm; ueFrom = "FModel's settings"; }
        else if (cl is { } c && BuildInfo.UeOf(c) is { } byCl) { ue = byCl; ueFrom = $"changelist {c}"; }
        else { ue = EGame.GAME_UE4_20; ueFrom = "default (pass --ue if this is wrong)"; }

        var provider = Mount(paks, ue, keys, o);

        // The pak's own Build.version can name the changelist, and with it the right engine version.
        if (provider.Files.TryGetValue("Engine/Build/Build.version", out var bvFile) && BuildInfo.ParseBuildVersion(Encoding.UTF8.GetString(bvFile.Read())) is { } paksCl)
        {
            if (cl != null && cl != paksCl && o.Cl == null) Warn($"The folder name says CL-{cl}, the paks' Build.version says CL-{paksCl}: using CL-{paksCl}.");
            cl ??= paksCl;
            if (o.Cl == null) cl = paksCl;
            if (o.Ue == null && ueFrom.StartsWith("default") && BuildInfo.UeOf(paksCl) is { } ue2 && ue2 != ue)
            {
                ue = ue2;
                ueFrom = $"changelist {paksCl}";
                provider.Dispose();
                provider = Mount(paks, ue, keys, o);
            }
        }

        var version = cl is { } k ? BuildInfo.VersionOf(k) : null;
        Info($"Build: {(version != null ? "Fortnite " + version + ", " : "")}{(cl != null ? "CL-" + cl : "changelist unknown (pass --cl)")}; engine {BuildInfo.UeName(ue)} (from {ueFrom})");
        if (cl != null && version == null) Warn($"CL-{cl} isn't one of the builds Core supports. A pak is only used for an exact changelist match.");
        Info($"Rules: {rules.Source}");

        // 3. Scan.
        Header("Reading every package's header");
        var sw = Stopwatch.StartNew();
        var all = Scanner.Scan(provider, rules, (n, total) => Progress($"  {n:N0} / {total:N0} packages"));
        EndProgress();
        Info($"{all.Count:N0} packages read in {sw.Elapsed.TotalSeconds:N0} s");

        var inScope = all.Where(p => InScope(p.Path, o)).ToList();
        foreach (var p in inScope)
        {
            (p.Keep, p.Why) = rules.Decide(p);
            if (p.Class == "<unreadable>" && rules.PathExclusion(p.Path, p.Class) == null) (p.Keep, p.Why) = (true, "unreadable header, kept to be safe");
            var size = p.HeaderSize;
            if (p.Keep && o.MaxMapSize is { } mm && p.IsMap && size > mm && !p.HasNavMesh) (p.Keep, p.Why) = (false, "--max-map-size");
            if (p.Keep && o.MaxSize is { } ms && size > ms) (p.Keep, p.Why) = (false, "--max-size");
        }
        if (!o.IncludeMaps && !rules.IncludeMaps)
        {
            // Maps are left out: a server loads them from the game's own paks. Kept are the levels known to hold
            // server-only data (keepLevelPaths) and every level with a navmesh, with its _BuiltData.
            foreach (var p in inScope.Where(p => p.Keep && p.IsMap))
            {
                if (rules.KeptLevelPattern(p.GamePath) is { } kl) p.Why = "keepLevelPaths " + kl;
                else if (p.HasNavMesh) p.Why = "level with a navmesh";
                else (p.Keep, p.Why) = (false, "maps left out by default");
            }
            var keptLevels = inScope.Where(p => p.Keep && p.IsMap).Select(p => p.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var p in inScope.Where(p => p.Keep && !p.IsMap && p.Path.EndsWith("_BuiltData", StringComparison.OrdinalIgnoreCase)))
                if (!keptLevels.Contains(p.Path[..^"_BuiltData".Length]))
                    (p.Keep, p.Why) = (false, "maps left out by default (_BuiltData of a left-out level)");
        }
        var kept = inScope.Where(p => p.Keep).ToList();

        var looseDecisions = provider.Files.Values
            .Where(f => !f.IsUePackage && !f.IsUePackagePayload && InScope(f.Path, o))
            .Select(f => (file: f, d: rules.DecideLoose(f.Path)))
            .ToList();
        var loose = looseDecisions.Where(x => x.d.keep).Select(x => x.file).OrderBy(f => f.Path, StringComparer.OrdinalIgnoreCase).ToList();
        var looseSkipped = looseDecisions.Where(x => !x.d.keep).GroupBy(x => x.d.why)
            .Select(g => (why: g.Key, count: g.Count(), size: g.Sum(x => x.file.Size), example: g.First().file.Path)).ToList();

        // 4. Report.
        var report = BuildReport(paksDir, cl, version, ue, rules, o, all, inScope, kept, loose, looseSkipped);
        Console.WriteLine();
        Console.WriteLine(report.summary);

        var contents = kept.Select(p => p.GamePath).Concat(loose.Select(f => f.Path)).OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList();

        if (o.DryRun)
        {
            if (o.Out != null)
            {
                var outDir = PrepareOut(o.Out, root, allowExisting: true);
                File.WriteAllLines(Path.Combine(outDir, "contents.txt"), contents);
                File.WriteAllText(Path.Combine(outDir, "report.txt"), report.full);
                Info($"Dry run: the lists are in {outDir} (no pak was written).");
            }
            else Info("Dry run: nothing was written. Add --out <folder> to save the lists.");
            provider.Dispose();
            return 0;
        }

        // 5. Write.
        var baseName = "ServerAssets" + (version != null ? "-" + version : "") + (cl != null ? "-CL-" + cl : "");
        // Run by double-click or drag-and-drop, the current folder can be anywhere (even System32): use the exe's folder.
        var defaultParent = _interactive ? AppContext.BaseDirectory : Environment.CurrentDirectory;
        var outPath = PrepareOut(o.Out ?? Path.Combine(defaultParent, baseName), root, allowExisting: false);
        var pakPath = Path.Combine(outPath, baseName + ".pak");
        var sigPath = Path.Combine(outPath, baseName + ".sig");

        Header($"Writing {pakPath}");
        var files = new List<CUE4Parse.FileProvider.Objects.GameFile>();
        foreach (var p in kept)
        {
            files.Add(p.Main);
            if (p.Uexp != null) files.Add(p.Uexp);

        }
        files.AddRange(loose);
        files = files.OrderBy(f => f.Path, StringComparer.OrdinalIgnoreCase).ToList();
        var totalBytes = files.Sum(f => f.Size);
        long written = 0;
        sw.Restart();
        using (var w = new PakWriter(pakPath))
        {
            foreach (var f in files)
            {
                w.Add(f.Path, f.Read());
                written += f.Size;
                Progress($"  {Size(written)} / {Size(totalBytes)} ({(totalBytes == 0 ? 100 : written * 100 / totalBytes)}%)");
            }
            w.Finish();
        }
        EndProgress();
        PakWriter.WriteSig(pakPath, sigPath);
        File.WriteAllLines(Path.Combine(outPath, "contents.txt"), contents);
        File.WriteAllText(Path.Combine(outPath, "report.txt"), report.full);
        provider.Dispose();
        Info($"{files.Count:N0} files, {Size(new FileInfo(pakPath).Length)}, in {sw.Elapsed.TotalSeconds:N0} s");

        // 6. Check what was written.
        Header("Checking the pak");
        var ok = Verifier.Verify(pakPath, ue, Console.WriteLine);
        Console.WriteLine();
        if (!ok)
        {
            Error("The pak failed a check (see above). Don't submit it; open an issue with report.txt instead.");
            return 3;
        }

        Header("Done");
        Console.WriteLine($"""
              Made in {outPath}:
                {Path.GetFileName(pakPath)}   the pak
                {Path.GetFileName(sigPath)}   its signature file
                contents.txt   every package in it
                report.txt     what was kept and left out, and why

              To submit it, upload the pak and sig somewhere (they're too big for GitHub issues) and open a
              "Server assets pak" issue at https://github.com/PongooDev/Core-ServerAssets/issues/new/choose
              with the links and contents.txt and report.txt attached.
            """);
        return 0;
    }

    private static int RunVerify(string[] args)
    {
        string? pak = null;
        EGame ue = EGame.GAME_UE4_20;
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--ue" && i + 1 < args.Length) ue = ParseUeOrThrow(args[++i]);
            else if (!args[i].StartsWith('-')) pak = args[i];
            else throw new UserError($"unknown option {args[i]}");
        }
        if (pak == null) throw new UserError("verify needs a pak file");
        Header($"Checking {pak}");
        var ok = Verifier.Verify(Path.GetFullPath(pak), ue, Console.WriteLine);
        Console.WriteLine(ok ? "All checks passed." : "Some checks FAILED.");
        return ok ? 0 : 3;
    }

    /// <summary>Mounts the paks with the keys there are, trying FModel's other keys and then asking when needed.</summary>
    private static PakListProvider Mount(List<FileInfo> paks, EGame ue, KeySet? keys, Options o)
    {
        var provider = new PakListProvider(paks, new VersionContainer(ue));
        provider.Initialize();
        provider.Mount(); // the unencrypted ones

        var needsMain = provider.RequiredKeys.Contains(new FGuid());
        var keyOrigin = "";
        if (needsMain)
        {
            if (keys?.MainKey is { } mk && provider.SubmitKey(new FGuid(), mk) > 0) keyOrigin = keys.Origin;
            else
            {
                if (keys?.MainKey != null) Warn($"The AES key from {keys.Origin} doesn't open these paks.");
                if (o.Aes == null)
                {
                    foreach (var (dir, key) in Keys.AllFModelMainKeys())
                    {
                        if (provider.SubmitKey(new FGuid(), key) > 0) { keyOrigin = "FModel's settings for " + dir + " (same key)"; break; }
                    }
                }
                while (keyOrigin == "" && _interactive)
                {
                    var typed = AskSecret("Paste the build's AES key (64 hex digits; it won't be shown or saved), or press Enter to stop:");
                    if (typed.Length == 0) break;
                    FAesKey key;
                    try { key = Keys.Parse(typed); }
                    catch (ArgumentException e) { Warn(e.Message); continue; }
                    if (provider.SubmitKey(new FGuid(), key) > 0) keyOrigin = "the key you typed";
                    else Warn("That key doesn't open the paks. Check it is for this exact version.");
                }
                if (keyOrigin == "")
                    throw new UserError("The paks are encrypted and no AES key opened them. Pass the build's key with --aes <hex>, " +
                                        "or open the build in FModel with its key first.");
            }
        }

        if (keys != null)
        {
            var dyn = keys.DynamicKeys.Where(d => provider.RequiredKeys.Contains(d.guid)).ToList();
            if (dyn.Count > 0) provider.SubmitKeys(dyn.Select(d => new KeyValuePair<FGuid, FAesKey>(d.guid, d.key)));
        }

        provider.PostMount();
        Info($"Mounted {provider.MountedVfs.Count} of {paks.Count} paks{(keyOrigin != "" ? ", AES key from " + keyOrigin : ", no AES key needed")}");
        foreach (var u in provider.UnloadedVfs)
            Warn($"Not mounted: {u.Name} (it needs an AES key that isn't known; its files are left out)");
        if (provider.MountedVfs.Count == 0)
            throw new UserError("No pak could be mounted. Check the engine version (--ue) and the AES key.");
        return provider;
    }

    private static bool InScope(string path, Options o)
    {
        if (o.Include.Count > 0 && !o.Include.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase))) return false;
        return !o.Exclude.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormPrefix(string p) => p.Replace('\\', '/').TrimStart('/');

    private static (string summary, string full) BuildReport(string paksDir, int? cl, string? version, EGame ue, Rules rules,
        Options o, List<PackageInfo> all, List<PackageInfo> inScope, List<PackageInfo> kept, List<CUE4Parse.FileProvider.Objects.GameFile> loose,
        List<(string why, int count, long size, string example)> looseSkipped)
    {
        var s = new StringBuilder();
        var keptBytes = kept.Sum(p => p.HeaderSize) + loose.Sum(f => f.Size);
        var skipped = inScope.Where(p => !p.Keep).ToList();

        s.AppendLine("Summary");
        s.AppendLine($"  packages in the build:   {all.Count,8:N0}");
        if (inScope.Count != all.Count) s.AppendLine($"  inside --include/--exclude: {inScope.Count,5:N0}");
        s.AppendLine($"  kept (data):             {kept.Count,8:N0}   {Size(kept.Sum(p => p.HeaderSize))}");
        s.AppendLine($"  left out:                {skipped.Count,8:N0}   {Size(skipped.Sum(p => p.HeaderSize + p.BulkSize))}");
        var mapSkips = skipped.Where(p => p.Why.StartsWith("maps left out by default")).ToList();
        if (!o.IncludeMaps && !rules.IncludeMaps)
        {
            s.AppendLine($"  maps left out by default:{mapSkips.Count,8:N0}   {Size(mapSkips.Sum(p => p.HeaderSize + p.BulkSize))}   (--include-maps keeps them)");
            var navKept = kept.Where(p => p.IsMap && p.HasNavMesh).ToList();
            s.AppendLine($"  levels with a navmesh, kept: {navKept.Count,5:N0}   {Size(navKept.Sum(p => p.HeaderSize))}");
        }
        s.AppendLine($"  config files (.ini):     {loose.Count,8:N0}   {Size(loose.Sum(f => f.Size))}");
        s.AppendLine($"  pak size (about):        {Size(keptBytes + (kept.Count * 2 + loose.Count) * 150L)}");
        var bulkSkipped = kept.Where(p => p.Bulk.Count > 0).ToList();
        if (bulkSkipped.Count > 0)
            s.AppendLine($"  bulk data (.ubulk/.uptnl) of kept packages, left out: {bulkSkipped.Count:N0} packages, {Size(bulkSkipped.Sum(p => p.BulkSize))}");
        var unreadable = inScope.Count(p => p.Class == "<unreadable>");
        if (unreadable > 0) s.AppendLine($"  unreadable headers (kept): {unreadable:N0}");

        s.AppendLine();
        s.AppendLine("Kept, by class (packages, size)");
        foreach (var g in kept.GroupBy(p => p.Class).OrderByDescending(g => g.Count()).ThenBy(g => g.Key))
            s.AppendLine($"  {g.Count(),7:N0}  {Size(g.Sum(p => p.HeaderSize)),10}  {g.Key}{(rules.IsUnmatched(g.Key) ? "   (not in any list)" : "")}");

        s.AppendLine();
        s.AppendLine("Left out, by class (packages, size)");
        foreach (var g in skipped.GroupBy(p => p.Class).OrderByDescending(g => g.Count()).ThenBy(g => g.Key))
            s.AppendLine($"  {g.Count(),7:N0}  {Size(g.Sum(p => p.HeaderSize + p.BulkSize)),10}  {g.Key}");

        s.AppendLine();
        s.AppendLine("Left out, by rule (packages, size, example)");
        foreach (var g in skipped.GroupBy(p => p.Why).OrderByDescending(g => g.Count()).ThenBy(g => g.Key))
            s.AppendLine($"  {g.Count(),7:N0}  {Size(g.Sum(p => p.HeaderSize + p.BulkSize)),10}  {g.Key}   e.g. {g.First().GamePath}");
        var rescued = kept.Where(p => p.Why.StartsWith("kept level")).ToList();
        if (rescued.Count > 0)
        {
            s.AppendLine($"  levels a path rule matched but kept for their gameplay/navigation data: {rescued.Count:N0}");
            foreach (var p in rescued.Take(40)) s.AppendLine($"           {p.GamePath} ({p.Why})");
        }
        s.AppendLine();
        s.AppendLine("Other files left out, by rule (files, size, example)");
        foreach (var g in looseSkipped.OrderByDescending(g => g.count))
            s.AppendLine($"  {g.count,7:N0}  {Size(g.size),10}  {g.why}   e.g. {g.example}");

        var nav = inScope.Where(p => p.Nav.Count > 0).ToList();
        var navMeshes = nav.SelectMany(p => p.Nav.Where(n => PackageInfo.IsNavMeshClass(n.Class) && !n.ExportName.StartsWith("Default__"))
            .Select(n => (p, n))).ToList();
        s.AppendLine();
        s.AppendLine("Navigation data");
        s.AppendLine($"  packages with navigation exports: {nav.Count:N0} (kept: {nav.Count(p => p.Keep):N0})");
        if (navMeshes.Count == 0) s.AppendLine("  no navmesh actor (RecastNavMesh or a subclass) in any level");
        else
        {
            s.AppendLine("  navmesh actors (the export's serialized size includes its tile data):");
            foreach (var (p, n) in navMeshes.OrderByDescending(x => x.n.SerialSize))
                s.AppendLine($"    {Size(n.SerialSize),10}  {n.Class} {n.ExportName} in {p.GamePath} ({(p.Keep ? "kept" : "left out: " + p.Why)}, package {Size(p.HeaderSize)})");
        }
        var builtData = inScope.Where(p => p.Path.EndsWith("_BuiltData", StringComparison.OrdinalIgnoreCase)).ToList();
        s.AppendLine($"  *_BuiltData packages: {builtData.Count:N0} ({Size(builtData.Sum(p => p.HeaderSize))}), kept: {builtData.Count(p => p.Keep):N0}");
        var summary = s.ToString();

        var f = new StringBuilder();
        f.AppendLine($"ServerAssetsExtractor {typeof(Program).Assembly.GetName().Version?.ToString(3)} report, {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");
        f.AppendLine($"Build: {(version != null ? "Fortnite " + version + ", " : "")}{(cl != null ? "CL-" + cl : "changelist unknown")}, {BuildInfo.UeName(ue)}");
        f.AppendLine($"Paks folder: {paksDir}");
        f.AppendLine($"Rules: {rules.Source}");
        if (o.Include.Count > 0) f.AppendLine($"--include {string.Join(" ", o.Include)}");
        if (o.Exclude.Count > 0) f.AppendLine($"--exclude {string.Join(" ", o.Exclude)}");
        if (o.MaxSize != null) f.AppendLine($"--max-size {Size(o.MaxSize.Value)}");
        if (o.IncludeMaps) f.AppendLine("--include-maps");
        if (o.MaxMapSize != null) f.AppendLine($"--max-map-size {Size(o.MaxMapSize.Value)}");
        f.AppendLine();
        f.Append(summary);

        var unmatched = kept.Where(p => rules.IsUnmatched(p.Class)).GroupBy(p => p.Class).OrderBy(g => g.Key).ToList();
        if (unmatched.Count > 0)
        {
            f.AppendLine();
            f.AppendLine("Classes in no list (kept because unmatchedClasses is \"include\"; add them to rules.json to sort them)");
            foreach (var g in unmatched) f.AppendLine($"  {g.Key}: {g.Count():N0}, e.g. {g.First().GamePath}");
        }

        f.AppendLine();
        f.AppendLine("All navigation exports");
        foreach (var p in nav.OrderBy(p => p.Path, StringComparer.OrdinalIgnoreCase))
            foreach (var n in p.Nav)
                f.AppendLine($"  {p.GamePath}\t{n.Class}\t{n.ExportName}\t{n.SerialSize}");

        var errors = inScope.Where(p => p.Error != null).ToList();
        if (errors.Count > 0)
        {
            f.AppendLine();
            f.AppendLine("Unreadable package headers");
            foreach (var p in errors) f.AppendLine($"  {p.Path}: {p.Error}");
        }

        f.AppendLine();
        f.AppendLine("Every package (kept/left out, class, size, reason)");
        foreach (var p in inScope)
            f.AppendLine($"  {(p.Keep ? "+" : "-")} {p.GamePath}\t{p.Class}\t{p.HeaderSize}\t{p.Why}");
        return (summary, f.ToString());
    }

    private static string PrepareOut(string outDir, string root, bool allowExisting)
    {
        var full = Path.GetFullPath(outDir.Trim().Trim('"'));
        var rootFull = Path.GetFullPath(root).TrimEnd('\\') + "\\";
        if ((full.TrimEnd('\\') + "\\").StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
            throw new UserError($"The output folder can't be inside the build ({full}); the build is never written to. Pick another --out.");
        if (Directory.Exists(full) && !allowExisting && Directory.EnumerateFileSystemEntries(full).Any())
            throw new UserError($"The output folder {full} already has files in it. Pick an empty or new folder (--out).");
        Directory.CreateDirectory(full);
        return full;
    }

    private static EGame ParseUeOrThrow(string s)
    {
        try { return BuildInfo.ParseUe(s); } catch (ArgumentException e) { throw new UserError(e.Message); }
    }

    private static FAesKey ParseKeyOrThrow(string s)
    {
        try { return Keys.Parse(s); } catch (ArgumentException e) { throw new UserError("--aes: " + e.Message); }
    }

    public static long ParseSize(string s)
    {
        s = s.Trim().ToUpperInvariant().Replace(" ", "");
        long mult = 1;
        foreach (var (suffix, m) in new[] { ("GB", 1L << 30), ("MB", 1L << 20), ("KB", 1L << 10), ("G", 1L << 30), ("M", 1L << 20), ("K", 1L << 10), ("B", 1L) })
        {
            if (s.EndsWith(suffix)) { mult = m; s = s[..^suffix.Length]; break; }
        }
        if (!double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) || v < 0)
            throw new UserError($"\"{s}\" isn't a size (use e.g. 200MB)");
        return (long) (v * mult);
    }

    public static string Size(long b) => b switch
    {
        >= 1L << 30 => $"{b / (double) (1L << 30):0.00} GB",
        >= 1L << 20 => $"{b / (double) (1L << 20):0.0} MB",
        >= 1L << 10 => $"{b / 1024.0:0.0} KB",
        _ => $"{b} B",
    };

    // Console helpers.
    private static bool _progressShown;

    private static void Header(string s)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("== " + s);
        Console.ResetColor();
    }

    private static void Info(string s) => Console.WriteLine(s);

    private static void Warn(string s)
    {
        EndProgress();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Warning: " + s);
        Console.ResetColor();
    }

    private static void Error(string s)
    {
        EndProgress();
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine("Error: " + s);
        Console.ResetColor();
    }

    private static readonly object ProgressLock = new();
    private static long _lastProgress;

    private static void Progress(string s)
    {
        lock (ProgressLock)
        {
            var now = Environment.TickCount64;
            if (now - _lastProgress < (Console.IsOutputRedirected ? 5000 : 250) && (_progressShown || Console.IsOutputRedirected)) return;
            _lastProgress = now;
            if (Console.IsOutputRedirected) { Console.WriteLine(s); return; }
            Console.Write("\r" + s.PadRight(Math.Max(0, Console.WindowWidth - 1)));
            _progressShown = true;
        }
    }

    private static void EndProgress()
    {
        lock (ProgressLock)
        {
            if (!_progressShown) return;
            Console.WriteLine();
            _progressShown = false;
        }
    }

    private static string Ask(string question)
    {
        if (!_interactive) throw new UserError("no build folder was given (see --help)");
        while (true)
        {
            Console.WriteLine(question);
            Console.Write("> ");
            var line = Console.ReadLine();
            if (line == null) throw new UserError("no build folder was given");
            if (line.Trim().Length > 0) return line;
        }
    }

    private static string AskSecret(string question)
    {
        Console.WriteLine(question);
        Console.Write("> ");
        var sb = new StringBuilder();
        if (Console.IsInputRedirected) return (Console.ReadLine() ?? "").Trim();
        while (true)
        {
            var k = Console.ReadKey(intercept: true);
            if (k.Key == ConsoleKey.Enter) break;
            if (k.Key == ConsoleKey.Backspace) { if (sb.Length > 0) sb.Length--; continue; }
            if (!char.IsControl(k.KeyChar)) sb.Append(k.KeyChar);
        }
        Console.WriteLine();
        return sb.ToString().Trim();
    }

    private static void PauseIfInteractive()
    {
        if (!_interactive || Console.IsInputRedirected) return;
        Console.WriteLine();
        Console.WriteLine("Press Enter to close.");
        Console.ReadLine();
    }
}
