# Core-ServerAssets

Server assets for [Core](https://github.com/PongooDev/CoreV2), the Fortnite private server: one small pak per
Fortnite build, holding only the assets a game server needs that the PC game doesn't ship.

The paks are published as [releases](https://github.com/PongooDev/Core-ServerAssets/releases) of this repository.
[`manifest.json`](manifest.json) says which pak belongs to which build.

**There are no paks yet.** If you can make one, see [Contributing a pak](#contributing-a-pak).

## Contents

- [What server assets are](#what-server-assets-are)
- [How a pak is used](#how-a-pak-is-used)
- [Server assets to get](#server-assets-to-get)
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

Core's pak is removed again if the manifest stops listing the build, or if you turn off **Download server assets for
hosted servers** (Settings → Servers). If GitHub can't be reached, the server starts without it.

Players don't need the pak. Only the server uses it.

## Server assets to get

These are the server-only assets Core knows it needs. **The list is growing.** If you find more server-only content,
[open an issue](https://github.com/PongooDev/Core-ServerAssets/issues/new?template=missing_server_asset.yml).

| Build (changelist) | Asset | What it is | What Core does without it |
| --- | --- | --- | --- |
| 1.10 (CL-3790078) and later | `/Game/AIDirector/AIDirector_Fortnite` (the Blueprint and its `Default__AIDirector_Fortnite_C`) | Save the World's AI director: encounter classes, spawn caps, spawn points, spawn group upgrades | Uses 1.9.1's settings, plus some for features added later (Mini-Bosses, the BluGlo Difficulty Pylon's boosts). Each later build's own settings are unknown |
| 3.0 (CL-3901517) and later | `/Game/Athena/Maps/Athena_LocationVolumes` | 34 POI volumes: named locations, gas stations, taco shops, and the Crab, Fox and Llama buildings. Needed **with each `FortPoiVolume`'s `BrushComponent`** (its transform and `BrushBodySetup`) | Guesses a box for each volume from the plots it stands on |
| 3.0 (CL-3901517) and later | `/Game/Athena/Maps/Streaming/Athena_GameplayActors` | 29 POI volumes: 15 `Quest_Dance_Forbidden_NN` and 14 `Quest_Visit_IceCreamTruck_NN`, **with their `BrushComponent`s** | Puts boxes on the ice cream trucks. The 15 forbidden dance spots can't be placed at all, so that quest can't be completed |
| 2.5 (CL-3889387) and later | The loot tier group `BluGloDifficultyTG` (the server's loot tier data; the PC paks only ship `LootTierData_Client`) | What each tier of Save the World's BluGlo Difficulty Pylon gives | Core-Backend gives a small reward of its own per tier |

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

## Contributing a pak

1. Get the server assets from a console build of the **same version and changelist** as a PC build Core supports:
   a PlayStation 4 build for Season 12 and earlier, or an Xbox build for any season.
   [ServerAssets-OGFN](https://github.com/MarvelcoCode/ServerAssets-OGFN) explains how. Check the changelist in the
   console build's `Engine/Build/Build.version`. Console patch numbers aren't Fortnite version numbers. For example,
   PS4 patch 01.30 is Fortnite 1.9.
2. Make a pak that the PC game can mount. **Only include the assets listed [above](#server-assets-to-get)** for that
   build (with their `.uasset`, `.uexp` and `.ubulk` files). Leave out everything else.
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
  "schema": 1,
  "builds": {
    "3901517": {
      "version": "3.0",
      "pak": "ServerAssets-3.0-CL-3901517.pak",
      "url": "https://github.com/PongooDev/Core-ServerAssets/releases/download/cl-3901517/ServerAssets-3.0-CL-3901517.pak",
      "sha256": "<64 lowercase hex digits>",
      "size": 1234567,
      "contents": [
        "/Game/Athena/Maps/Athena_LocationVolumes",
        "/Game/Athena/Maps/Streaming/Athena_GameplayActors"
      ]
    }
  }
}
```

| Field | Meaning |
| --- | --- |
| `schema` | The format version. Currently `1`. The launcher ignores a manifest with a schema it doesn't know |
| `builds` | One entry per build. The key is the build's changelist, as digits only |
| `version` | The Fortnite version, for people reading the file |
| `pak` | The release asset's file name |
| `url` | Where to download it. It must be a release download in this repository (`https://github.com/PongooDev/Core-ServerAssets/releases/download/...`) |
| `sha256` | The pak's SHA-256, in hex. The launcher only installs a pak whose hash matches |
| `size` | The pak's size in bytes. The launcher checks it too |
| `contents` | The asset paths in the pak |

The launcher refuses the whole manifest if any entry is incomplete or points outside this repository's releases.

## Publishing a pak (maintainer)

Once a contributed pak has passed the checks:

1. Name it `ServerAssets-<version>-CL-<changelist>.pak`.
2. Create a release tagged `cl-<changelist>`, titled `<version> (CL-<changelist>)`, and upload the pak to it. List the
   assets it contains in the release notes, and link the issue it came from.
3. Get its hash and size: `(Get-FileHash <pak> -Algorithm SHA256).Hash.ToLower()` and `(Get-Item <pak>).Length`.
4. Add its entry to `manifest.json`: version, pak, the release asset's download URL, sha256, size and contents. Commit
   it to `main`.

Hosts' launchers install the pak the next time they start a server for that build.

To replace a pak, upload the new file to the same release and update the entry's `url`, `sha256` and `size`.
Launchers then replace the installed copy. To withdraw a pak, remove its entry, and launchers remove the installed copy.

Game files never go in this repository's git history. They are only release assets, so the manifest and the
contribution forms can live on even if a release is taken down.
