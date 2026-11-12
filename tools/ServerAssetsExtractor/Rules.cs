using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ServerAssetsExtractor;

/// <summary>The include/exclude lists from rules.json.</summary>
public sealed class Rules
{
    /// <summary>The rules' own version, printed in report.txt; 3 since platform-cooked exports and bulk data are refused.</summary>
    public int Version { get; set; }
    public List<string> IncludeClasses { get; set; } = [];
    public List<string> ExcludeClasses { get; set; } = [];
    /// <summary>Classes cooked per platform (textures, meshes, audio, materials, lighting data): a package with an export of one is left out.</summary>
    public List<string> ExcludeExportClasses { get; set; } = [];
    /// <summary>true: a package whose data is partly in a .ubulk/.uptnl (texture mips, audio, mesh data) is left out.</summary>
    public bool ExcludeBulkData { get; set; } = true;
    public string UnmatchedClasses { get; set; } = "include";
    public List<string> AlwaysIncludePaths { get; set; } = [];
    public List<string> ExcludePaths { get; set; } = [];
    public List<string> PlatformNameTokens { get; set; } = [];
    public List<string> PlatformFolderNames { get; set; } = [];
    public List<string> PlatformRuleExemptClasses { get; set; } = [];
    public List<string> ExcludeIfImportPathContains { get; set; } = [];
    public List<string> GameplayLevelClasses { get; set; } = [];
    public List<string> NotGameplayLevelClasses { get; set; } = [];
    public List<string> ClientOnlyLevelPaths { get; set; } = [];
    public List<string> NavigationClasses { get; set; } = [];
    public List<string> LooseFiles { get; set; } = [];
    public List<string> ExcludeLooseFiles { get; set; } = [];
    /// <summary>false (the default): maps are left out, except KeepLevelPaths (and levels with a navmesh, with KeepNavMeshLevels).</summary>
    public bool IncludeMaps { get; set; }
    public List<string> KeepLevelPaths { get; set; } = [];
    /// <summary>true: with maps left out, a level that holds a navmesh is kept anyway (rules before version 3 did this).</summary>
    public bool KeepNavMeshLevels { get; set; }

    private Matcher _include = null!, _exclude = null!, _excludeExport = null!, _always = null!, _excludePaths = null!, _nav = null!, _gameplay = null!, _loose = null!, _excludeLoose = null!, _platformExempt = null!, _notGameplay = null!, _clientLevels = null!, _keepLevels = null!;
    private Regex? _platformToken, _platformFolder;

    public string Source { get; private set; } = "";

    public static Rules Load(string? path)
    {
        string json, source;
        if (path != null)
        {
            json = File.ReadAllText(path);
            source = Path.GetFullPath(path);
        }
        else
        {
            var nextToExe = Path.Combine(AppContext.BaseDirectory, "rules.json");
            if (File.Exists(nextToExe))
            {
                json = File.ReadAllText(nextToExe);
                source = nextToExe;
            }
            else
            {
                using var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("rules.json")
                              ?? throw new InvalidOperationException("the built-in rules.json is missing");
                using var r = new StreamReader(s);
                json = r.ReadToEnd();
                source = "built-in rules.json";
            }
        }

        var rules = JsonSerializer.Deserialize<Rules>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        }) ?? throw new InvalidOperationException("rules.json is empty");
        rules.Source = source;
        if (rules.UnmatchedClasses is not ("include" or "exclude"))
            throw new InvalidOperationException("rules.json: unmatchedClasses must be \"include\" or \"exclude\"");
        rules._include = new Matcher(rules.IncludeClasses);
        rules._exclude = new Matcher(rules.ExcludeClasses);
        rules._excludeExport = new Matcher(rules.ExcludeExportClasses);
        rules._always = new Matcher(rules.AlwaysIncludePaths);
        rules._excludePaths = new Matcher(rules.ExcludePaths);
        rules._nav = new Matcher(rules.NavigationClasses);
        rules._gameplay = new Matcher(rules.GameplayLevelClasses);
        rules._loose = new Matcher(rules.LooseFiles);
        rules._excludeLoose = new Matcher(rules.ExcludeLooseFiles);
        rules._platformExempt = new Matcher(rules.PlatformRuleExemptClasses);
        rules._notGameplay = new Matcher(rules.NotGameplayLevelClasses);
        rules._clientLevels = new Matcher(rules.ClientOnlyLevelPaths);
        rules._keepLevels = new Matcher(rules.KeepLevelPaths);

        // A platform name counts only as a whole word of a path: between / _ - . or the ends.
        var tokens = rules.PlatformNameTokens.Where(t => t.Length > 0).Select(Regex.Escape).ToList();
        if (tokens.Count > 0)
            rules._platformToken = new Regex($@"(^|[/_\-.])({string.Join("|", tokens)})([/_\-.]|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        var folders = rules.PlatformFolderNames.Where(t => t.Length > 0).Select(Regex.Escape).ToList();
        if (folders.Count > 0)
            rules._platformFolder = new Regex($@"/({string.Join("|", folders)})/", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        return rules;
    }

    /// <summary>The keepLevelPaths pattern (matched against the /Game/... name) that keeps this level, or null.</summary>
    public string? KeptLevelPattern(string gamePath) => _keepLevels.Match(gamePath);

    public bool IsNavigationClass(string cls) => _nav.Match(cls) != null;
    public bool IsGameplayLevelClass(string cls) => _notGameplay.Match(cls) == null && _gameplay.Match(cls) != null;
    public bool IsUnmatched(string cls) => _exclude.Match(cls) == null && _include.Match(cls) == null;

    /// <summary>The excludeExportClasses pattern a class matches, or null.</summary>
    public string? PlatformCookedClass(string cls) => _excludeExport.Match(cls);

    /// <summary>
    /// Why a package holds platform-cooked data, or null: an export (the main one or any other) of an
    /// excludeExportClasses class, or data in a .ubulk/.uptnl. Nothing else can keep such a package.
    /// </summary>
    public string? PlatformCooked(PackageInfo pkg)
    {
        if (_excludeExport.Match(pkg.Class) is { } m) return "excludeExportClasses " + m;
        foreach (var cls in pkg.ExportClasses.OrderBy(c => c, StringComparer.OrdinalIgnoreCase))
            if (_excludeExport.Match(cls) is { } x) return $"excludeExportClasses {x} (a {cls} export inside)";
        if (ExcludeBulkData && pkg.Bulk.Count > 0) return "excludeBulkData (" + string.Join(", ", pkg.Bulk.Select(b => "." + b.Extension.ToLowerInvariant()).Distinct()) + ")";
        return null;
    }

    /// <summary>The path rule (exclude path or platform name) that leaves this path out, or null.</summary>
    public string? PathExclusion(string path, string? cls = null)
    {
        if (_excludePaths.Match(path) is { } p) return "excludePaths " + p;
        // A data asset with a platform in its name (an Xbox-exclusive hero, say) is still gameplay data.
        if (cls != null && _platformExempt.Match(cls) != null) return null;
        if (_platformToken?.Match(path) is { Success: true } m) return "platformNameTokens " + m.Groups[2].Value;
        if (_platformFolder?.Match(path) is { Success: true } f) return "platformFolderNames " + f.Groups[1].Value;
        return null;
    }

    /// <summary>Decides a loose (non-package) file such as an .ini.</summary>
    public (bool keep, string why) DecideLoose(string path)
    {
        if (_loose.Match(path) is not { } p) return (false, "not in looseFiles");
        if (_excludeLoose.Match(path) is { } x) return (false, "excludeLooseFiles " + x);
        if (PathExclusion(path) is { } ex) return (false, ex);
        return (true, "looseFiles " + p);
    }

    /// <summary>Decides one package. The reason names the rule and the pattern that matched.</summary>
    public (bool keep, string why) Decide(PackageInfo pkg)
    {
        // Platform-cooked rendering data never goes in: the pak would replace a PC player's own copy with the
        // console's. Leaving it out keeps the PC copy, which is what the server and the player both use then.
        if (PlatformCooked(pkg) is { } cooked) return (false, cooked);

        if (_always.Match(pkg.Path) is { } a) return (true, "alwaysIncludePaths " + a);

        if (PathExclusion(pkg.Path, pkg.Class) is { } ex)
        {
            // A level that holds the navmesh or gameplay actors is kept; only render-only levels (HLOD sublevels
            // and the like) are left out.
            // Client-only levels (the frontend and lobby) go even with gameplay-looking actors in them.
            var clientOnly = _clientLevels.Match(pkg.Path) != null;
            if (pkg.IsMap && (pkg.HasNavMesh || (!clientOnly && pkg.GameplayExports > 0))) return (true, $"kept level with {(pkg.HasNavMesh ? "a navmesh" : pkg.GameplayExports + " gameplay actors, e.g. " + pkg.GameplayExample)} despite {ex}");
            return (false, ex);
        }

        if (_exclude.Match(pkg.Class) is { } c) return (false, "excludeClasses " + c);

        if (!pkg.IsMap && pkg.HlodImport is { } imp) return (false, "excludeIfImportPathContains " + imp);

        if (_include.Match(pkg.Class) is { } i) return (true, "includeClasses " + i);
        return (UnmatchedClasses == "include", "unmatchedClasses " + UnmatchedClasses);
    }

    public string? ImportExclusion(string importPath) =>
        ExcludeIfImportPathContains.FirstOrDefault(s => importPath.Contains(s, StringComparison.OrdinalIgnoreCase));

    private sealed class Matcher
    {
        private readonly (string pattern, Regex re)[] _list;

        public Matcher(IEnumerable<string> patterns)
        {
            _list = patterns.Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => (p.Trim(), new Regex("^" + Regex.Escape(p.Trim()).Replace("\\*", ".*") + "$",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled)))
                .ToArray();
        }

        /// <summary>The first pattern that matches, or null.</summary>
        public string? Match(string s)
        {
            foreach (var (pattern, re) in _list)
                if (re.IsMatch(s)) return pattern;
            return null;
        }
    }
}
