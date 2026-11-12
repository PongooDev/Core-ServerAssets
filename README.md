# Core-ServerAssets

Server assets for [Core](https://github.com/PongooDev/CoreV2), the Fortnite private server: one pak per Fortnite
build, holding the game data a server needs, taken from a console build that still has what the PC game strips.

The paks are published as [releases](https://github.com/PongooDev/Core-ServerAssets/releases) of this repository.
[`manifest.json`](manifest.json) says which pak belongs to which build.

**Published so far:** [1.11 (CL-3807424)](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-3807424),
[2.2 (CL-3841827)](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-3841827),
[2.4.2 (CL-3870737)](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-3870737),
[2.5 (CL-3889387)](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-3889387),
[3.0 (CL-3901517)](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-3901517),
[3.1.0 (CL-3915963)](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-3915963),
[3.1.1 (CL-3917250)](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-3917250),
[3.2 (CL-3935073)](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-3935073),
[3.3 (CL-3942182)](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-3942182),
[3.5.1 (CL-4000805)](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4000805),
[3.5.2 (CL-4008490)](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4008490),
[3.6 (CL-4019403)](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4019403),
[4.0 (CL-4039451)](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4039451),
[4.1 (CL-4053532)](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4053532),
[4.2 (CL-4072250)](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4072250),
[4.4.0 (CL-4117433)](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4117433),
[4.4.1 (CL-4127312)](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4127312),
[4.4.2 (CL-4132537)](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4132537),
[4.5 (CL-4159770)](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4159770),
[5.00 (CL-4204761)](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4204761),
[5.0.1 (CL-4214610)](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4214610),
[5.10 (CL-4225813)](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4225813),
[5.10 (CL-4240749)](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4240749),
[5.21 (CL-4288479)](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4288479),
[5.30 (CL-4305896)](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4305896),
[5.40 (CL-4352937)](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4352937),
[5.41 (CL-4363240)](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4363240),
[6.00 (CL-4395664)](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4395664),
[6.00 (CL-4402180)](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4402180),
[6.01 (CL-4417689)](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4417689),
[6.02 (CL-4442095)](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4442095),
[6.10 (CL-4464155)](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4464155),
[6.10.1 (CL-4476098)](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4476098),
[6.20 (CL-4497486)](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4497486),
[6.21 (CL-4526925)](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4526925),
[6.22 (CL-4543176)](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4543176),
[6.30 (CL-4573096)](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4573096) and
[6.31 (CL-4573279)](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4573279), each with `AIDirector_Fortnite`, `Athena_LocationVolumes` and `Athena_GameplayActors`. 4.0's also has Save the World's loot tables and `LootQuotaData`. Paks are wanted for every build, including full extractions to replace these early hand-picked ones. If you have a console build, see
[Making a pak with the extractor](#making-a-pak-with-the-extractor).

## Contents

- [What server assets are](#what-server-assets-are)
- [How a pak is used](#how-a-pak-is-used)
- [Server assets to get](#server-assets-to-get)
- [Published paks](#published-paks)
- [Making a pak with the extractor](#making-a-pak-with-the-extractor)
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

A pak is now a **full data extraction** of a console build, made with
[ServerAssetsExtractor](#making-a-pak-with-the-extractor). It holds every data asset of the build that a dedicated
server could read (item and cosmetic definitions, loot, playlists, game data, AI, quests, abilities, building data,
levels with their gameplay actors and navigation data, server-side Blueprints), not a hand-picked list. That way
server-only data nobody has found yet comes along too.

Server-only content found so far includes `AIDirector_Fortnite` (1.10 and later), the POI volumes in
`Athena_LocationVolumes` and `Athena_GameplayActors` (3.0 and later), 4.0's Save the World loot tables
(`LootTierData_Client`, `LootPackages_Client`), `LootQuotaData`, and the server loot tier data. A full extraction
carries all of them. If you find server-only content the extractor leaves out,
[open an issue](https://github.com/PongooDev/Core-ServerAssets/issues/new?template=missing_server_asset.yml).

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
| 3.6 | 4019403 |
| 4.0 | 4039451 |
| 4.1 | 4053532 |

## Published paks

| Build | Release | Contains | Still wanted for this build |
| --- | --- | --- | --- |
| 1.11 (CL-3807424) | [`cl-3807424`](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-3807424), pak and sig | Full data extraction, with `AIDirector_Fortnite`, `LootTierData_Client`, `LootPackages_Client`, `LootQuotaData` | nothing known (`Athena_LocationVolumes` and `Athena_GameplayActors` are only needed from 3.0); the pak's `DefaultEngine.ini` has no `VersionDisplayName`, so its version is unconfirmed from it |
| 2.2 (CL-3841827) | [`cl-3841827`](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-3841827), pak and sig | Full data extraction, with `AIDirector_Fortnite`, `LootTierData_Client`, `LootPackages_Client`, `LootQuotaData` | nothing known (`Athena_LocationVolumes` and `Athena_GameplayActors` are only needed from 3.0); the pak's `DefaultEngine.ini` has no `VersionDisplayName`, so its version is unconfirmed from it |
| 2.4.2 (CL-3870737) | [`cl-3870737`](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-3870737), pak and sig | Full data extraction, with `AIDirector_Fortnite`, `LootTierData_Client`, `LootPackages_Client`, `LootQuotaData` | nothing known (`Athena_LocationVolumes` and `Athena_GameplayActors` are only needed from 3.0); from a PlayStation 4 build, checked against the Windows 2.4.2 paks (same package format, nearly all files byte-identical), not tested on a server; the pak's `DefaultEngine.ini` has no `VersionDisplayName`, so its version is unconfirmed from it |
| 2.5 (CL-3889387) | [`cl-3889387`](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-3889387), pak and sig | Full data extraction, with `AIDirector_Fortnite`, `Athena_GameplayActors`, `LootTierData_Client`, `LootPackages_Client`, `LootQuotaData` | nothing known (`Athena_LocationVolumes` is only needed from 3.0); from a PlayStation 4 build, checked against the Windows 2.5 paks (same package format, nearly all files byte-identical), not tested on a server |
| 3.0 (CL-3901517) | [`cl-3901517`](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-3901517), pak and sig | `AIDirector_Fortnite`, `Athena_LocationVolumes`, `Athena_GameplayActors` | the server loot tier data, `LootQuotaData` |
| 3.1.0 (CL-3915963) | [`cl-3915963`](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-3915963), pak and sig | `AIDirector_Fortnite`, `Athena_LocationVolumes`, `Athena_GameplayActors` | the server loot tier data, `LootQuotaData` |
| 3.1.1 (CL-3917250) | [`cl-3917250`](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-3917250), pak and sig | `AIDirector_Fortnite`, `Athena_LocationVolumes`, `Athena_GameplayActors` | the server loot tier data, `LootQuotaData` |
| 3.2 (CL-3935073) | [`cl-3935073`](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-3935073), pak and sig | `AIDirector_Fortnite`, `Athena_LocationVolumes`, `Athena_GameplayActors` | the server loot tier data, `LootQuotaData` |
| 3.3 (CL-3942182) | [`cl-3942182`](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-3942182), pak and sig | `AIDirector_Fortnite`, `Athena_LocationVolumes`, `Athena_GameplayActors` | the server loot tier data, `LootQuotaData` |
| 3.5.1 (CL-4000805) | [`cl-4000805`](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4000805), pak and sig | `AIDirector_Fortnite`, `Athena_LocationVolumes`, `Athena_GameplayActors` | the server loot tier data, `LootQuotaData` |
| 3.5.2 (CL-4008490) | [`cl-4008490`](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4008490), pak and sig | `AIDirector_Fortnite`, `Athena_LocationVolumes`, `Athena_GameplayActors` | the server loot tier data, `LootQuotaData` |
| 3.6 (CL-4019403) | [`cl-4019403`](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4019403), pak and sig | `AIDirector_Fortnite`, `Athena_LocationVolumes`, `Athena_GameplayActors` | the server loot tier data, `LootQuotaData` |
| 4.0 (CL-4039451) | [`cl-4039451`](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4039451), pak and sig | `AIDirector_Fortnite`, `Athena_LocationVolumes`, `Athena_GameplayActors`, `LootTierData_Client`, `LootPackages_Client`, `LootQuotaData` | the server loot tier data |
| 4.1 (CL-4053532) | [`cl-4053532`](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4053532), pak and sig | Full data extraction, with `AIDirector_Fortnite`, `Athena_LocationVolumes`, `Athena_GameplayActors`, `LootTierData_Client`, `LootPackages_Client`, `LootQuotaData` | the server loot tier data |
| 4.2 (CL-4072250) | [`cl-4072250`](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4072250), pak and sig | Full data extraction, with `AIDirector_Fortnite`, `Athena_LocationVolumes`, `Athena_GameplayActors`, `LootTierData_Client`, `LootPackages_Client`, `LootQuotaData` | the server loot tier data |
| 4.4.0 (CL-4117433) | [`cl-4117433`](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4117433), pak and sig | Full data extraction, with `AIDirector_Fortnite`, `Athena_LocationVolumes`, `Athena_GameplayActors`, `LootTierData_Client`, `LootPackages_Client`, `LootQuotaData` | the server loot tier data |
| 4.4.1 (CL-4127312) | [`cl-4127312`](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4127312), pak and sig | Full data extraction, with `AIDirector_Fortnite`, `Athena_LocationVolumes`, `Athena_GameplayActors`, `LootTierData_Client`, `LootPackages_Client`, `LootQuotaData` | the server loot tier data; the pak's `DefaultEngine.ini` still reads 4.4.0, as the build ships it |
| 4.4.2 (CL-4132537) | [`cl-4132537`](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4132537), pak and sig | Full data extraction, with `AIDirector_Fortnite`, `Athena_LocationVolumes`, `Athena_GameplayActors`, `LootTierData_Client`, `LootPackages_Client`, `LootQuotaData` | the server loot tier data; the pak's `DefaultEngine.ini` reads 4.4.0, as the build ships it |
| 4.5 (CL-4159770) | [`cl-4159770`](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4159770), pak and sig | Full data extraction, with `AIDirector_Fortnite`, `Athena_LocationVolumes`, `Athena_GameplayActors`, `LootTierData_Client`, `LootPackages_Client`, `LootQuotaData` | the server loot tier data |
| 5.00 (CL-4204761) | [`cl-4204761`](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4204761), pak and sig | Full data extraction, with `AIDirector_Fortnite`, `Athena_LocationVolumes`, `Athena_GameplayActors`, `LootTierData_Client`, `LootPackages_Client`, `LootQuotaData` | the server loot tier data; from a PlayStation 4 build, so the CL is the archived PC build's of the same version, and Core doesn't support Season 5 yet, so it's untested on a server |
| 5.0.1 (CL-4214610) | [`cl-4214610`](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4214610), pak and sig | The same pak as 5.00 (CL-4204761), byte for byte, under its own CL | the CL is the archived PC build's of the same version, not tested on a server |
| 5.10 (CL-4225813) | [`cl-4225813`](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4225813), pak and sig | The same pak as 5.10 (CL-4240749), byte for byte, under its own CL | the CL is the archived PC build's of the same version, not tested on a server |
| 5.10 (CL-4240749) | [`cl-4240749`](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4240749), pak and sig | Full data extraction, with `AIDirector_Fortnite`, `Athena_LocationVolumes`, `Athena_GameplayActors`, `LootTierData_Client`, `LootPackages_Client`, `LootQuotaData` | the server loot tier data; from a PlayStation 4 build, so the CL is the archived PC build's of the same version, and Core doesn't support Season 5 yet, so it's untested on a server |
| 5.21 (CL-4288479) | [`cl-4288479`](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4288479), pak and sig | Full data extraction, with `AIDirector_Fortnite`, `Athena_LocationVolumes`, `Athena_GameplayActors`, `LootTierData_Client`, `LootPackages_Client`, `LootQuotaData` | the server loot tier data; from a PlayStation 4 build, so the CL is the archived PC build's of the same version, and Core doesn't support Season 5 yet, so it's untested on a server |
| 5.30 (CL-4305896) | [`cl-4305896`](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4305896), pak and sig | Full data extraction, with `AIDirector_Fortnite`, `Athena_LocationVolumes`, `Athena_GameplayActors`, `LootTierData_Client`, `LootPackages_Client`, `LootQuotaData` | the server loot tier data; from a PlayStation 4 build, so the CL is the archived PC build's of the same version, and Core doesn't support Season 5 yet, so it's untested on a server |
| 5.40 (CL-4352937) | [`cl-4352937`](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4352937), pak and sig | Full data extraction, with `AIDirector_Fortnite`, `Athena_LocationVolumes`, `Athena_GameplayActors`, `LootTierData_Client`, `LootPackages_Client`, `LootQuotaData` | the server loot tier data; from a PlayStation 4 build, so the CL is the archived PC build's of the same version, and Core doesn't support Season 5 yet, so it's untested on a server |
| 5.41 (CL-4363240) | [`cl-4363240`](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4363240), pak and sig | Full data extraction, with `AIDirector_Fortnite`, `Athena_LocationVolumes`, `Athena_GameplayActors`, `LootTierData_Client`, `LootPackages_Client`, `LootQuotaData` | the server loot tier data; from a PlayStation 4 build, so the CL is the archived PC build's of the same version, and Core doesn't support Season 5 yet, so it's untested on a server |
| 6.00 (CL-4395664) | [`cl-4395664`](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4395664), pak and sig | Full data extraction, with `AIDirector_Fortnite`, `Athena_LocationVolumes`, `Athena_GameplayActors`, `LootTierData_Client`, `LootPackages_Client`, `LootQuotaData` | the server loot tier data; from a PlayStation 4 build, so the CL is the archived PC build's of the same version, and Core doesn't support Season 6 yet, so it's untested on a server |
| 6.00 (CL-4402180) | [`cl-4402180`](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4402180), pak and sig | The same pak as 6.00 (CL-4395664), byte for byte, under its own CL: a full data extraction, including `BP_IslandServerOnly`, the floating island's server half | the server loot tier data; checked against the Windows CL-4402180 paks (same package format; 62,994 of its 63,101 files byte-identical) and boot-tested on a CL-4402180 server. Two Shadow Stone Blueprints (`DA_SpookyMist`, `GA_SpookyMist_PassiveSetup`) are the earlier revision: CL-4402180's own copies add a knocked-down (DBNO) check that the pak's copies replace |
| 6.01 (CL-4417689) | [`cl-4417689`](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4417689), pak and sig | Full data extraction, with `AIDirector_Fortnite`, `Athena_LocationVolumes`, `Athena_GameplayActors`, `LootTierData_Client`, `LootPackages_Client`, `LootQuotaData` | the server loot tier data; from a PlayStation 4 build, so the CL is the archived PC build's of the same version, and Core doesn't support Season 6 yet, so it's untested on a server |
| 6.02 (CL-4442095) | [`cl-4442095`](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4442095), pak and sig | Full data extraction, with `AIDirector_Fortnite`, `Athena_LocationVolumes`, `Athena_GameplayActors`, `LootTierData_Client`, `LootPackages_Client`, `LootQuotaData` | the server loot tier data; from a PlayStation 4 build, so the CL is the archived PC build's of the same version, and Core doesn't support Season 6 yet, so it's untested on a server |
| 6.10 (CL-4464155) | [`cl-4464155`](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4464155), pak and sig | Full data extraction, with `AIDirector_Fortnite`, `Athena_LocationVolumes`, `Athena_GameplayActors`, `LootTierData_Client`, `LootPackages_Client`, `LootQuotaData` | the server loot tier data; from a PlayStation 4 build, so the CL is the archived PC build's of the same version, and Core doesn't support Season 6 yet, so it's untested on a server |
| 6.10.1 (CL-4476098) | [`cl-4476098`](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4476098), pak and sig | Full data extraction, with `AIDirector_Fortnite`, `Athena_LocationVolumes`, `Athena_GameplayActors`, `LootTierData_Client`, `LootPackages_Client`, `LootQuotaData` | the server loot tier data; from a PlayStation 4 build, so the CL is the archived PC build's of the same version, and Core doesn't support Season 6 yet, so it's untested on a server |
| 6.20 (CL-4497486) | [`cl-4497486`](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4497486), pak and sig | Full data extraction, with `AIDirector_Fortnite`, `Athena_LocationVolumes`, `Athena_GameplayActors`, `LootTierData_Client`, `LootPackages_Client`, `LootQuotaData` | the server loot tier data; from a PlayStation 4 build, so the CL is the archived PC build's of the same version, and Core doesn't support Season 6 yet, so it's untested on a server |
| 6.21 (CL-4526925) | [`cl-4526925`](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4526925), pak and sig | Full data extraction, with `AIDirector_Fortnite`, `Athena_LocationVolumes`, `Athena_GameplayActors`, `LootTierData_Client`, `LootPackages_Client`, `LootQuotaData` | the server loot tier data; from a PlayStation 4 build, so the CL is the archived PC build's of the same version, and Core doesn't support Season 6 yet, so it's untested on a server |
| 6.22 (CL-4543176) | [`cl-4543176`](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4543176), pak and sig | Full data extraction, with `AIDirector_Fortnite`, `Athena_LocationVolumes`, `Athena_GameplayActors`, `LootTierData_Client`, `LootPackages_Client`, `LootQuotaData` | the server loot tier data; from a PlayStation 4 build, so the CL is the archived PC build's of the same version, and Core doesn't support Season 6 yet, so it's untested on a server |
| 6.30 (CL-4573096) | [`cl-4573096`](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4573096), pak and sig | Full data extraction, with `AIDirector_Fortnite`, `Athena_LocationVolumes`, `Athena_GameplayActors`, `LootTierData_Client`, `LootPackages_Client`, `LootQuotaData` | the server loot tier data; from a PlayStation 4 build, so the CL is the archived PC build's of the same version, and Core doesn't support Season 6 yet, so it's untested on a server |
| 6.31 (CL-4573279) | [`cl-4573279`](https://github.com/PongooDev/Core-ServerAssets/releases/tag/cl-4573279), pak and sig | Full data extraction, with `AIDirector_Fortnite`, `Athena_LocationVolumes`, `Athena_GameplayActors`, `LootTierData_Client`, `LootPackages_Client`, `LootQuotaData` | the server loot tier data; from a PlayStation 4 build, so the CL is the archived PC build's of the same version, and Core doesn't support Season 6 yet, so it's untested on a server |

The first 3.0 pak ([#1](https://github.com/PongooDev/Core-ServerAssets/issues/1)) only had `Athena_LocationVolumes`,
because it came from a PS4 patch PKG, which carries only the changed blocks. MrPodge6's pak
([#2](https://github.com/PongooDev/Core-ServerAssets/issues/2)), from a PS4 3.0 build, replaced it with all three
assets. Only the server loot tier data and `LootQuotaData` are still wanted for 3.0.

MrPodge6's 3.1.1 pak ([#8](https://github.com/PongooDev/Core-ServerAssets/issues/8)) is byte-identical to the 3.1.0 one,
so 3.1.1 did not change these assets. Paks are per changelist and a launcher installs a pak only for an exact
changelist match, so 3.1.1 has its own release and manifest entry.

90sNath's 4.0 pak ([#10](https://github.com/PongooDev/Core-ServerAssets/issues/10)), from a PS4 4.0 build, is the first with Save the World's loot tables (`LootTierData_Client`, `LootPackages_Client`) and `LootQuotaData`. Only the server loot tier data is still wanted for 4.0.

## Making a pak with the extractor

ServerAssetsExtractor reads a Fortnite build's paks and writes a ready-to-submit server-assets pak of its data. It
only reads the build; it never changes it.

**The rule it follows: don't take anything a dedicated server obviously won't need.** When in doubt, it keeps a
pure-data asset and drops a presentation one.

### Step by step

1. Download `ServerAssetsExtractor.exe` from the
   [extractor release](https://github.com/PongooDev/Core-ServerAssets/releases/tag/extractor-v2). It is a single
   Windows exe; you don't need .NET or anything else.
2. Get a console build of the **same version and changelist** as a PC build Core supports (see
   [the list](#server-assets-to-get)): a PlayStation 4 build for Season 12 and earlier, or an Xbox build for any
   season. It must be the full build, not a patch PKG (a patch only carries the changed blocks). Unpacking the
   console package is up to you; [ServerAssets-OGFN](https://github.com/MarvelcoCode/ServerAssets-OGFN) explains how.
   Console patch numbers aren't Fortnite version numbers: PS4 patch 01.30 is Fortnite 1.9, for example.
3. Double-click `ServerAssetsExtractor.exe`. When it asks, drag the build's folder (the one with `FortniteGame` in
   it, or its `Paks` folder) into the window and press Enter. You can also drop the folder onto the exe.
4. Wait. It finds the paks, the changelist, the engine version and the AES key by itself where it can, then reads
   every package (under a minute on a fast disk) and writes the pak (about 370 MB for a Season 4 build).
5. It checks the pak it made and ends with **Done**. Next to the exe there is now a
   `ServerAssets-<version>-CL-<changelist>` folder with four files:
   - `ServerAssets-<version>-CL-<changelist>.pak`, the pak
   - `ServerAssets-<version>-CL-<changelist>.sig`, its signature file
   - `contents.txt`, every package in the pak
   - `report.txt`, what was kept and left out, and by which rule
6. Submit them: see [Contributing a pak](#contributing-a-pak).

### The AES key

Fortnite's paks are encrypted, and a console build uses the same key as the PC build of the same version. The
extractor looks for it in this order:

1. `--aes <64 hex digits>`, if you give it;
2. [FModel](https://fmodel.app)'s settings (`%APPDATA%\FModel\AppSettings.json`): the key FModel has for that build's
   folder, or else any key FModel knows that opens the paks (so having the matching PC build set up in FModel is
   enough);
3. it asks you to paste it (what you type isn't shown).

The key is never printed, logged or written to any file. Don't post it in an issue.

### What it includes

It reads the header of every package and judges it by its main export's class:

- **Kept:** data tables and curve tables, item and cosmetic definitions and every other data asset, playlists, game
  data, loot, AI (behavior trees, EQS, the AI director), quests, abilities and gameplay effects, building data,
  server-side Blueprints, the levels listed in `keepLevelPaths` and every level with navigation data (see below),
  nav areas and nav filters, `*_BuiltData` of the levels kept, string tables, and the platform-neutral `Default*.ini` and `Base*.ini` config.
- **Left out:** maps (see below), meshes, textures, materials, animations, physics assets, audio, VFX and particles, camera shakes,
  fonts, movies and media, UI (widgets, HUD, icons, brushes, everything under `/UI/`), the frontend and lobby, HLOD
  content, editor-only engine content, platform-named assets (PS4, Xbox and the like), input, scalability and
  device-profile config, shader caches, and bulk data (`.ubulk`/`.uptnl`).

Console-only and render-only content is left out on purpose. The pak mounts over the game's own files, on the server
and on a player's game that shares the build folder, so console UI or HLODs would replace the PC ones. Leaving a
package out never removes it: the PC paks' own copy is used. Don't add that content back. A level that a rule would
drop is still kept when it holds a navmesh or gameplay actors, so only render-only levels such as HLOD sublevels go.

**Maps are left out by default.** A dedicated server loads its maps from the game's own paks, and a console copy of a
whole map would replace the PC map on a shared install. On the 4.0 PC build, 1,086 maps (such as `Athena_Terrain`)
were 568 MB of the pak. Two kinds of level are kept anyway: the levels in `keepLevelPaths` in `rules.json`
(`Athena_LocationVolumes` and `Athena_GameplayActors`, which carry server-only data the PC cook strips; add a
`/Game/...` level name there to keep another), and **every level with navigation data** (a `RecastNavMesh`,
`FortNavMesh` or other `NavigationData` actor), together with its `_BuiltData`. Levels with navigation data are
always kept. To keep every map, pass `--include-maps` or set `"includeMaps": true` in `rules.json`. `report.txt` says
how many maps were left out and their size.

The rules are in `rules.json` next to the exe; each list is explained at its top. `report.txt` shows, per rule, how
many packages it dropped with an example, and lists the data classes no rule names yet. Packages of an unlisted
class are kept.

Navigation data: the PC 4.0 build has navmesh tile data in only a few Save the World levels (`Tile_Suburban_002`
has about 1.2 MB of it; the rest a few KB each) and none in the Battle Royale map. So a console extraction is the
only source of any navmesh data the PC build strips. `report.txt` lists every navmesh actor and its size.

### Options

Run `ServerAssetsExtractor --help` for all of them. The useful ones:

| Option | What it does |
| --- | --- |
| `--out <folder>` | Where the files go: an empty or new folder, never inside the build |
| `--dry-run` | Only list and count. With `--out`, it writes `contents.txt` and `report.txt` but no pak |
| `--ue 4.19` or `--ue 4.20` | The engine version, if it can't tell: 4.19 for 1.7.2 to 2.4.2, 4.20 for 2.5 to 4.x |
| `--aes <hex>` | The AES key |
| `--cl <changelist>` | The changelist, if it can't find it (it names the files) |
| `--include <prefix>`, `--exclude <prefix>` | Only, or not, pak paths starting with this, e.g. `FortniteGame/Content/` |
| `--max-size <size>` | Leave out any package bigger than this, e.g. `200MB` |
| `--include-maps` | Keep every map (they are left out by default, except the `keepLevelPaths` levels and levels with a navmesh) |
| `--max-map-size <size>` | Leave out levels bigger than this, unless they hold a navmesh |
| `--rules <file>` | Use another rules file |
| `verify <pak>` | Check a pak: version 3, mount point `../../../`, entries and hashes, that CUE4Parse reads it, its `.sig` and `contents.txt` |

For a submission, use the defaults: no `--include`, `--exclude`, size limits or edited rules.

### Adding assets to a published pak

If someone finds that one more map or data package is needed, the pak doesn't have to be extracted again.
A contributor attaches (or links, for big files) the loose `.uasset`, `.uexp`, `.umap` and `.ubulk` files on a
["Server assets pak" issue](https://github.com/PongooDev/Core-ServerAssets/issues/new/choose), in a folder laid out
like the game (`FortniteGame/Content/...`), which is what FModel's **Export Raw Data** produces from the console
build. A maintainer then runs:

```
ServerAssetsExtractor add <existing.pak> <files or folders...> [--out new.pak] [--force] [--ue 4.20]
```

- Folder arguments give each file's pak path from the folder structure. A single file can be passed as
  `<file>=FortniteGame/Content/...` (or sit inside a `FortniteGame/` folder).
- The result is a new pak with the same format and `../../../` mount point (default `added/<name>.pak` next to the
  old pak, never the old pak itself). A path that is already in the pak is replaced; others are added. The `.sig` is
  rebuilt, `contents.txt` is written beside the new pak, and `verify` runs at the end.
- Paths outside `FortniteGame/` and `Engine/` are refused.
- Anything the default rules leave out (UI, textures and the like) gets a warning and is not added, unless `--force`
  is given. Maps are never refused for being maps, so a map can be added this way.
- Then the maintainer republishes the pak (new size and SHA-256 in `manifest.json`).

### Troubleshooting

- **"no .pak files were found"**: point it at the folder that holds `FortniteGame`, or at `FortniteGame\Content\Paks`.
  An unpacked console build often has its paks a few folders down; the extractor looks up to six levels deep.
- **"The AES key ... doesn't open these paks"** or **"no AES key opened them"**: the key is for another version. Use
  the key of the PC build of exactly the same version, or set that PC build up in FModel first.
- **"Not mounted: ..."** for some paks: those need a key the extractor doesn't know (a dynamic key). FModel's
  dynamic keys for the build are used when it has them; otherwise those paks' files are left out.
- **"No pak could be mounted"**, or most packages unreadable in `report.txt`: the engine version is wrong. Pass
  `--ue 4.19` or `--ue 4.20`.
- **The final check shows FAIL**: don't submit the pak. Open an issue with `report.txt` instead.
- **Windows warns about the exe**: choose More info, then Run anyway. It isn't signed.

### Building it (developers)

The source is in [`tools/ServerAssetsExtractor`](tools/ServerAssetsExtractor). It needs the .NET 10 SDK and uses
[CUE4Parse](https://github.com/FabianFG/CUE4Parse), pinned in the `.csproj`.

- Run from source: `dotnet run -c Release --project tools/ServerAssetsExtractor -- <build folder> --dry-run`
- Release exe: `dotnet publish tools/ServerAssetsExtractor -c Release -r win-x64 -o <folder>` makes one
  self-contained `ServerAssetsExtractor.exe`, with `rules.json` beside it (the exe also carries a built-in copy).

## Contributing a pak

1. Make the pak with [the extractor](#making-a-pak-with-the-extractor), from a console build of the **same version
   and changelist** as a PC build Core supports. Check the changelist it prints; it reads the build's
   `Engine/Build/Build.version` when there is one.
2. Test it if you can: put the pak and sig in a PC build's `FortniteGame\Content\Paks` and host a server. The
   server log says when Core uses a real asset instead of its stand-in.
3. Share the `.pak` and `.sig`. GitHub issues only take attachments up to 25 MB, so a pak (hundreds of MB) can't be
   attached there, zipped or not. **Preferred: a release on your own fork**, which the maintainer can download
   straight away:
   1. [Fork this repository](https://github.com/PongooDev/Core-ServerAssets/fork) (the **Fork** button, top right).
   2. On your fork, open **Releases** (right-hand column of the repository page), then **Draft a new release**.
   3. Under **Choose a tag**, type a new tag, such as `cl-<changelist>`, and choose **Create new tag**. The tag
      name doesn't matter.
   4. Drag the `.pak` and `.sig` into the **Attach binaries** box and wait for the uploads to finish. GitHub allows
      up to 2 GB per file. Don't zip them.
   5. Click **Publish release**. It has to be published, not saved as a draft, or nobody else can download it.
   6. Copy the release's page link for the issue.

   **Fallback:** a host with direct downloads, such as [pixeldrain.com](https://pixeldrain.com) or a Dropbox link
   (set it to "anyone with the link can view"). gofile and MEGA block automated downloads, so the maintainer has to
   download those by hand, and publishing may take longer.
4. [Open a "Server assets pak" issue](https://github.com/PongooDev/Core-ServerAssets/issues/new?template=server_assets_pak.yml)
   with the release link (or links), the extractor's version, and `contents.txt` and `report.txt` attached (those
   two are small). Once the pak is published as a release here, you can delete your fork's release or upload.

**What gets checked before a pak is published:**
- it is for the right changelist
- it was made by the extractor with its default rules (`verify` passes, and `contents.txt` matches)
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

Fetch the pak from the issue first: a fork's release link downloads directly (`gh release download <tag> --repo <fork>`),
as does a pixeldrain or Dropbox link, while gofile and MEGA links have to be downloaded by hand. Once a contributed pak
has passed the checks (`ServerAssetsExtractor verify <pak>` checks its format, sig and `contents.txt`):

1. Name it `ServerAssets-<version>-CL-<changelist>.pak`, and its sig, if it has one, `ServerAssets-<version>-CL-<changelist>.sig`.
2. Create a release tagged `cl-<changelist>`, titled `<version> (CL-<changelist>)`, and upload the pak (and sig) to
   it. List the assets it contains in the release notes, and link the issue it came from.
3. Get the hash and size of each file: `(Get-FileHash <file> -Algorithm SHA256).Hash.ToLower()` and
   `(Get-Item <file>).Length`.
4. Add its entry to `manifest.json`: version, pak, the release asset's download URL, sha256, size and contents (the
   lines of the extractor's `contents.txt`), plus
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
