# Linux build environment for local toolset releases (tools/ToolsetDistribution.py release-local).
# It mirrors the ubuntu-24.04 packages that .github/workflows/release-toolset.yml installs.
FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:0d99ebf1ccad15037b5c98c9fe8979d6b8dbac23812feae2277a0b277b5c5ffa
ENV DEBIAN_FRONTEND=noninteractive DOTNET_NOLOGO=1 DOTNET_CLI_TELEMETRY_OPTOUT=1
RUN apt-get update -qq \
    && apt-get install -y --no-install-recommends python3 git ca-certificates curl \
        libpcre3 libsqlite3-0 libfuse2t64 libfontconfig1 libx11-6 libxrandr2 libxfixes3 libxi6 libxcursor1 \
        libice6 libsm6 libglib2.0-0t64 libgl1 libgl1-mesa-dri xvfb xauth x11-utils squashfs-tools file \
        desktop-file-utils patchelf \
    && rm -rf /var/lib/apt/lists/*
