# Star Wars: Legends of the Old Republic

SWLOR is a Neverwinter Nights: Enhanced Edition persistent world. This repository contains its C# server, module content, and desktop toolset.

[Website](https://starwarsnwn.com/) · [Discord](https://discord.gg/MyQAM6m)

## Toolset users and content contributors

Use the **SWLOR Toolset installer** from [Releases](https://github.com/zunath/SWLOR_NWN/releases/latest). Choose the installer asset, rather than GitHub's source-code ZIP. A portable ZIP is also available for running without installation.

1. Install and open SWLOR Toolset.
2. Select **Create SWLOR workspace** and choose a folder for your work.
3. Optionally select **Locate Neverwinter Nights** for base-game models and tiles.
4. Edit, save, and run **Tools → Run Validation**.
5. Use **File → Export Contribution** and send the ZIP to your SWLOR maintainer for review.

Git, Visual Studio, Docker, and the .NET SDK are not required for the installed toolset. The first workspace downloads SWLOR assets separately; later workspaces reuse them. Application updates preserve your editable files. **Help → Check for Updates** updates an installed copy.

Conversations need a matching server build before they can be tested in game. Exporting a contribution lets the maintainer build and test the reviewed changes together.

The standalone downloads become available after the first toolset release is published. Until then, use the source setup below. See the [toolset guide](SWLOR.Toolset/README.md) for details.

## Source developers

Install [Git](https://git-scm.com/downloads/win) and a stable [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0). Use an IDE that supports that SDK if you want code editing or debugging. Docker Desktop is needed only to run a local server; NWN:EE supplies base-game assets and the game client.

Fork this repository, then clone your fork:

```powershell
git clone --recursive https://github.com/<your_username>/SWLOR_NWN.git
```

Double-click **Setup.cmd** inside the checkout. It initializes the pinned asset submodule and builds the toolset. No IDE setup or startup-project selection is needed.

- **CheckSetup.cmd** checks the source prerequisites without changing files or containers.
- **StartToolset.cmd** builds and opens the editor from source.
- **Setup.cmd -Profile Server** checks Docker and builds the local server launcher.
- **StartServer.cmd** packs the module, prepares this checkout's local server files, and runs the server. The first run can take a while. Press Ctrl+C to stop it.

For command-line use:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/SetupRepository.ps1 -Mode Check -Profile Server
```

Builds always disable automatic deployment. The server shortcut explicitly invokes the existing pack/deploy pipeline. Existing local configuration is preserved.

## Troubleshooting

**Missing assets or TLK:** run Setup.cmd. It initializes the exact SWLOR_Haks revision pinned by the parent repository.

**SDK not found:** install a stable .NET 10 SDK and reopen your terminal or IDE. global.json selects the latest installed .NET 10 feature band.

**Docker unavailable:** start Docker Desktop, select Linux containers, and run CheckSetup.cmd -Profile Server.

**Missing HAK/TLK when joining a local server:** point your NWN profile's HAK and TLK aliases at this checkout's debugserver/hak and debugserver/tlk directories, or copy those built files into your profile.

**Contributor import conflict:** the target file has changed since the contributor's snapshot. A maintainer must review and merge it; imports never silently overwrite that newer work.

Ask for help on [Discord](https://discord.gg/MyQAM6m), including the setup error or the toolset's Output messages. Avoid deleting unrelated Docker containers, images, or game profiles.
