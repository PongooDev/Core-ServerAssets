using System.IO.Hashing;
using System.Security.Cryptography;
using System.Text;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Versions;

namespace ServerAssetsExtractor;

/// <summary>
/// Checks a server-assets pak the way a maintainer would before publishing it: the raw format (version, mount point,
/// entry headers and hashes), that CUE4Parse mounts it and reads every package header, the .sig, and contents.txt.
/// </summary>
public static class Verifier
{
    public static bool Verify(string pakPath, EGame ue, Action<string> log)
    {
        var ok = true;
        void Fail(string msg) { ok = false; log("  FAIL  " + msg); }
        void Pass(string msg) => log("  ok    " + msg);

        var pak = new FileInfo(pakPath);
        if (!pak.Exists) { Fail($"{pakPath} does not exist"); return false; }

        // 1. Raw format.
        var names = new List<string>();
        using (var fs = pak.OpenRead())
        using (var br = new BinaryReader(fs))
        {
            if (fs.Length < 44) { Fail("the file is too small to be a pak"); return false; }
            fs.Seek(-44, SeekOrigin.End);
            var magic = br.ReadUInt32();
            var version = br.ReadInt32();
            var indexOffset = br.ReadInt64();
            var indexSize = br.ReadInt64();
            var indexHash = br.ReadBytes(20);
            if (magic != PakWriter.PakMagic) { Fail($"bad pak magic 0x{magic:X8}"); return false; }
            if (version != PakWriter.PakVersion) Fail($"pak version {version}, expected {PakWriter.PakVersion}"); else Pass("pak version 3");
            if (indexOffset + indexSize + 44 != fs.Length) Fail("the index does not end at the footer");

            fs.Seek(indexOffset, SeekOrigin.Begin);
            var index = br.ReadBytes((int) indexSize);
            if (!SHA1.HashData(index).AsSpan().SequenceEqual(indexHash)) Fail("the index hash does not match"); else Pass("index hash");

            using var ir = new BinaryReader(new MemoryStream(index));
            var mount = ReadFString(ir);
            if (mount != PakWriter.MountPoint) Fail($"mount point is \"{mount}\", must be exactly \"{PakWriter.MountPoint}\"");
            else Pass($"mount point \"{mount}\"");

            var count = ir.ReadInt32();
            var badHeaders = 0;
            var badHashes = 0;
            var compressedOrEncrypted = 0;
            for (var i = 0; i < count; i++)
            {
                var name = ReadFString(ir);
                names.Add(name);
                var offset = ir.ReadInt64();
                var size = ir.ReadInt64();
                var usize = ir.ReadInt64();
                var comp = ir.ReadInt32();
                var hash = ir.ReadBytes(20);
                if (comp != 0) { compressedOrEncrypted++; ir.ReadBytes(ir.ReadInt32() * 16); }
                var encrypted = ir.ReadByte();
                ir.ReadUInt32();
                if (encrypted != 0) compressedOrEncrypted++;

                fs.Seek(offset, SeekOrigin.Begin);
                var hOffset = br.ReadInt64();
                var hSize = br.ReadInt64();
                var hUsize = br.ReadInt64();
                var hComp = br.ReadInt32();
                var hHash = br.ReadBytes(20);
                br.ReadByte();
                br.ReadUInt32();
                if (hOffset != 0 || hSize != size || hUsize != usize || hComp != comp || !hHash.AsSpan().SequenceEqual(hash)) badHeaders++;
                var data = br.ReadBytes((int) size);
                if (!SHA1.HashData(data).AsSpan().SequenceEqual(hash)) badHashes++;
            }
            if (compressedOrEncrypted > 0) Fail($"{compressedOrEncrypted} entries are compressed or encrypted"); else Pass("no entry is compressed or encrypted");
            if (badHeaders > 0) Fail($"{badHeaders} inline entry headers don't match the index"); else Pass($"{count} entries, inline headers match the index");
            if (badHashes > 0) Fail($"{badHashes} entries' data doesn't match their SHA-1"); else Pass("every entry's SHA-1 matches its data");
            if (ir.BaseStream.Position != index.Length) Fail("the index has trailing bytes");
            var notContent = names.Count(n => !n.Contains("/Content/", StringComparison.OrdinalIgnoreCase) && !n.Contains("/Config/", StringComparison.OrdinalIgnoreCase));
            if (notContent > 0) log($"  note  {notContent} entries are outside a Content or Config folder");
        }

        // 2. CUE4Parse mounts it on its own and reads every package header.
        using (var provider = new PakListProvider([pak], new VersionContainer(ue)))
        {
            provider.Initialize();
            var mounted = provider.Mount();
            if (mounted != 1) Fail("CUE4Parse did not mount the pak");
            else
            {
                var files = provider.Files.Values.ToList();
                if (files.Count != names.Count) Fail($"CUE4Parse sees {files.Count} files, the index has {names.Count}");
                else Pass($"CUE4Parse mounts it and sees all {files.Count} files");
                var missing = names.Count(n => !provider.Files.ContainsKey(n));
                if (missing > 0) Fail($"{missing} index paths aren't where CUE4Parse expects them (mount point)");
                else Pass("CUE4Parse finds every file at its source path");

                var packages = files.Where(f => f.IsUePackage).ToList();
                var unreadable = 0;
                string? firstError = null;
                foreach (var f in packages)
                {
                    try { _ = new Package(f.CreateReader(), null, (CUE4Parse.UE4.Readers.FArchive?) null, null, provider, true).ExportMap.Length; }
                    catch (Exception e) { unreadable++; firstError ??= f.Path + ": " + e.Message; }
                }
                if (unreadable > 0) Fail($"{unreadable} of {packages.Count} package headers don't parse (first: {firstError})");
                else Pass($"all {packages.Count} package headers parse");
            }
        }

        // 3. The sig.
        var sigPath = Path.ChangeExtension(pakPath, ".sig");
        if (!File.Exists(sigPath)) Fail("no .sig next to the pak");
        else
        {
            var sig = File.ReadAllBytes(sigPath);
            var chunks = (int) ((pak.Length + 65535) / 65536);
            if (sig.Length != 68 + 4 * chunks) Fail($"the sig is {sig.Length} bytes, expected {68 + 4 * chunks}");
            else if (sig.AsSpan(0, 64).ContainsAnyExcept((byte) 0)) Fail("the sig's master signature isn't zeroed");
            else if (BitConverter.ToUInt32(sig, 64) != chunks) Fail("the sig's chunk count is wrong");
            else
            {
                using var fs = pak.OpenRead();
                var buf = new byte[65536];
                var bad = 0;
                for (var i = 0; i < chunks; i++)
                {
                    var n = PakWriter.ReadFull(fs, buf);
                    if (Crc32.HashToUInt32(buf.AsSpan(0, n)) != BitConverter.ToUInt32(sig, 68 + 4 * i)) bad++;
                }
                if (bad > 0) Fail($"{bad} of the sig's {chunks} CRCs are wrong"); else Pass($"sig: zeroed master signature, {chunks} chunk CRCs match");
            }
        }

        // 4. contents.txt lists exactly the pak's packages.
        var contentsPath = Path.Combine(pak.DirectoryName!, "contents.txt");
        if (!File.Exists(contentsPath)) log("  note  no contents.txt next to the pak");
        else
        {
            var listed = File.ReadAllLines(contentsPath).Where(l => l.Length > 0 && !l.StartsWith('#')).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var inPak = names.Where(n => n.EndsWith(".uasset", StringComparison.OrdinalIgnoreCase) || n.EndsWith(".umap", StringComparison.OrdinalIgnoreCase))
                .Select(n => PackageInfo.ToGamePath(n[..n.LastIndexOf('.')])).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var loose = names.Where(n => !n.EndsWith(".uasset", StringComparison.OrdinalIgnoreCase) && !n.EndsWith(".umap", StringComparison.OrdinalIgnoreCase)
                                         && !n.EndsWith(".uexp", StringComparison.OrdinalIgnoreCase) && !n.EndsWith(".ubulk", StringComparison.OrdinalIgnoreCase)
                                         && !n.EndsWith(".uptnl", StringComparison.OrdinalIgnoreCase)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            inPak.UnionWith(loose);
            if (!listed.SetEquals(inPak)) Fail($"contents.txt lists {listed.Count} entries, the pak has {inPak.Count} packages and files");
            else Pass($"contents.txt lists all {listed.Count} packages and files");
        }

        return ok;
    }

    private static string ReadFString(BinaryReader r)
    {
        var len = r.ReadInt32();
        if (len == 0) return "";
        if (len > 0) return Encoding.ASCII.GetString(r.ReadBytes(len), 0, len - 1);
        var chars = -len;
        return Encoding.Unicode.GetString(r.ReadBytes(chars * 2), 0, (chars - 1) * 2);
    }
}
