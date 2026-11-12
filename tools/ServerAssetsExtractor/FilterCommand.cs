using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using CUE4Parse.UE4.Versions;

namespace ServerAssetsExtractor;

public static partial class Program
{
    /// <summary>
    /// filter: copies an existing pak with only the packages and files the current rules keep, the same decisions an
    /// extraction makes, and writes a new sig, contents.txt and report.txt. For updating a published pak to new rules
    /// without the console build it came from.
    /// </summary>
    private static int RunFilter(string[] args)
    {
        string? pak = null, outPak = null, rulesPath = null;
        EGame? ue = null;
        var o = new Options();
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            string Next() => i + 1 < args.Length ? args[++i] : throw new UserError($"{a} needs a value");
            switch (a)
            {
                case "--out": outPak = Next(); break;
                case "--ue": ue = ParseUeOrThrow(Next()); break;
                case "--rules": rulesPath = Next(); break;
                case "--include-maps": o.IncludeMaps = true; break;
                default:
                    if (a.StartsWith('-')) throw new UserError($"unknown option {a}");
                    if (pak != null) throw new UserError("filter takes one pak");
                    pak = a;
                    break;
            }
        }
        if (pak == null) throw new UserError("filter needs a pak");
        pak = Path.GetFullPath(pak);
        if (!File.Exists(pak)) throw new UserError($"{pak} does not exist");
        var rules = Rules.Load(rulesPath);

        var clMatch = Regex.Match(Path.GetFileName(pak), @"CL-(\d{7})", RegexOptions.IgnoreCase);
        int? cl = clMatch.Success ? int.Parse(clMatch.Groups[1].Value) : null;
        var engine = ue ?? (cl is { } c ? BuildInfo.UeOf(c) : null)
                     ?? throw new UserError("can't tell the engine version from the file name; pass --ue");
        var version = cl is { } k ? BuildInfo.VersionOf(k) : null;

        outPak = Path.GetFullPath(outPak ?? Path.Combine(Path.GetDirectoryName(pak)!, "filtered", Path.GetFileName(pak)));
        if (outPak.Equals(pak, StringComparison.OrdinalIgnoreCase)) throw new UserError("--out must be a different file from the existing pak");
        if (File.Exists(outPak)) throw new UserError($"{outPak} already exists");
        var outDir = Path.GetDirectoryName(outPak)!;
        Directory.CreateDirectory(outDir);
        var sigPath = Path.ChangeExtension(outPak, ".sig");
        if (File.Exists(sigPath)) throw new UserError($"{sigPath} already exists");

        Header($"Filtering {Path.GetFileName(pak)} ({BuildInfo.UeName(engine)})");
        Info($"Rules: {rules.Source} (version {rules.Version})");
        var sw = Stopwatch.StartNew();
        using var provider = new PakListProvider([new FileInfo(pak)], new VersionContainer(engine));
        provider.Initialize();
        if (provider.Mount() != 1) throw new UserError("the pak can't be mounted (is it encrypted, or not a pak?)");
        var all = Scanner.Scan(provider, rules, (n, total) => Progress($"  {n:N0} / {total:N0} packages"));
        EndProgress();
        var unreadable = all.Count(p => p.Class == "<unreadable>");
        if (unreadable > 0) Warn($"{unreadable:N0} package headers can't be read. Is --ue right?");

        ApplyDecisions(all, rules, o);
        var kept = all.Where(p => p.Keep).ToList();
        var (loose, looseSkipped) = DecideLooseFiles(provider, rules, o);
        var report = BuildReport(pak, cl, version, engine, rules, o, all, all, kept, loose, looseSkipped);
        Console.WriteLine();
        Console.WriteLine(report.summary);

        Header($"Writing {outPak}");
        var count = WritePak(outPak, kept, loose);
        PakWriter.WriteSig(outPak, sigPath);
        var contents = kept.Select(p => p.GamePath).Concat(loose.Select(f => f.Path)).OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList();
        File.WriteAllLines(Path.Combine(outDir, "contents.txt"), contents);
        File.WriteAllText(Path.Combine(outDir, "report.txt"), report.full, Encoding.UTF8);
        var dropped = all.Count - kept.Count;
        Info($"{count:N0} files kept ({kept.Count:N0} packages and {loose.Count:N0} other files); {dropped:N0} packages left out; " +
             $"{Size(new FileInfo(pak).Length)} -> {Size(new FileInfo(outPak).Length)}, in {sw.Elapsed.TotalSeconds:N0} s");

        Header("Checking the pak");
        var ok = Verifier.Verify(outPak, engine, rules, o.IncludeMaps || rules.IncludeMaps, Console.WriteLine);
        Console.WriteLine(ok ? "All checks passed." : "Some checks FAILED.");
        return ok ? 0 : 3;
    }
}
