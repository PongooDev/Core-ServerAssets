# ServerAssetsExtractor

Source in `tools/ServerAssetsExtractor` (.NET 10; CUE4Parse pinned in the `.csproj`); tests in
`tools/ServerAssetsExtractor.Tests`. Contributors run the released single-file exe; the README's "Making a pak with
the extractor" section is their guide and must match what the tool does.

## Pieces

| File | Role |
| --- | --- |
| `Program.cs` | command line: the default extraction, `verify`, `filter`, `add`, options (`--out`, `--dry-run`, `--ue`, `--aes`, `--cl`, ...) |
| `BuildInfo.cs` | finds the paks, CL and engine version (`Engine/Build/Build.version`, known versions) |
| `Keys.cs` | AES keys from `--aes`, FModel's `AppSettings.json`, or a hidden prompt. Keys are held in memory and handed to CUE4Parse only: **nothing may print, log or write a key** |
| `Scanner.cs` | reads every package's exports and decides keep or leave out |
| `Rules.cs`, `rules.json` | the rules (version 4): `excludeExportClasses`, `excludeBulkData`, `keepNavMeshLevels` (decided first), `keepLevelPaths`... The exe also carries a built-in copy of `rules.json` |
| `PakWriter.cs` | writes the pak at the build's pak version, plain index, uncompressed entries, mount point `../../../`, and the `.sig` (chunk CRC32s per 64 KiB, zeroed master signature) |
| `Verifier.cs` | `verify`: format, hashes, readable by CUE4Parse, nothing the rules leave out, the sig, `contents.txt` |

## Rules that must hold

- A pak never holds console-cooked rendering data (textures, meshes, materials, audio, lighting, physics), except a
  level kept whole because it holds a built navmesh (`keepNavMeshLevels`, decided before the export classes). The
  pak overrides the host's own PC copies, so console data would break their client.
- The extractor only reads a build; it never writes inside it (`--out` must be outside the build).
- When the rules change, bump `rules.json`'s `version`, make `verify` fail paks from older rules, and say in the
  release notes what changes for published paks (they may need `filter`).
- Fix wrong output in the tool, never by hand-editing a pak.

## Changing it

1. Worktree under `H:\CoreWork` from `origin/main`.
2. Change the code; add a test in `tools/ServerAssetsExtractor.Tests` (tests build small paks in temp folders, no
   game build needed).
3. `dotnet test tools/ServerAssetsExtractor.Tests`.
4. If you have a real console build to check against, run `--dry-run` with `--out` in a scratch folder. Never point
   `--out` inside a build folder, and never print a key.
5. Bump `<Version>` in `ServerAssetsExtractor.csproj`; update the README (the release link, options, troubleshooting).

## Releasing it

```
dotnet publish tools/ServerAssetsExtractor -c Release -r win-x64 -o H:\CoreWork\sae-<version>
gh release create extractor-v<version> H:\CoreWork\sae-<version>\ServerAssetsExtractor.exe tools\ServerAssetsExtractor\rules.json -R PongooDev/Core-ServerAssets --title "ServerAssetsExtractor <version>" --notes-file <notes.md>
```

The notes say what changed, the rules version, and whether published paks need `filter`. Update the README's link to
the new `extractor-v<version>` release in the same commit as the version bump.
