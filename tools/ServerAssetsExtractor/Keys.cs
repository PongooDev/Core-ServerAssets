using System.Text.Json;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Versions;

namespace ServerAssetsExtractor;

/// <summary>
/// AES keys for a build. Keys are only ever held in memory and handed to CUE4Parse: nothing here prints a key or
/// writes one anywhere.
/// </summary>
public sealed class KeySet
{
    public FAesKey? MainKey;
    public readonly List<(FGuid guid, FAesKey key)> DynamicKeys = [];
    public string Origin = "";
    public EGame? FModelUe;
}

public static class Keys
{
    public static string FModelSettingsPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FModel", "AppSettings.json");

    public static FAesKey Parse(string hex)
    {
        hex = hex.Trim();
        if (!hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) hex = "0x" + hex;
        if (hex.Length != 66 || !hex[2..].All(Uri.IsHexDigit))
            throw new ArgumentException("an AES key is 64 hex digits (optionally starting with 0x)");
        return new FAesKey(hex);
    }

    /// <summary>
    /// The FModel game entry for this build: the one whose game directory is the build folder, contains it, or is
    /// inside it. Null when FModel isn't installed or doesn't know the build.
    /// </summary>
    public static KeySet? FromFModel(string root, string paksDir)
    {
        // Best: FModel's game folder is the build folder or holds the paks. Next: it is inside the folder given,
        // but only when exactly one entry is (a folder of many builds must not pick one at random).
        var holding = new List<(string dir, JsonElement entry)>();
        var inside = new List<(string dir, JsonElement entry)>();
        foreach (var e in FModelEntries())
        {
            if (Contains(e.dir, paksDir) || Contains(e.dir, root)) holding.Add(e);
            else if (Contains(root, e.dir)) inside.Add(e);
        }
        var pick = holding.OrderByDescending(e => e.dir.Length).Select(e => ((string, JsonElement)?) e).FirstOrDefault()
                   ?? (inside.Count == 1 ? inside[0] : null);
        if (pick is not { } p) return null;
        var set = ToKeySet(p.Item2);
        set.Origin = "FModel's settings for " + p.Item1;
        return set;
    }

    /// <summary>Every distinct main key FModel knows, to try on a build FModel has no entry for.</summary>
    public static List<(string dir, FAesKey key)> AllFModelMainKeys()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var list = new List<(string, FAesKey)>();
        foreach (var (dir, entry) in FModelEntries())
        {
            if (!entry.TryGetProperty("AesKeys", out var aes) || !aes.TryGetProperty("mainKey", out var mk)) continue;
            var s = mk.GetString();
            if (string.IsNullOrWhiteSpace(s) || !seen.Add(s)) continue;
            try { list.Add((dir, Parse(s))); } catch (ArgumentException) { }
        }
        return list;
    }

    private static KeySet ToKeySet(JsonElement entry)
    {
        var set = new KeySet();
        if (entry.TryGetProperty("UeVersion", out var ue) && ue.TryGetUInt32(out var ueInt) && Enum.IsDefined(typeof(EGame), ueInt))
            set.FModelUe = (EGame) ueInt;
        if (entry.TryGetProperty("AesKeys", out var aes))
        {
            if (aes.TryGetProperty("mainKey", out var mk) && mk.GetString() is { Length: > 0 } s)
            {
                try { set.MainKey = Parse(s); } catch (ArgumentException) { }
            }
            if (aes.TryGetProperty("dynamicKeys", out var dk) && dk.ValueKind == JsonValueKind.Array)
            {
                foreach (var k in dk.EnumerateArray())
                {
                    if (!k.TryGetProperty("guid", out var g) || !k.TryGetProperty("key", out var kk)) continue;
                    try { set.DynamicKeys.Add((new FGuid(g.GetString()!), Parse(kk.GetString()!))); }
                    catch (Exception) { }
                }
            }
        }
        return set;
    }

    private static IEnumerable<(string dir, JsonElement entry)> FModelEntries()
    {
        JsonDocument doc;
        try
        {
            if (!File.Exists(FModelSettingsPath)) yield break;
            doc = JsonDocument.Parse(File.ReadAllText(FModelSettingsPath), new JsonDocumentOptions { AllowTrailingCommas = true });
        }
        catch (Exception) { yield break; }

        using (doc)
        {
            if (!doc.RootElement.TryGetProperty("PerDirectory", out var per) || per.ValueKind != JsonValueKind.Object) yield break;
            foreach (var p in per.EnumerateObject())
            {
                var dir = p.Value.TryGetProperty("GameDirectory", out var gd) && gd.GetString() is { Length: > 0 } g ? g : p.Name;
                yield return (dir, p.Value.Clone());
            }
        }
    }

    /// <summary>True when <paramref name="inner"/> is <paramref name="outer"/> or inside it.</summary>
    private static bool Contains(string outer, string inner)
    {
        static string N(string p)
        {
            try { return Path.GetFullPath(p).TrimEnd('\\', '/') + "\\"; } catch { return p; }
        }
        return N(inner).StartsWith(N(outer), StringComparison.OrdinalIgnoreCase);
    }
}
