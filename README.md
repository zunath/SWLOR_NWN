# Star Wars: Legends of the Old Republic
Server-side C# code used in the Neverwinter Nights Star Wars: Legends of the Old Republic module.

Game: Neverwinter Nights: Enhanced Edition

Website: https://starwarsnwn.com/

Discord: https://discord.gg/MyQAM6m

Forums: https://forums.starwarsnwn.com/

# Project Description

This project contains the C# source code used on the Star Wars: Legends of the Old Republic server. 

It serves as a replacement for NWScript and handles most server features and functions. This is possible by using the NWNX_DotNet plugin for NWNX.

Refer to the quick start guide below and be sure to post any issues on our forums. The link to the forums is above.

# Prerequisites: 

Git: https://git-scm.com/downloads
  
Docker: https://www.docker.com/products/docker-desktop/
  
Visual Studio 2022: https://www.visualstudio.com/downloads/

Neverwinter Nights: https://store.steampowered.com/app/704450/Neverwinter_Nights_Enhanced_Edition/

# Installation:

1. Fork this repo.

2. At command line: ``git clone --recursive https://github.com/<your_username>/SWLOR_NWN.git``
  
3. Open SWLOR.Game.Server.sln with Visual Studio
  
4. Right click on SWLOR.Runner within Visual Studio and select "Set as Startup Project"
  
5. Click the green run button at the top of Visual Studio. Note that this will take a while the first time you do it.

# Troubleshooting

**'TLK does not exist' error:**

This happens because you didn't clone with the --recursive command. Redo step 2.

**'Type initializer for Ductus.FluentDocker.Extensions.CommandExtensions threw an exception' error:**

You need to install docker.

# Development Guide

Please refer to this guide for setting up your development environment: https://wiki.starwarsnwn.com/en/Development/Environment-SetUp

# Getting Help

If you need help with anything related to Star Wars: Legends of the Old Republic please feel free to contact us on our Discord here: https://discord.gg/MyQAM6m

For NWNX and Docker related issues please look for help in the NWNX Discord channel here: https://discord.gg/m2hJPDE
