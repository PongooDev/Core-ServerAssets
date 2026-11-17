---
name: release-extractor
description: Use when changing ServerAssetsExtractor (tools/ServerAssetsExtractor in Core-ServerAssets) - a fix, a new Fortnite season or UE version, or a rules change - and publishing it as an extractor-v<version> release with rules.json.
---

# Change and release ServerAssetsExtractor

Depth: `docs/agents/extractor.md`. Never print, log or write an AES key; never write inside a build folder.

1. Read `H:\CoreWork\HANDOFF.md`. Worktree from `origin/main` under `H:\CoreWork`.
2. Make the change in `tools/ServerAssetsExtractor`. Fix wrong output in the tool, not by editing paks.
3. Add or update a test in `tools/ServerAssetsExtractor.Tests`; run `dotnet test tools/ServerAssetsExtractor.Tests`.
4. Rules change? Bump `rules.json`'s `version`, make `verify` fail paks from the old rules, and plan the `filter`
   pass for published paks (list them in `HANDOFF.md` if you don't do it now).
5. Bump `<Version>` in `ServerAssetsExtractor.csproj`. Update the README: the release link, options, troubleshooting.
6. Commit (Freebuff/Codebuff: end with `Agent: Freebuff`, no Claude Co-Authored-By line; Claude: its own line; the
   pre-commit hook runs, never `--no-verify`),
   `git fetch origin`, `git rebase origin/main`, `git push origin HEAD:main`.
7. Publish:
   ```
   dotnet publish tools/ServerAssetsExtractor -c Release -r win-x64 -o H:\CoreWork\sae-<version>
   gh release create extractor-v<version> H:\CoreWork\sae-<version>\ServerAssetsExtractor.exe tools\ServerAssetsExtractor\rules.json -R PongooDev/Core-ServerAssets --title "ServerAssetsExtractor <version>" --notes-file <notes.md>
   ```
   Notes: what changed, the rules version, whether published paks need `filter`.
8. Comment on the issue that asked for it with the release link and commit; close it.
9. Remove the worktree, branch and publish folder.

Done when: tests pass, the release has the exe and `rules.json`, and the README links it.
