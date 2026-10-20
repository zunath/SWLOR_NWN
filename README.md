# Star Wars: Legends of the Old Republic

SWLOR is a Neverwinter Nights: Enhanced Edition persistent world. This repository holds its C# server, module content, and desktop toolset.

[Website](https://starwarsnwn.com/) · [Discord](https://discord.gg/MyQAM6m)

- **Edit content** (areas, blueprints, scripts): [install the toolset](#install-the-toolset).
- **Change server code or run a local server**: [set up the source checkout](#set-up-the-source-checkout).

## Install the toolset

You need nothing else installed: no Git, .NET SDK, Python, or Docker.

1. Download a package from [Releases](https://github.com/zunath/SWLOR_NWN/releases/latest): the Windows installer or portable ZIP, or the Linux x64 AppImage or tar.gz. Do not use GitHub's "Source code" ZIP.
2. Install and open SWLOR Toolset. On Linux, run `chmod +x *.AppImage` first.
3. Select **Create SWLOR workspace** and choose a folder for your work.
4. Optional: select **Locate Neverwinter Nights** for base-game models and tiles.
5. Edit, save, then run **Tools → Run Validation**.
6. Use **File → Export Contribution** and send the ZIP to a maintainer.

The first workspace downloads SWLOR assets; later workspaces reuse them. Updating the toolset keeps your files. **Help → Check for Updates** updates the installer and AppImage builds; for portable archives, download the newer archive. Linux builds are tested on Ubuntu 24.04 and need X11 or Wayland with XWayland.

Conversation changes need a matching server build before you can test them in game. The maintainer builds and tests your contribution.

No toolset release is published yet. Until one is, use the source checkout. More detail: [toolset guide](SWLOR.Toolset/README.md).

## Set up the source checkout

Windows:

1. Install [Git](https://git-scm.com/downloads) and a stable [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (10.0.100 or newer; release candidates do not work).
2. Fork this repository and clone your fork: `git clone https://github.com/<your_username>/SWLOR_NWN.git`
3. Run **Setup.cmd** in the checkout.

Linux (Ubuntu 24.04):

1. Install Git and the .NET SDK as above, then the libraries:
   `sudo apt-get install libpcre3 libsqlite3-0 libfontconfig1 libx11-6 libxrandr2 libxfixes3 libxi6 libxcursor1 libice6 libsm6 libglib2.0-0t64 libgl1 libgl1-mesa-dri`
2. Fork and clone as above.
3. Run `./Setup.sh` in the checkout.

Setup initializes the pinned `SWLOR_Haks` submodule, prepares the native NWN tools, and builds the toolset. On Linux it downloads checksum-verified tools into `~/.local/share/SWLOR.Toolset/NativeTools`. It needs no Python and no IDE.

| Windows | Linux | What it does |
|---|---|---|
| `Setup.cmd` | `./Setup.sh` | Prepares the checkout and builds the toolset |
| `CheckSetup.cmd` | `./CheckSetup.sh` | Lists missing prerequisites without changing submodules, native tools, or containers (builds the small checker on first run) |
| `StartToolset.cmd` | `./StartToolset.sh` | Builds and opens the toolset |
| `StartServer.cmd` | `./StartServer.sh` | Packs the module, deploys it, and runs a local server (slow the first time; Ctrl+C stops it) |

To run a local server, also install Docker Desktop (Linux containers) on Windows, or Docker Engine with the Compose plugin on Linux. Check it with `CheckSetup.cmd -Profile Server` or `./CheckSetup.sh --profile Server`. To join the server, you need NWN:EE.

## Troubleshooting

**"Install a stable .NET 10 SDK" or "compatible SDK not found":** `global.json` accepts only stable .NET 10 SDKs. Install one, reopen your terminal, and run `dotnet --version` in the checkout. It must print `10.x.x` without a `-rc` or `-preview` suffix.

**Missing assets or TLK:** run `Setup.cmd` or `./Setup.sh`.

**"The native tool cache is incomplete":** close the toolset, delete the folder named in the message, and rerun setup.

**Docker unavailable:** start Docker, then rerun the server check above.

**Missing HAK or TLK when joining a local server:** point your NWN profile's HAK and TLK paths at this checkout's `debugserver/hak` and `debugserver/tlk`, or copy those files into your profile.

Still stuck? Ask on [Discord](https://discord.gg/MyQAM6m) and include the setup error or the toolset's Output messages. Do not delete unrelated Docker containers, images, or game profiles.
