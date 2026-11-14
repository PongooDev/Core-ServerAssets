using System.Text.Json;
using System.Text.RegularExpressions;
using CUE4Parse.UE4.Versions;

namespace ServerAssetsExtractor;

/// <summary>Finds a build's paks, changelist and engine version.</summary>
public static class BuildInfo
{
    /// <summary>The builds Core supports or takes paks for: changelist, Fortnite version, engine version.</summary>
    public static readonly (int cl, string version, EGame ue)[] KnownBuilds =
    [
        (3790078, "1.10", EGame.GAME_UE4_19),
        (3807424, "1.11", EGame.GAME_UE4_19),
        (3825894, "2.1", EGame.GAME_UE4_19),
        (3841827, "2.2", EGame.GAME_UE4_19),
        (3847564, "2.3", EGame.GAME_UE4_19),
        (3858292, "2.4", EGame.GAME_UE4_19),
        (3870737, "2.4.2", EGame.GAME_UE4_19),
        (3889387, "2.5", EGame.GAME_UE4_20),
        (3901517, "3.0", EGame.GAME_UE4_20),
        (3915963, "3.1.0", EGame.GAME_UE4_20),
        (3917250, "3.1.1", EGame.GAME_UE4_20),
        (3935073, "3.2", EGame.GAME_UE4_20),
        (3942182, "3.3", EGame.GAME_UE4_20),
        (4000805, "3.5.1", EGame.GAME_UE4_20),
        (4008490, "3.5.2", EGame.GAME_UE4_20),
        (4019403, "3.6", EGame.GAME_UE4_20),
        (4039451, "4.0", EGame.GAME_UE4_20),
        (4053532, "4.1", EGame.GAME_UE4_20),
        (4072250, "4.2", EGame.GAME_UE4_20),
        (4117433, "4.4.0", EGame.GAME_UE4_20),
        (4127312, "4.4.1", EGame.GAME_UE4_20),
        (4132537, "4.4.2", EGame.GAME_UE4_20),
        (4159770, "4.5", EGame.GAME_UE4_20),
        (4166199, "4.5", EGame.GAME_UE4_20),
        (4204761, "5.00", EGame.GAME_UE4_21),
        (4214610, "5.0.1", EGame.GAME_UE4_21),
        (4225813, "5.10", EGame.GAME_UE4_21),
        (4240749, "5.10", EGame.GAME_UE4_21),
        (4288479, "5.21", EGame.GAME_UE4_21),
        (4305896, "5.30", EGame.GAME_UE4_21),
        (4352937, "5.40", EGame.GAME_UE4_21),
        (4363240, "5.41", EGame.GAME_UE4_21),
        (4395664, "6.00", EGame.GAME_UE4_21),
        (4402180, "6.00", EGame.GAME_UE4_21),
        (4417689, "6.01", EGame.GAME_UE4_21),
        (4442095, "6.02", EGame.GAME_UE4_21),
        (4464155, "6.10", EGame.GAME_UE4_21),
        (4476098, "6.10.1", EGame.GAME_UE4_21),
        (4497486, "6.20", EGame.GAME_UE4_21),
        (4526925, "6.21", EGame.GAME_UE4_21),
        (4543176, "6.22", EGame.GAME_UE4_21),
        (4573096, "6.30", EGame.GAME_UE4_21),
        (4573279, "6.31", EGame.GAME_UE4_21),
        (4629139, "7.00", EGame.GAME_UE4_21),
        (4667333, "7.10", EGame.GAME_UE4_22),
        (4727874, "7.20", EGame.GAME_UE4_22),
        (4834550, "7.30", EGame.GAME_UE4_22),
        (5046157, "7.40", EGame.GAME_UE4_22),
        (5203069, "8.00", EGame.GAME_UE4_22),
        (5625478, "8.20", EGame.GAME_UE4_23),
        (5793395, "8.30", EGame.GAME_UE4_23),
        (6005771, "8.40", EGame.GAME_UE4_23),
        (6058028, "8.50", EGame.GAME_UE4_23),
        (6165369, "8.51", EGame.GAME_UE4_23),
    ];

    public static string? VersionOf(int cl) => KnownBuilds.FirstOrDefault(b => b.cl == cl).version;

    /// <summary>
    /// The known changelists of a Fortnite version as a build's VersionDisplayName writes it: as written, else without
    /// a last ".0" (7.00.0 is 7.00). Several where a version had more than one build.
    /// </summary>
    public static List<int> ChangelistsOf(string shown)
    {
        var exact = KnownBuilds.Where(b => b.version == shown).Select(b => b.cl).ToList();
        if (exact.Count > 0 || !shown.EndsWith(".0")) return exact;
        var trimmed = shown[..^2];
        return KnownBuilds.Where(b => b.version == trimmed).Select(b => b.cl).ToList();
    }

    /// <summary>The engine version a changelist was built with, or null if it can't be told.</summary>
    public static EGame? UeOf(int cl)
    {
        var known = KnownBuilds.FirstOrDefault(b => b.cl == cl);
        if (known.cl != 0) return known.ue;
        if (cl >= 3700000 && cl < 3889387) return EGame.GAME_UE4_19; // 1.7.2 .. 2.4.2
        if (cl >= 3889387 && cl < 4200000) return EGame.GAME_UE4_20; // 2.5 .. 4.x
        if (cl >= 4200000 && cl < 4667333) return EGame.GAME_UE4_21; // 5.x .. 7.00
        if (cl >= 4667333 && cl <= 5203069) return EGame.GAME_UE4_22; // 7.10 .. 8.00
        if (cl > 5203069 && cl <= 6165369) return EGame.GAME_UE4_23; // 8.20 .. 8.51
        return null;
    }

    public static EGame ParseUe(string s)
    {
        var m = Regex.Match(s.Trim(), @"^(?:GAME_)?(?:UE)?(\d)[._]?(\d{1,2})$", RegexOptions.IgnoreCase);
        if (!m.Success || !Enum.TryParse<EGame>($"GAME_UE{m.Groups[1].Value}_{m.Groups[2].Value}", out var g))
            throw new ArgumentException($"unknown engine version \"{s}\" (use e.g. 4.19, 4.20, 4.21, 4.22 or 4.23)");
        return g;
    }

    public static string UeName(EGame g) => g.ToString().Replace("GAME_UE", "UE ").Replace('_', '.');

    /// <summary>The game's pak files under a Paks folder or a build root. Core's own pak is left out.</summary>
    public static List<FileInfo> FindPaks(string root, out string paksDir)
    {
        var dir = new DirectoryInfo(root);
        if (!dir.Exists) throw new DirectoryNotFoundException($"the folder \"{root}\" does not exist");

        IEnumerable<FileInfo> Paks(DirectoryInfo d) => d.EnumerateFiles("*.pak", SearchOption.TopDirectoryOnly)
            .Where(f => !f.Name.StartsWith("pakchunkCoreServerAssets", StringComparison.OrdinalIgnoreCase));

        var direct = Paks(dir).ToList();
        if (direct.Count > 0)
        {
            paksDir = dir.FullName;
            return direct;
        }

        // The build root: look for a folder holding paks, preferring <Game>/Content/Paks.
        var candidates = new List<DirectoryInfo>();
        try
        {
            candidates = dir.EnumerateDirectories("*", new EnumerationOptions
                {
                    RecurseSubdirectories = true, MaxRecursionDepth = 6, IgnoreInaccessible = true,
                    AttributesToSkip = FileAttributes.ReparsePoint,
                })
                .Where(d => Paks(d).Any())
                .OrderByDescending(d => d.Name.Equals("Paks", StringComparison.OrdinalIgnoreCase))
                .ThenBy(d => d.FullName.Length)
                .ToList();
        }
        catch (IOException) { }

        if (candidates.Count == 0)
            throw new FileNotFoundException($"no .pak files were found in \"{root}\" or its subfolders");
        paksDir = candidates[0].FullName;
        return Paks(candidates[0]).ToList();
    }

    /// <summary>A changelist from an on-disk Engine/Build/Build.version, or from a CL-nnnnnnn folder name.</summary>
    public static int? FindChangelistOnDisk(string root, string paksDir)
    {
        for (var d = new DirectoryInfo(paksDir); d != null; d = d.Parent)
        {
            var bv = Path.Combine(d.FullName, "Engine", "Build", "Build.version");
            if (File.Exists(bv) && ParseBuildVersion(File.ReadAllText(bv)) is { } cl) return cl;
            if (d.FullName.Length <= Path.GetPathRoot(d.FullName)!.Length) break;
        }

        foreach (var path in new[] { paksDir, root })
        {
            var m = Regex.Match(path, @"CL[-_ ]?(\d{7})", RegexOptions.IgnoreCase);
            if (m.Success) return int.Parse(m.Groups[1].Value);
        }
        return null;
    }

    public static int? ParseBuildVersion(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("Changelist", out var c) && c.TryGetInt32(out var cl) && cl > 0) return cl;
        }
        catch (JsonException) { }
        return null;
    }
}
