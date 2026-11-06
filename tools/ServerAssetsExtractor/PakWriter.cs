using System.IO.Hashing;
using System.Security.Cryptography;
using System.Text;

namespace ServerAssetsExtractor;

/// <summary>
/// Writes a version 3 pak (what UE 4.19 and 4.20 read): unencrypted, uncompressed, one mount point. The layout is
/// that of the published paks: each file is an inline FPakEntry header (offset 0) followed by its bytes, then the
/// index (mount point, entry count, then name + FPakEntry per file), then the 44-byte footer.
/// </summary>
public sealed class PakWriter : IDisposable
{
    public const uint PakMagic = 0x5A6F12E1;
    public const int PakVersion = 3; // PakFile_Version_CompressionEncryption
    public const string MountPoint = "../../../";

    private readonly FileStream _out;
    private readonly List<(string name, long offset, long size, byte[] sha1)> _entries = [];

    public PakWriter(string path)
    {
        _out = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1 << 20);
    }

    public long Position => _out.Position;

    /// <summary>Adds a file. <paramref name="name"/> is relative to the mount point, e.g. FortniteGame/Content/X.uasset.</summary>
    public void Add(string name, byte[] data)
    {
        var sha1 = SHA1.HashData(data);
        var offset = _out.Position;
        WriteEntry(_out, 0, data.LongLength, sha1);
        _out.Write(data);
        _entries.Add((name, offset, data.LongLength, sha1));
    }

    public int Count => _entries.Count;

    /// <summary>Writes the index and footer and closes the file.</summary>
    public void Finish()
    {
        using var index = new MemoryStream();
        WriteFString(index, MountPoint);
        index.Write(BitConverter.GetBytes(_entries.Count));
        foreach (var e in _entries)
        {
            WriteFString(index, e.name);
            WriteEntry(index, e.offset, e.size, e.sha1);
        }

        var indexBytes = index.ToArray();
        var indexOffset = _out.Position;
        _out.Write(indexBytes);

        Span<byte> footer = stackalloc byte[44];
        BitConverter.TryWriteBytes(footer[0..4], PakMagic);
        BitConverter.TryWriteBytes(footer[4..8], PakVersion);
        BitConverter.TryWriteBytes(footer[8..16], indexOffset);
        BitConverter.TryWriteBytes(footer[16..24], (long) indexBytes.Length);
        SHA1.HashData(indexBytes).CopyTo(footer[24..44]);
        _out.Write(footer);
        _out.Flush(true);
        _out.Dispose();
    }

    public void Dispose() => _out.Dispose();

    /// <summary>FPakEntry, version 3: Offset, Size, UncompressedSize, CompressionMethod, Hash, bEncrypted, CompressionBlockSize.</summary>
    private static void WriteEntry(Stream s, long offset, long size, byte[] sha1)
    {
        Span<byte> b = stackalloc byte[53];
        BitConverter.TryWriteBytes(b[0..8], offset);
        BitConverter.TryWriteBytes(b[8..16], size);
        BitConverter.TryWriteBytes(b[16..24], size);
        BitConverter.TryWriteBytes(b[24..28], 0); // COMPRESS_None
        sha1.CopyTo(b[28..48]);
        b[48] = 0; // not encrypted
        BitConverter.TryWriteBytes(b[49..53], 0u); // no compression blocks
        s.Write(b);
    }

    /// <summary>An FString: ANSI with a positive length when it can be, UTF-16 with a negative length otherwise.</summary>
    private static void WriteFString(Stream s, string str)
    {
        if (str.All(c => c < 128))
        {
            var bytes = Encoding.ASCII.GetBytes(str + "\0");
            s.Write(BitConverter.GetBytes(bytes.Length));
            s.Write(bytes);
        }
        else
        {
            var bytes = Encoding.Unicode.GetBytes(str + "\0");
            s.Write(BitConverter.GetBytes(-(bytes.Length / 2)));
            s.Write(bytes);
        }
    }

    /// <summary>
    /// The pak's .sig: a 64-byte master signature (zeroed; Core's builds don't check it), a uint32 chunk count, then
    /// the zlib CRC32 of each 64 KiB chunk of the pak.
    /// </summary>
    public static void WriteSig(string pakPath, string sigPath)
    {
        const int chunk = 64 * 1024;
        var crcs = new List<uint>();
        using (var pak = new FileStream(pakPath, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 20))
        {
            var buf = new byte[chunk];
            while (true)
            {
                var read = ReadFull(pak, buf);
                if (read == 0) break;
                crcs.Add(Crc32.HashToUInt32(buf.AsSpan(0, read)));
                if (read < chunk) break;
            }
        }

        using var sig = new FileStream(sigPath, FileMode.CreateNew, FileAccess.Write);
        sig.Write(new byte[64]);
        sig.Write(BitConverter.GetBytes((uint) crcs.Count));
        foreach (var c in crcs) sig.Write(BitConverter.GetBytes(c));
    }

    public static int ReadFull(Stream s, byte[] buf)
    {
        var total = 0;
        while (total < buf.Length)
        {
            var n = s.Read(buf, total, buf.Length - total);
            if (n == 0) break;
            total += n;
        }
        return total;
    }
}
