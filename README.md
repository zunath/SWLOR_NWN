# Star Wars: Legends of the Old Republic

SWLOR is a Neverwinter Nights: Enhanced Edition persistent world. This repository contains its C# server, module content, and desktop toolset.

[Website](https://starwarsnwn.com/) · [Discord](https://discord.gg/MyQAM6m)

## Toolset users and content contributors

Download **SWLOR Toolset** from [Releases](https://github.com/zunath/SWLOR_NWN/releases/latest): the Windows installer or portable ZIP, or the Linux x64 AppImage or portable tar.gz. Choose an application asset rather than GitHub's source-code ZIP. On Linux, make the AppImage executable with `chmod +x *.AppImage`, then open it. Ubuntu 24.04 is the tested Linux baseline; use X11 or Wayland with XWayland.

1. Install and open SWLOR Toolset.
2. Select **Create SWLOR workspace** and choose a folder for your work.
3. Optionally select **Locate Neverwinter Nights** for base-game models and tiles.
4. Edit, save, and run **Tools → Run Validation**.
5. Use **File → Export Contribution** and send the ZIP to your SWLOR maintainer for review.

Git, Visual Studio, Docker, and the .NET SDK are not required for the installed toolset. The first workspace downloads SWLOR assets separately; later workspaces reuse them. Application updates preserve your editable files. **Help → Check for Updates** updates an installed Windows copy or Linux AppImage. Portable archives are updated by downloading a newer archive.

Conversations need a matching server build before they can be tested in game. Exporting a contribution lets the maintainer build and test the reviewed changes together.

The standalone downloads become available after the first toolset release is published. Until then, use the source setup below. See the [toolset guide](SWLOR.Toolset/README.md) for details.

## Source developers

Install [Git](https://git-scm.com/downloads), [Python 3.11 or newer](https://www.python.org/downloads/), and a stable [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0). Use an IDE that supports that SDK if you want code editing or debugging. Docker Engine with Compose on Linux, or Docker Desktop on Windows, is needed only to run a local server; NWN:EE supplies base-game assets and the game client.

Fork this repository, then clone your fork:

```powershell
git clone --recursive https://github.com/<your_username>/SWLOR_NWN.git
```

Double-click **Setup.cmd** inside the checkout. It initializes the pinned asset submodule and builds the toolset. No IDE setup or startup-project selection is needed.

- **CheckSetup.cmd** checks the source prerequisites without changing files or containers.
- **StartToolset.cmd** builds and opens the editor from source.
- **Setup.cmd -Profile Server** checks Docker and builds the local server launcher.
- **StartServer.cmd** packs the module, prepares this checkout's local server files, and runs the server. The first run can take a while. Press Ctrl+C to stop it.

On Linux, install these source-tool dependencies on Ubuntu 24.04:

```bash
sudo apt-get install python3 libpcre3 libsqlite3-0 libfontconfig1 libx11-6 libxrandr2 libxfixes3 libxi6 libxcursor1 libice6 libsm6 libglib2.0-0t64 libgl1 libgl1-mesa-dri
./Setup.sh
./StartToolset.sh
```

`./CheckSetup.sh`, `./Setup.sh --profile Server`, and `./StartServer.sh` provide the same checks and server workflow. Setup downloads checksum-verified Linux NWN utilities into the user's cache; it does not require PowerShell. Both platforms also support `python scripts/SetupRepository.py --mode Check --profile Server` (use `python3` on Linux).

Builds always disable automatic deployment. The server shortcut explicitly invokes the existing pack/deploy pipeline. Existing local configuration is preserved.

## Troubleshooting

**Missing assets or TLK:** run Setup.cmd or ./Setup.sh. It initializes the exact SWLOR_Haks revision pinned by the parent repository.

**SDK not found:** install a stable .NET 10 SDK and reopen your terminal or IDE. global.json selects the latest installed .NET 10 feature band.

**Docker unavailable:** start Docker Engine with Compose on Linux, or Docker Desktop with Linux containers on Windows. Run ./CheckSetup.sh --profile Server or CheckSetup.cmd -Profile Server.

**Missing HAK/TLK when joining a local server:** point your NWN profile's HAK and TLK aliases at this checkout's debugserver/hak and debugserver/tlk directories, or copy those built files into your profile.

**Contributor import conflict:** the target file has changed since the contributor's snapshot. A maintainer must review and merge it; imports never silently overwrite that newer work.

Ask for help on [Discord](https://discord.gg/MyQAM6m), including the setup error or the toolset's Output messages. Avoid deleting unrelated Docker containers, images, or game profiles.
