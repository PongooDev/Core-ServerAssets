using System.Text;
using CUE4Parse.UE4.Versions;
using ServerAssetsExtractor;

namespace ServerAssetsExtractor.Tests;

/// <summary>
/// Season 10's build folder has a pakchunkearly pak next to pakchunk0, and both hold the same config files. Writing a
/// path twice made verify fail ("CUE4Parse sees N files, the index has N+1"), because CUE4Parse lists one file per
/// path no matter how many paks have it. The extractor now writes the one copy a running game loads.
/// </summary>
public sealed class DuplicatePathsTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "sae-tests-" + Guid.NewGuid().ToString("N"));

    private string BuildDir => Path.Combine(_root, "build");
    private string PaksDir => Path.Combine(BuildDir, "FortniteGame", "Content", "Paks");

    public void Dispose()
    {
        try { Directory.Delete(_root, true); } catch { }
    }

    [Fact]
    public void ExtractsOneCopyOfAConfigTwoPaksShare()
    {
        WritePak("pakchunk0-ps4.pak",
            ("Engine/Config/Base.ini", "[Core]\nVersion=10.00\n"),
            ("Engine/Config/BaseEngine.ini", "[Engine]\nBuild=10.00\n"),
            ("FortniteGame/Config/DefaultGame.ini", "[Game]\nVersion=10.00\n"));
        // The early chunk repeats pakchunk0's configs. The copies are built from the same files; this one differs so
        // the test can tell which copy was written.
        WritePak("pakchunkearly-ps4.pak",
            ("Engine/Config/Base.ini", "[Core]\nVersion=10.00\n"),
            ("Engine/Config/BaseEngine.ini", "[Engine]\nBuild=10.00-early\n"));

        var outDir = Path.Combine(_root, "out");
        var exit = Program.Main([BuildDir, "--ue", "4.23", "--cl", "7658179", "--out", outDir, "--yes"]);
        Assert.Equal(0, exit);

        var contents = File.ReadAllLines(Path.Combine(outDir, "contents.txt"));
        Assert.Equal(3, contents.Length);
        Assert.Equal(contents.Length, contents.Distinct(StringComparer.OrdinalIgnoreCase).Count());

        using var provider = new PakListProvider(
            [new FileInfo(Path.Combine(outDir, "ServerAssets-CL-7658179.pak"))], new VersionContainer(EGame.GAME_UE4_23));
        provider.Initialize();
        Assert.Equal(1, provider.Mount());
        Assert.Equal(3, provider.Files.Values.Count());

        // The game loads pakchunkearly's copy: MountAllPakFiles mounts a folder's paks in descending name order and
        // FPakPlatformFile keeps equal read orders in mount order, so the pak mounted first is searched first.
        Assert.Contains("10.00-early", Encoding.UTF8.GetString(provider.Files["Engine/Config/BaseEngine.ini"].Read()));
    }

    [Fact]
    public void KeepsOneCopyPerPathWhateverOrderThePaksAreMountedIn()
    {
        WritePak("pakchunk0-ps4.pak",
            ("Engine/Config/Base.ini", "[Core]\nVersion=10.00\n"),
            ("Engine/Config/BaseEngine.ini", "[Engine]\nBuild=10.00\n"));
        WritePak("pakchunkearly-ps4.pak",
            ("Engine/Config/Base.ini", "[Core]\nVersion=10.00\n"),
            ("Engine/Config/BaseEngine.ini", "[Engine]\nBuild=10.00-early\n"));

        using var provider = new PakListProvider(
        [
            new FileInfo(Path.Combine(PaksDir, "pakchunkearly-ps4.pak")),
            new FileInfo(Path.Combine(PaksDir, "pakchunk0-ps4.pak")),
        ], new VersionContainer(EGame.GAME_UE4_23));
        provider.Initialize();
        provider.Mount();

        var loose = Scanner.LooseFiles(provider, out var duplicates);

        Assert.Equal(2, duplicates);
        Assert.Equal(2, loose.Count);
        Assert.Single(loose, f => f.Path == "Engine/Config/Base.ini");
        Assert.Contains("10.00-early", Encoding.UTF8.GetString(loose.Single(f => f.Path == "Engine/Config/BaseEngine.ini").Read()));
    }

    private void WritePak(string name, params (string Path, string Text)[] files)
    {
        Directory.CreateDirectory(PaksDir);
        using var writer = new PakWriter(Path.Combine(PaksDir, name));
        foreach (var (path, text) in files) writer.Add(path, Encoding.UTF8.GetBytes(text));
        writer.Finish();
    }
}
