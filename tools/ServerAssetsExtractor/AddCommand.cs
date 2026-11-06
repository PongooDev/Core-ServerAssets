using System.Text;
using System.Text.RegularExpressions;
using CUE4Parse.UE4.Versions;

namespace ServerAssetsExtractor;

public static partial class Program
{
    private static readonly string[] AddExtensions = [".uasset", ".uexp", ".umap", ".ubulk", ".uptnl", ".ini"];

    /// <summary>One entry of an existing pak: the name, and where its data (after the inline header) starts.</summary>
    private sealed record PakEntry(string Name, long DataOffset, long Size);

    private static bool IsPackageFile(string n) =>
        n.EndsWith(".uasset", StringComparison.OrdinalIgnoreCase) || n.EndsWith(".umap", StringComparison.OrdinalIgnoreCase)
        || n.EndsWith(".uexp", StringComparison.OrdinalIgnoreCase) || n.EndsWith(".ubulk", StringComparison.OrdinalIgnoreCase)
        || n.EndsWith(".uptnl", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// add: copies an existing pak, replacing or adding the given files, and writes a new sig and contents.txt.
    /// </summary>
    private static int RunAdd(string[] args)
    {
        string? pak = null, outPak = null, rulesPath = null;
        var force = false;
        var ue = EGame.GAME_UE4_20;
        var inputs = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            string Next() => i + 1 < args.Length ? args[++i] : throw new UserError($"{a} needs a value");
            switch (a)
            {
                case "--out": outPak = Next(); break;
                case "--ue": ue = ParseUeOrThrow(Next()); break;
                case "--rules": rulesPath = Next(); break;
                case "--force": force = true; break;
                default:
                    if (a.StartsWith('-')) throw new UserError($"unknown option {a}");
                    if (pak == null) pak = a; else inputs.Add(a);
                    break;
            }
        }
        if (pak == null || inputs.Count == 0) throw new UserError("add needs an existing pak and at least one file or folder");
        pak = Path.GetFullPath(pak);
        if (!File.Exists(pak)) throw new UserError($"{pak} does not exist");
        var rules = Rules.Load(rulesPath);

        // 1. The files to add, with their pak paths.
        var add = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); // pak path -> source file
        foreach (var input in inputs)
        {
            var spec = input.Trim('"');
            var eq = spec.IndexOf('=');
            if (eq > 1 && !Directory.Exists(spec))
            {
                Put(add, Path.GetFullPath(spec[..eq]), spec[(eq + 1)..]);
            }
            else if (Directory.Exists(spec))
            {
                var dir = Path.GetFullPath(spec);
                foreach (var f in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
                {
                    var rel = Path.GetRelativePath(dir, f).Replace('\\', '/');
                    if (!AddExtensions.Contains(Path.GetExtension(f).ToLowerInvariant())) { Warn($"Skipped {rel}: not a package or config file."); continue; }
                    Put(add, f, rel);
                }
            }
            else if (File.Exists(spec))
            {
                // A single file: its pak path is the part of its path from FortniteGame/ or Engine/ on.
                var full = Path.GetFullPath(spec).Replace('\\', '/');
                var m = Regex.Match(full, "/((FortniteGame|Engine)/.*)$", RegexOptions.IgnoreCase);
                if (!m.Success) throw new UserError($"{spec}: can't tell its game path. Pass it as <file>=FortniteGame/Content/... or put it in a FortniteGame/Content/... folder.");
                Put(add, full, m.Groups[1].Value);
            }
            else throw new UserError($"{spec} does not exist");
        }
        if (add.Count == 0) throw new UserError("no files to add");

        // 2. The default rules, checked on the files alone (a small temporary pak, mounted and read like a build).
        var excluded = new List<(string path, string why)>();
        var tmpDir = Path.Combine(Path.GetTempPath(), "sae-add-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmpDir);
        try
        {
            var tmpPak = Path.Combine(tmpDir, "check.pak");
            using (var w = new PakWriter(tmpPak))
            {
                foreach (var (name, src) in add.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)) w.Add(name, File.ReadAllBytes(src));
                w.Finish();
            }
            using var provider = new PakListProvider([new FileInfo(tmpPak)], new VersionContainer(ue));
            provider.Initialize();
            provider.Mount();
            foreach (var p in Scanner.Scan(provider, rules, (_, _) => { }))
            {
                var (keep, why) = rules.Decide(p);
                if (p.Class == "<unreadable>") Warn($"{p.GamePath}: header can't be read ({p.Error}). Is --ue right?");
                else if (!keep) excluded.Add((p.Path, why));
            }
            foreach (var f in add.Keys.Where(k => !IsPackageFile(k)))
            {
                var (keep, why) = rules.DecideLoose(f);
                if (!keep) excluded.Add((f, why));
            }
        }
        finally { try { Directory.Delete(tmpDir, true); } catch { } }

        foreach (var (path, why) in excluded)
            Warn($"The default rules leave out {path} ({why}).{(force ? " Adding it (--force)." : " Not added; pass --force to add it.")}");
        if (!force)
        {
            var drop = excluded.Select(e => e.path).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var k in add.Keys.ToList())
            {
                var bare = IsPackageFile(k) ? k[..k.LastIndexOf('.')] : k;
                if (drop.Contains(bare)) add.Remove(k);
            }
            if (add.Count == 0) throw new UserError("nothing left to add");
        }

        // 3. Output.
        outPak = Path.GetFullPath(outPak ?? Path.Combine(Path.GetDirectoryName(pak)!, "added", Path.GetFileName(pak)));
        if (outPak.Equals(pak, StringComparison.OrdinalIgnoreCase)) throw new UserError("--out must be a different file from the existing pak");
        if (File.Exists(outPak)) throw new UserError($"{outPak} already exists");
        var outDir = Path.GetDirectoryName(outPak)!;
        Directory.CreateDirectory(outDir);
        var sigPath = Path.ChangeExtension(outPak, ".sig");
        var contentsPath = Path.Combine(outDir, "contents.txt");
        if (File.Exists(sigPath)) throw new UserError($"{sigPath} already exists");

        Header($"Adding {add.Count} files to {Path.GetFileName(pak)}");
        var entries = ReadPakIndex(pak);
        var names = new HashSet<string>(entries.Select(e => e.Name), StringComparer.OrdinalIgnoreCase);
        var replaced = add.Keys.Count(names.Contains);
        var merged = new SortedDictionary<string, (PakEntry? old, string? src)>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in entries) merged[e.Name] = (e, null);
        foreach (var (name, src) in add) merged[name] = (null, src);

        using (var fs = new FileStream(pak, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 20))
        using (var w = new PakWriter(outPak))
        {
            foreach (var (name, (old, src)) in merged)
            {
                if (old != null)
                {
                    var buf = new byte[old.Size];
                    fs.Seek(old.DataOffset, SeekOrigin.Begin);
                    if (PakWriter.ReadFull(fs, buf) != buf.Length) throw new UserError("the existing pak is cut short");
                    w.Add(name, buf);
                }
                else w.Add(name, File.ReadAllBytes(src!));
            }
            w.Finish();
        }
        PakWriter.WriteSig(outPak, sigPath);
        File.WriteAllLines(contentsPath, ContentsOf(merged.Keys));
        Info($"{add.Count - replaced} added, {replaced} replaced; {merged.Count:N0} entries, {Size(new FileInfo(outPak).Length)}");
        foreach (var k in add.Keys.Where(k => k.EndsWith(".uasset", StringComparison.OrdinalIgnoreCase) || k.EndsWith(".umap", StringComparison.OrdinalIgnoreCase)))
            if (!merged.ContainsKey(k[..k.LastIndexOf('.')] + ".uexp"))
                Warn($"{k} has no .uexp in the pak; a cooked package needs both.");

        Header("Checking the pak");
        var ok = Verifier.Verify(outPak, ue, Console.WriteLine);
        Console.WriteLine(ok ? "All checks passed." : "Some checks FAILED.");
        if (ok) Info($"Made {outPak} (with its .sig and contents.txt beside it).");
        return ok ? 0 : 3;
    }

    private static void Put(Dictionary<string, string> add, string src, string pakPath)
    {
        pakPath = pakPath.Replace('\\', '/').TrimStart('/');
        if (!AddExtensions.Contains(Path.GetExtension(pakPath).ToLowerInvariant()))
            throw new UserError($"{pakPath}: only {string.Join(", ", AddExtensions)} files can be added");
        var root = pakPath.Split('/')[0];
        if (!root.Equals("FortniteGame", StringComparison.OrdinalIgnoreCase) && !root.Equals("Engine", StringComparison.OrdinalIgnoreCase))
            throw new UserError($"{pakPath}: the path must start with FortniteGame/ or Engine/");
        if (pakPath.Contains("/../") || pakPath.Contains("//")) throw new UserError($"{pakPath}: not a valid path");
        add[pakPath] = src;
    }

    /// <summary>The same list the extractor writes: package game paths and loose files, sorted.</summary>
    private static List<string> ContentsOf(IEnumerable<string> names)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var n in names)
        {
            if (n.EndsWith(".uasset", StringComparison.OrdinalIgnoreCase) || n.EndsWith(".umap", StringComparison.OrdinalIgnoreCase))
                set.Add(PackageInfo.ToGamePath(n[..n.LastIndexOf('.')]));
            else if (!IsPackageFile(n)) set.Add(n);
        }
        return set.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>Reads the index of a pak this tool wrote: version 3, unencrypted, uncompressed.</summary>
    private static List<PakEntry> ReadPakIndex(string pakPath)
    {
        using var fs = File.OpenRead(pakPath);
        using var br = new BinaryReader(fs);
        if (fs.Length < 44) throw new UserError("the file is too small to be a pak");
        fs.Seek(-44, SeekOrigin.End);
        if (br.ReadUInt32() != PakWriter.PakMagic) throw new UserError("not a pak (bad magic)");
        if (br.ReadInt32() != PakWriter.PakVersion) throw new UserError("only version 3 paks (the ones this tool makes) can be added to");
        var indexOffset = br.ReadInt64();
        var indexSize = br.ReadInt64();
        fs.Seek(indexOffset, SeekOrigin.Begin);
        using var ir = new BinaryReader(new MemoryStream(br.ReadBytes((int) indexSize)));
        string ReadStr()
        {
            var len = ir.ReadInt32();
            if (len == 0) return "";
            if (len > 0) return Encoding.ASCII.GetString(ir.ReadBytes(len), 0, len - 1);
            return Encoding.Unicode.GetString(ir.ReadBytes(-len * 2), 0, (-len - 1) * 2);
        }
        if (ReadStr() != PakWriter.MountPoint) throw new UserError($"the pak's mount point must be \"{PakWriter.MountPoint}\"");
        var count = ir.ReadInt32();
        var list = new List<PakEntry>(count);
        for (var i = 0; i < count; i++)
        {
            var name = ReadStr();
            var offset = ir.ReadInt64();
            var size = ir.ReadInt64();
            ir.ReadInt64();
            var comp = ir.ReadInt32();
            ir.ReadBytes(20);
            if (comp != 0) throw new UserError("the pak has compressed entries; it isn't one of this tool's paks");
            if (ir.ReadByte() != 0) throw new UserError("the pak has encrypted entries; it isn't one of this tool's paks");
            ir.ReadUInt32();
            list.Add(new PakEntry(name, offset + 53, size));
        }
        return list;
    }
}
