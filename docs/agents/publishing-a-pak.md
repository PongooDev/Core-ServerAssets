# Publishing a server-assets pak

The README's "Contributing a pak" and "Publishing a pak (maintainer)" sections are the reference. This is the same
routine as a checklist, with the rules agents must follow on the owner's PC.

## 1. Read the submission

`gh issue view <n> -R PongooDev/Core-ServerAssets --comments`. Note: the version and CL, the extractor version
(3.1.0 or later), the download link(s), and the attached `contents.txt` and `report.txt`.

- The CL must be one Core supports or tracks (README table, `project_status`). A console build rarely names its CL:
  check the submitter's reasoning (the PC build of the same version).
- Two CLs of one version: a pak for one may be published under the other only when the owner says so.

## 2. Download safely

- Each download in its own new empty folder: `H:\CoreWork\sa-<n>\download\`. Scripts elsewhere.
- A fork's release: `gh release download <tag> -R <fork owner>/Core-ServerAssets -D H:\CoreWork\sa-<n>\download`.
  pixeldrain or Dropbox: direct download. gofile and MEGA block automated downloads: leave those for the owner
  (`HANDOFF.md`).
- Never run anything from a download. Zips: extract only `.pak`, `.sig`, `.txt` into the same folder.

## 3. Check

```
cd H:\CoreWork\sa-<n>
dotnet run -c Release --project <worktree>\tools\ServerAssetsExtractor -- verify download\<file>.pak
```

- `verify` passes: go on with that pak and its sig.
- `verify` fails because of rules (an older extractor, console data, bad sig): run `filter`, which writes
  `filtered\<name>.pak` with a new sig, `contents.txt` and `report.txt`, then verifies it. Publish the filtered pak.
- A 3.0.0 pak lacks navmesh levels and `filter` can't add them back: ask the submitter for a 3.1.0+ pak.
- Read `report.txt`: anything surprising (a huge size, missing `AIDirector_Fortnite` on 1.10+, no POI volumes on 3.0+)
  goes in the release notes and the issue.
- No boot test on the owner's PC: never place the pak in an installed build. Say so in the issue comment.

## 4. Publish

1. Name the files `ServerAssets-<version>-CL-<CL>.pak` and `ServerAssets-<version>-CL-<CL>.sig`.
2. Release:
   ```
   gh release create cl-<CL> <pak> <sig> -R PongooDev/Core-ServerAssets --title "<version> (CL-<CL>)" --notes-file <notes.md>
   ```
   The notes list the pak's notable contents, the extractor version, whether it was filtered, and link the issue.
3. Hash and size of both uploaded files:
   ```
   (Get-FileHash <pak> -Algorithm SHA256).Hash.ToLower(); (Get-Item <pak>).Length
   (Get-FileHash <sig> -Algorithm SHA256).Hash.ToLower(); (Get-Item <sig>).Length
   ```
4. `manifest.json` entry, keyed by the CL as digits: `version`, `pak`, `url` (the release asset's download URL in this
   repo), `sha256`, `size`, `sig` {`file`, `url`, `sha256`, `size`}, and `contents`: the lines of `contents.txt`
   as JSON strings (generate them; don't type them). Keep the file's existing order (by CL).
5. Check the manifest parses the launcher's way:
   ```
   $env:CORE_SERVER_ASSETS_MANIFEST = "<worktree>\manifest.json"
   C:\Users\tkenn\Documents\GitHub\Core-Launcher\tests\Core.Launcher.Tests\bin\Debug\net10.0-windows\win-x64\Core.Launcher.Tests.exe -method Core.Launcher.Tests.ServerAssetsTests.ParsesTheRealManifest
   ```
6. README: add the build's row to the published-paks table (what it holds, whose pak, the issue).
7. Commit `manifest.json` and `README.md` only (no game files), rebase, push.
8. Close the issue: `gh issue close <n> -R PongooDev/Core-ServerAssets --comment "Published as cl-<CL> (<release link>), manifest in <commit>. Checks: verify<, filter>; not boot-tested here."`

## Replacing or withdrawing a pak

- Replace: upload the new pak and sig to the same release under the same names
  (`gh release upload cl-<CL> <pak> <sig> --clobber -R PongooDev/Core-ServerAssets`), update `url`, `sha256` and
  `size` for both in the manifest, and add a release note saying what changed. Launchers replace the installed copy
  when the hash differs.
- Withdraw: remove the manifest entry; launchers remove the installed copy. Deleting a release asset is the owner's
  decision.

## Clean up

Delete your download folder under `H:\CoreWork` when done (it is your own scratch), or list it in `HANDOFF.md` if the
work continues.
