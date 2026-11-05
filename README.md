# Core-ServerAssets

Server assets for [Core](https://github.com/PongooDev/CoreV2), the Fortnite private server: one small pak per
Fortnite build, holding only the assets a game server needs that the PC game doesn't ship.

The paks are published as [releases](https://github.com/PongooDev/Core-ServerAssets/releases) of this repository.
[`manifest.json`](manifest.json) says which pak belongs to which build.

**Published so far:** [3.0 (CL-3901517)](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-3901517),
[3.1.0 (CL-3915963)](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-3915963),
[3.1.1 (CL-3917250)](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-3917250),
[3.2 (CL-3935073)](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-3935073) and
[3.3 (CL-3942182)](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-3942182),
[3.5.1 (CL-4000805)](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4000805) and
[3.5.2 (CL-4008490)](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4008490), each with `AIDirector_Fortnite`, `Athena_LocationVolumes` and `Athena_GameplayActors`. Most assets are still wanted for other builds. If you can make a pak, see
[Contributing a pak](#contributing-a-pak).

## Contents

- [What server assets are](#what-server-assets-are)
- [How a pak is used](#how-a-pak-is-used)
- [Server assets to get](#server-assets-to-get)
- [Published paks](#published-paks)
- [Contributing a pak](#contributing-a-pak)
- [manifest.json](#manifestjson)
- [Publishing a pak (maintainer)](#publishing-a-pak-maintainer)

## What server assets are

When Epic cooked the PC game, they left out content that only their own servers used. Some assets are missing
entirely. Others are still there, but with the server-only parts removed. Players never noticed, because the client
never needs that content.

Core runs its game servers from the PC game, so it has the same gaps. For each one, Core has a stand-in: settings
copied from an older build, or shapes it works out from what the PC paks still contain. A stand-in works, but it is
not Epic's data, and some gaps can't be filled at all.

The missing content is still in the same version's console builds:

- **PlayStation 4** builds have the server assets up to Season 12.
- **Xbox** builds have them in every build.

A pak made from those builds puts it back. When the real asset is there, Core uses it and its stand-in steps aside.
The server log says which one is in use.

## How a pak is used

You don't have to do anything. [Core-Launcher](https://github.com/PongooDev/Core-Launcher) does it for anyone who
hosts servers:

1. Before a build's first server starts, the launcher reads `manifest.json`.
2. If there is a pak for that build's changelist, the launcher downloads it and checks its size and SHA-256.
3. It puts the pak in the build's `FortniteGame\Content\Paks` folder as `pakchunkCoreServerAssets-WindowsClient_P.pak`.
   The game's own paks are never touched.
4. Some builds, such as 3.0, sign their paks and refuse to start if a pak has no `.sig` file next to it. For those,
   the manifest lists a sig, and the launcher installs it as `pakchunkCoreServerAssets-WindowsClient_P.sig`. Both
   files are checked before either one is put in place, so a pak is never installed without its sig. This needs
   Core-Launcher 1.0.15 or later. Older launchers install nothing.

Core's pak (and sig) is removed again if the manifest stops listing the build, or if you turn off **Download server assets for
hosted servers** (Settings → Servers). If GitHub can't be reached, the server starts without it.

Players don't need the pak. Only the server uses it.

## Server assets to get

These are the server-only assets Core knows it needs. **The list is growing.** If you find more server-only content,
[open an issue](https://github.com/PongooDev/Core-ServerAssets/issues/new?template=missing_server_asset.yml).

| Build (changelist) | Asset | What it is | What Core does without it | Published for |
| --- | --- | --- | --- | --- |
| 1.10 (CL-3790078) and later | `/Game/AIDirector/AIDirector_Fortnite` (the Blueprint and its `Default__AIDirector_Fortnite_C`) | Save the World's AI director: encounter classes, spawn caps, spawn points, spawn group upgrades | Uses 1.9.1's settings, plus some for features added later (Mini-Bosses, the BluGlo Difficulty Pylon's boosts). Each later build's own settings are unknown | **3.0**, **3.1.0**, **3.1.1**, **3.2**, **3.3**, **3.5.1**, **3.5.2** |
| 3.0 (CL-3901517) and later | `/Game/Athena/Maps/Athena_LocationVolumes` | 34 POI volumes: named locations, gas stations, taco shops, and the Crab, Fox and Llama buildings. Needed **with each `FortPoiVolume`'s `BrushComponent`** (its transform and `BrushBodySetup`) | Guesses a box for each volume from the plots it stands on | **3.0**, **3.1.0**, **3.1.1**, **3.2**, **3.3**, **3.5.1**, **3.5.2** |
| 3.0 (CL-3901517) and later | `/Game/Athena/Maps/Streaming/Athena_GameplayActors` | 29 POI volumes: 15 `Quest_Dance_Forbidden_NN` and 14 `Quest_Visit_IceCreamTruck_NN`, **with their `BrushComponent`s** | Puts boxes on the ice cream trucks. The 15 forbidden dance spots can't be placed at all, so that quest can't be completed | **3.0**, **3.1.0**, **3.1.1**, **3.2**, **3.3**, **3.5.1**, **3.5.2** |
| 2.5 (CL-3889387) and later | The loot tier group `BluGloDifficultyTG` (the server's loot tier data; the PC paks only ship `LootTierData_Client`) | What each tier of Save the World's BluGlo Difficulty Pylon gives | Core-Backend gives a small reward of its own per tier | **Wanted** for every build |

The PC builds from 1.1 to 1.9.1 still have `AIDirector_Fortnite`, so they don't need it. The rest of
`/Game/AIDirector` is in the PC paks of every build Core supports. Only `AIDirector_Fortnite` is missing.

Core supports these builds. Each pak must match one of them exactly:

| Build | Changelist |
| --- | --- |
| 1.10 | 3790078 |
| 1.11 | 3807424 |
| 2.1 | 3825894 |
| 2.2 | 3841827 |
| 2.3 | 3847564 |
| 2.4 | 3858292 |
| 2.4.2 | 3870737 |
| 2.5 | 3889387 |
| 3.0 | 3901517 |
| 3.1.0 | 3915963 |
| 3.1.1 | 3917250 |
| 3.2 | 3935073 |
| 3.3 | 3942182 |
| 3.5.1 | 4000805 |
| 3.5.2 | 4008490 |

## Published paks

| Build | Release | Contains | Still wanted for this build |
| --- | --- | --- | --- |
| 3.0 (CL-3901517) | [`cl-3901517`](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-3901517), pak and sig | `AIDirector_Fortnite`, `Athena_LocationVolumes`, `Athena_GameplayActors` | the server loot tier data |
| 3.1.0 (CL-3915963) | [`cl-3915963`](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-3915963), pak and sig | `AIDirector_Fortnite`, `Athena_LocationVolumes`, `Athena_GameplayActors` | the server loot tier data |
| 3.1.1 (CL-3917250) | [`cl-3917250`](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-3917250), pak and sig | `AIDirector_Fortnite`, `Athena_LocationVolumes`, `Athena_GameplayActors` | the server loot tier data |
| 3.2 (CL-3935073) | [`cl-3935073`](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-3935073), pak and sig | `AIDirector_Fortnite`, `Athena_LocationVolumes`, `Athena_GameplayActors` | the server loot tier data |
| 3.3 (CL-3942182) | [`cl-3942182`](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-3942182), pak and sig | `AIDirector_Fortnite`, `Athena_LocationVolumes`, `Athena_GameplayActors` | the server loot tier data |
| 3.5.1 (CL-4000805) | [`cl-4000805`](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4000805), pak and sig | `AIDirector_Fortnite`, `Athena_LocationVolumes`, `Athena_GameplayActors` | the server loot tier data |
| 3.5.2 (CL-4008490) | [`cl-4008490`](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4008490), pak and sig | `AIDirector_Fortnite`, `Athena_LocationVolumes`, `Athena_GameplayActors` | the server loot tier data |

The first 3.0 pak ([#1](https://github.com/PongooDev/Core-ServerAssets/issues/1)) only had `Athena_LocationVolumes`,
because it came from a PS4 patch PKG, which carries only the changed blocks. MrPodge6's pak
([#2](https://github.com/PongooDev/Core-ServerAssets/issues/2)), from a PS4 3.0 build, replaced it with all three
assets. Only the server loot tier data is still wanted for 3.0.

MrPodge6's 3.1.1 pak ([#8](https://github.com/PongooDev/Core-ServerAssets/issues/8)) is byte-identical to the 3.1.0 one,
so 3.1.1 did not change these assets. Paks are per changelist and a launcher installs a pak only for an exact
changelist match, so 3.1.1 has its own release and manifest entry.

## Contributing a pak

1. Get the server assets from a console build of the **same version and changelist** as a PC build Core supports:
   a PlayStation 4 build for Season 12 and earlier, or an Xbox build for any season.
   [ServerAssets-OGFN](https://github.com/MarvelcoCode/ServerAssets-OGFN) explains how. Check the changelist in the
   console build's `Engine/Build/Build.version`. Console patch numbers aren't Fortnite version numbers. For example,
   PS4 patch 01.30 is Fortnite 1.9.
2. Make a pak that the PC game can mount. **Only include the assets listed [above](#server-assets-to-get)** for that
   build (with their `.uasset`, `.uexp` and `.ubulk` files). Leave out everything else. If the build's own paks have
   `.sig` files next to them, the pak needs one too, or the server dies at startup. A sig is a 64-byte master signature,
   a `uint32` chunk count, then the CRC32 of each 64 KiB chunk of the pak. Use the correct CRCs and a zeroed master
   signature.
3. Test it if you can: put it in the build's `FortniteGame\Content\Paks` and host a server. The server log should say
   Core uses the real asset.
4. [Open a "Server assets pak" issue](https://github.com/PongooDev/Core-ServerAssets/issues/new?template=server_assets_pak.yml)
   with the pak attached, or a link to it.

**What gets checked before a pak is published:**
- it is for the right changelist
- it contains only the listed assets
- it loads on a headless server

If you only have [FModel](https://fmodel.app) JSON exports, not a pak, share them on
[CoreV2's "Server asset data" form](https://github.com/PongooDev/CoreV2/issues/new?template=server_asset_data.yml).
Core can use the data to make its stand-ins match Epic's.

## manifest.json

```json
{
  "schema": 2,
  "builds": {
    "3901517": {
      "version": "3.0",
      "pak": "ServerAssets-3.0-CL-3901517.pak",
      "url": "https://github.com/PongooDev/Core-ServerAssets/releases/download/cl-3901517/ServerAssets-3.0-CL-3901517.pak",
      "sha256": "<64 lowercase hex digits>",
      "size": 1234567,
      "sig": {
        "file": "ServerAssets-3.0-CL-3901517.sig",
        "url": "https://github.com/PongooDev/Core-ServerAssets/releases/download/cl-3901517/ServerAssets-3.0-CL-3901517.sig",
        "sha256": "<64 lowercase hex digits>",
        "size": 80
      },
      "contents": [
        "/Game/Athena/Maps/Athena_LocationVolumes"
      ]
    }
  }
}
```

| Field | Meaning |
| --- | --- |
| `schema` | The format version. `2` since `sig` was added, and a manifest with any `sig` must be `2`. Launchers before 1.0.15 only read schema `1`, so they install nothing rather than a pak without its sig. The launcher ignores a manifest with a schema it doesn't know |
| `builds` | One entry per build. The key is the build's changelist, as digits only |
| `version` | The Fortnite version, for people reading the file |
| `pak` | The release asset's file name |
| `url` | Where to download it. It must be a release download in this repository (`https://github.com/PongooDev/Core-ServerAssets/releases/download/...`) |
| `sha256` | The pak's SHA-256, in hex. The launcher only installs a pak whose hash matches |
| `size` | The pak's size in bytes. The launcher checks it too |
| `sig` | Optional. The pak's signature file, for builds that sign their paks (their own paks have `.sig` files next to them). It has `file` (the release asset's file name, ending in `.sig`), `url`, `sha256` and `size`, with the same rules as the pak's. The launcher installs it next to the pak as `pakchunkCoreServerAssets-<platform>_P.sig`, and installs and removes both together. Leave it out for builds that don't sign their paks, and no sig is installed |
| `contents` | The asset paths in the pak |

The launcher refuses the whole manifest if any entry is incomplete or points outside this repository's releases.

## Publishing a pak (maintainer)

Once a contributed pak has passed the checks:

1. Name it `ServerAssets-<version>-CL-<changelist>.pak`, and its sig, if it has one, `ServerAssets-<version>-CL-<changelist>.sig`.
2. Create a release tagged `cl-<changelist>`, titled `<version> (CL-<changelist>)`, and upload the pak (and sig) to
   it. List the assets it contains in the release notes, and link the issue it came from.
3. Get the hash and size of each file: `(Get-FileHash <file> -Algorithm SHA256).Hash.ToLower()` and
   `(Get-Item <file>).Length`.
4. Add its entry to `manifest.json`: version, pak, the release asset's download URL, sha256, size and contents, plus
   `sig` for a build that signs its paks. Check it parses: in a Core-Launcher checkout, build the tests and run
   `Core.Launcher.Tests.exe -method Core.Launcher.Tests.ServerAssetsTests.ParsesTheRealManifest` with
   `CORE_SERVER_ASSETS_MANIFEST` set to the file's path. Commit it to `main`.

Hosts' launchers install the pak the next time they start a server for that build.

To replace a pak, upload the new file to the same release and update the entry's `url`, `sha256` and `size`, and
its sig's, because a sig only fits the pak it was made for. Launchers then replace the installed copy. To withdraw a pak, remove its entry, and launchers remove the installed copy.

Game files never go in this repository's git history. They are only release assets, so the manifest and the
contribution forms can live on even if a release is taken down.

### Credits:
- Thanks to andrew (andr1ww_) for discovering xbox builds have serverassets.
