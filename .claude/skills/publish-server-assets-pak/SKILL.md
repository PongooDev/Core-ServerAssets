---
name: publish-server-assets-pak
description: Use when a "Server assets pak" issue arrives on Core-ServerAssets, or a published pak must be replaced - download safely, verify (and filter), release as cl-<CL> with its sig, add the manifest entry, check it with the launcher's test, update the README and close the issue. Never touches build folders or AES keys.
---

# Publish a server-assets pak

Depth: `docs/agents/publishing-a-pak.md`. Rules: AGENTS.md sections 1 and 2.

Never: print or post an AES key; commit a game file; put a pak in an installed build; copy or overlay a build; run
anything from a download.

1. Read `H:\CoreWork\HANDOFF.md`. Run `project_status` (core MCP, if available) for the CLs already published.
2. Read the issue: `gh issue view <n> -R PongooDev/Core-ServerAssets --comments`. Note CL, version, extractor
   version (3.1.0+), links, `contents.txt`, `report.txt`.
3. Worktree: `git -C C:\Users\tkenn\Documents\GitHub\Core-ServerAssets worktree add -b pak-<n> H:\CoreWork\sa-<n>-wt origin/main`.
4. Download into a new empty folder `H:\CoreWork\sa-<n>\download\` (`gh release download <tag> -R <fork> -D ...`;
   gofile/MEGA links: leave for the owner in `HANDOFF.md`).
5. `dotnet run -c Release --project H:\CoreWork\sa-<n>-wt\tools\ServerAssetsExtractor -- verify <pak>`.
   If it fails on rules or sig: `... -- filter <pak>` and use `filtered\<name>.pak` and its new sig.
   A 3.0.0 pak: ask for a 3.1.0+ one. Read `report.txt` for anything odd.
6. Name the files `ServerAssets-<version>-CL-<CL>.pak` / `.sig`, then
   `gh release create cl-<CL> <pak> <sig> -R PongooDev/Core-ServerAssets --title "<version> (CL-<CL>)" --notes-file <notes.md>`
   (notes: contents, extractor version, filtered or not, the issue).
7. Hash and size both files (`(Get-FileHash <f> -Algorithm SHA256).Hash.ToLower()`, `(Get-Item <f>).Length`).
8. Add the `manifest.json` entry (CL key; version, pak, url, sha256, size, sig {file, url, sha256, size}, contents
   generated from `contents.txt`).
9. Check it:
   ```
   $env:CORE_SERVER_ASSETS_MANIFEST = "H:\CoreWork\sa-<n>-wt\manifest.json"
   C:\Users\tkenn\Documents\GitHub\Core-Launcher\tests\Core.Launcher.Tests\bin\Debug\net10.0-windows\win-x64\Core.Launcher.Tests.exe -method Core.Launcher.Tests.ServerAssetsTests.ParsesTheRealManifest
   ```
10. README published-paks table row.
11. Commit `manifest.json` and `README.md` only. Freebuff/Codebuff commits end with `Agent: Freebuff` (no Claude
    Co-Authored-By line); Claude uses its own. `git fetch origin`, `git rebase origin/main`, `git push origin HEAD:main`.
12. Close the issue naming the release and commit, and saying the checks run and that no boot test was done here.
13. Delete `H:\CoreWork\sa-<n>` downloads, remove the worktree and branch; record anything unfinished in `HANDOFF.md`.

Done when: release `cl-<CL>` has pak and sig, the manifest entry matches them and passes the launcher test, the README
lists it, and the issue is closed with the release named.
