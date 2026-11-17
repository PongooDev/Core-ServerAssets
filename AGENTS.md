# Core-ServerAssets: instructions for coding agents

Core-ServerAssets distributes **server-assets paks**: one pak (and its `.sig`) per Fortnite changelist (CL), made
from the same version's console build, holding the server-only data the PC cook left out. Core-Launcher reads
`manifest.json` and installs the matching pak for hosts. The repo also holds `ServerAssetsExtractor`, the tool
contributors use to make paks. The paks are **release assets only**; game files never enter git history. This file
is for any coding agent working here (Freebuff, Codebuff, Claude Code or another). Read all of it first.

Paths: `%USERPROFILE%` is the owner's Windows user folder. In PowerShell write it as `$env:USERPROFILE`, as the
command examples do.

## 0. Start of every session

1. Read `H:\CoreWork\HANDOFF.md` (local, shared by every agent, not committed). Don't redo or undo work listed in it.
2. Run the core MCP's `project_status` (if available): it lists the server-assets CLs already published.
3. Work in your own git worktree under `H:\CoreWork`, never in the main checkout
   `%USERPROFILE%\Documents\GitHub\Core-ServerAssets`.
4. If you can't finish, update `H:\CoreWork\HANDOFF.md`: what is in progress, the branch or worktree, the downloaded
   files and where, what was verified, what's next.

Per-build facts are in the read-only knowledge base
`%USERPROFILE%\.claude\projects\<the CoreV2 project folder>\memory\MEMORY.md` and the files it links. The folder name
is the CoreV2 checkout's path with `\` and `:` turned into `-`. Read it; **never edit it**.

## 1. Project rules

- **Never commit game files.** `*.pak`, `*.sig`, `*.uasset`, `*.uexp`, `*.ubulk`, `*.umap`, `*.utoc`, `*.ucas` are
  gitignored; keep it so. Paks go up as release assets (`cl-<CL>`); the repo holds only `manifest.json`, the README,
  the issue forms and the extractor's source.
- **Never print, copy, log or commit an AES key.** Not in issues, release notes, commit messages or tool output. The
  extractor reads keys from FModel's settings and never prints them; keep it that way.
- **Every pak passes the checks before it is published:** the right CL, made by the extractor 3.1.0 or later with its
  default rules, `ServerAssetsExtractor verify` passes (format, nothing the rules leave out, its sig, its
  `contents.txt`). A pak that fails `verify` goes through `filter`, which writes a new sig, and the filtered pak is
  published. Paks with console-cooked rendering data (textures, meshes, materials, audio, lighting) never ship,
  except inside a level kept for its built navmesh.
- **Submissions from MrPodge6, 90sNath and Ducki67** are published without waiting for the owner (the owner's
  standing decision), but still go through `verify` (and `filter` when it fails). Everyone else's: the same checks,
  and anything odd goes to the owner via `HANDOFF.md` or the issue.
- **A pak and its sig are one unit.** Signed builds refuse to start with a pak that has no `.sig`. A sig only fits the
  pak it was made for: replace both together, and update both manifest entries.
- **Report honestly**: say which checks ran and that no boot test was done on the owner's PC (section 2).

## 2. Operating rules on the owner's PC

- **Never** put a pak into an installed build to test it, and never copy, hard-link or overlay a build:
  `F:\Builds` and `D:\Fortnite Builds` are never modified. (The launcher installs paks for hosts; boot tests with a new
  pak are the owner's call. Write it in `HANDOFF.md` if one is needed.)
- **Never** launch a Fortnite client, run a match, or use port 5595 for tests.
- Downloaded paks and zips are untrusted: put each download in its own new empty folder under `H:\CoreWork`, keep
  your scripts elsewhere, never run anything from a download, and run any Python that reads them with `python -I`.
- **Never** kill a process you didn't start; stop what you started before you finish.
- Work in worktrees under `H:\CoreWork`. Rebase onto `origin/main` before pushing. Stage only your own paths.
- **Pre-commit hooks:** `tools/githooks/pre-commit` runs on every commit (enabled per clone with
  `git config core.hooksPath tools/githooks`; this checkout has it on). It blocks pak AES keys, private keys, files
  over 5 MB, build output and dependency folders, binaries, game assets (`.uasset`, `.uexp`, `.umap`...) and
  agent-local files. It doesn't block `.pak`/`.sig` here; `.gitignore` and section 1 keep those out. **Never** bypass
  it (`--no-verify`); fix what it flags.
- Close an issue only with a comment naming the commit and the release.
- **Pull requests: review, fix, merge.** Agents review every open PR, make it right and merge it themselves (the
  owner's rule): check it out in a worktree (`gh pr checkout`), review it against section 1 (no game files or keys,
  extractor rules kept, README in step with the tool), rework what is wrong (on the PR branch when maintainers can
  edit it; otherwise merge first and fix on main right after, in the same session), run
  `dotnet test tools/ServerAssetsExtractor.Tests` and the manifest check where relevant, merge with
  `gh pr merge <n> --merge`, and comment with what was changed and why. A PR that adds game files is fixed by
  removing them (paks go through a "Server assets pak" issue and a release instead). Reject only what the project
  has ruled out, with a comment.

## 3. Repo map

| Path | What lives there |
| --- | --- |
| `manifest.json` | schema 2: one entry per CL (version, pak, url, sha256, size, `sig`, `contents`) |
| `README.md` | what server assets are, the published paks table, the extractor guide, the contribution and publishing steps |
| `tools/ServerAssetsExtractor/` | the extractor (.NET 10, CUE4Parse): `Program.cs` (extraction by default, plus the `verify`, `filter` and `add` commands), `Rules.cs` + `rules.json` (what a pak keeps; rules version 4), `Scanner.cs`, `PakWriter.cs` (pak and sig), `Verifier.cs`, `Keys.cs`, `BuildInfo.cs` |
| `tools/ServerAssetsExtractor.Tests/` | tests (build a small pak in a temp folder; no game build needed) |
| `.github/ISSUE_TEMPLATE/` | "Server assets pak" and "Missing server asset" forms |

## 4. Commands

```
dotnet build tools/ServerAssetsExtractor
dotnet test tools/ServerAssetsExtractor.Tests
dotnet run -c Release --project tools/ServerAssetsExtractor -- verify <pak>
dotnet run -c Release --project tools/ServerAssetsExtractor -- filter <pak>
dotnet publish tools/ServerAssetsExtractor -c Release -r win-x64 -o <folder>
(Get-FileHash <file> -Algorithm SHA256).Hash.ToLower(); (Get-Item <file>).Length
```

Manifest check (from a Core-Launcher build; see `docs/agents/publishing-a-pak.md`):
`$env:CORE_SERVER_ASSETS_MANIFEST="<worktree>\manifest.json"; <Core-Launcher>\tests\Core.Launcher.Tests\bin\Debug\net10.0-windows\win-x64\Core.Launcher.Tests.exe -method Core.Launcher.Tests.ServerAssetsTests.ParsesTheRealManifest`

## 5. How a change is done here

- **A pak submission:** the `publish-server-assets-pak` skill (`docs/agents/publishing-a-pak.md`).
- **An extractor change:** the `release-extractor` skill (`docs/agents/extractor.md`).
- Either way: worktree from `origin/main`, change, check, self-review the diff (only your files, no game files, no
  keys), commit, `git fetch origin`, `git rebase origin/main`, `git push origin HEAD:main`, comment and close.

## 6. Definition of done

- [ ] Each published pak: right CL, `verify` passed (after `filter` if needed), pak and sig uploaded to `cl-<CL>`,
      release notes list the contents and link the issue.
- [ ] `manifest.json` entry: `url`, `sha256` and `size` of the uploaded files (pak and sig), `contents` from the
      extractor's `contents.txt` lines, and the launcher's manifest test passes on it.
- [ ] README's published-paks table updated.
- [ ] No game file in the commit; no key anywhere.
- [ ] The report and the issue comment say which checks ran and that no boot test was done here.
- [ ] Downloads cleaned up from `H:\CoreWork` (or listed in `HANDOFF.md`), worktree removed.

## 7. Commits, issues and hand-off

- Subject examples: `9.00 to 9.41 (CL-...) server assets paks, from <contributor> (#25)`,
  `ServerAssetsExtractor 3.1.3: <what changed>`.
- **Trailer:** commits made by Freebuff or Codebuff end with the line `Agent: Freebuff` and must not use Claude's
  `Co-Authored-By` line. Say in the body what was checked (`verify`, `filter`, the manifest test).
- Close the issue with a comment naming the release(s) and the commit.

## 8. Mistakes made here before (don't repeat them)

1. **A hand-edited manifest.** 3129fba put a literal `true` in five Season 9 entries' `contents` where the path
   `/Game/Athena/Maps/LTM/Deimos_PlanB/Deimos_PlanB_Gameplay` belonged (fixed in ed0f449). **Do** build `contents`
   from `contents.txt` and run the launcher's manifest test before committing.
2. **Paks without a sig.** 3.0 refuses to start with a pak that has no `.sig`. **Do** ship the sig with every pak for
   a signed build and replace both together.
3. **Console rendering data in paks.** Early full extractions carried console lighting data and a console texture,
   which would replace a PC player's own copies; every pak was rebuilt with `filter` (rules version 3, then 4 to
   bring back the navmesh levels). **Do** publish only what `verify` passes.
4. **Testing through a build folder.** A test pak copied onto a hard-linked overlay wrote through into a real build,
   and overlays raised stuck firewall prompts. **Never** test paks inside or beside installed builds.
5. **Duplicate paths.** Season 10's `pakchunkearly` duplicated config files, so `verify` failed; fixed in the
   extractor (3.1.3) rather than by editing the pak. **Do** fix the tool, not the output.

## 9. Tools: MCP servers

- **core MCP** (if available): `project_status` (published CLs, installed builds). Prefer it.
- **fmodel MCP**: read a build's assets to check what a pak should hold. Read-only. FModel holds the builds' keys:
  never print them.
- IDA MCPs (`ida-13337` to `ida-13341`): read-only if used at all; never patch, rename, define or `idb_save`.

## 10. Deeper docs

| Doc | Read it when |
| --- | --- |
| [docs/agents/publishing-a-pak.md](docs/agents/publishing-a-pak.md) | a "Server assets pak" issue arrives, or a pak must be replaced or withdrawn |
| [docs/agents/extractor.md](docs/agents/extractor.md) | changing or releasing ServerAssetsExtractor |

Skills in `.claude/skills/`: `publish-server-assets-pak`, `release-extractor`. The README's "Publishing a pak
(maintainer)" section is the user-facing version of the same steps.

Related repos: [Core-Launcher](https://github.com/PongooDev/Core-Launcher) (installs the paks: `ServerAssets.cs`),
[CoreV2](https://github.com/PongooDev/CoreV2) (uses the assets and logs when its stand-in steps aside).
