using System.Text.Json;
using System.Text.RegularExpressions;
using CUE4Parse.UE4.Versions;

namespace ServerAssetsExtractor;

/// <summary>Finds a build's paks, changelist and engine version.</summary>
public static class BuildInfo
{
    /// <summary>The builds Core supports: changelist, Fortnite version, engine version.</summary>
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
    ];

    public static string? VersionOf(int cl) => KnownBuilds.FirstOrDefault(b => b.cl == cl).version;

    /// <summary>The engine version a changelist was built with, or null if it can't be told.</summary>
    public static EGame? UeOf(int cl)
    {
        var known = KnownBuilds.FirstOrDefault(b => b.cl == cl);
        if (known.cl != 0) return known.ue;
        if (cl >= 3700000 && cl < 3889387) return EGame.GAME_UE4_19; // 1.7.2 .. 2.4.2
        if (cl >= 3889387 && cl < 4200000) return EGame.GAME_UE4_20; // 2.5 .. 4.x
        return null;
    }

    public static EGame ParseUe(string s)
    {
        var m = Regex.Match(s.Trim(), @"^(?:GAME_)?(?:UE)?(\d)[._]?(\d{1,2})$", RegexOptions.IgnoreCase);
        if (!m.Success || !Enum.TryParse<EGame>($"GAME_UE{m.Groups[1].Value}_{m.Groups[2].Value}", out var g))
            throw new ArgumentException($"unknown engine version \"{s}\" (use e.g. 4.19 or 4.20)");
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
