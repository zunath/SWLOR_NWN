# SWLOR Docker Image Generation

This document explains how to generate a new Docker image with the latest NWN and NWNXEE versions for the SWLOR project.

## Overview

The SWLOR Docker setup uses a custom image based on the NWNXEE unified image. When new versions of NWN or NWNXEE are released, you need to generate a new image to stay up-to-date.

## Prerequisites

- Docker installed and running
- Access to the SWLOR project repository
- Basic knowledge of Docker commands

## Current Image Information

- **Base Image**: `nwnxee/unified:latest`
- **Custom Image**: `zunath/nwn-dotnet:8193.37.3-1`
- **Features**: Includes Visual Studio debugger for remote debugging

## Step-by-Step Process

### 1. Check Current NWN Version

First, check what version of NWN.Native the project is using:

```bash
# Check the project file for the current NWN.Native version
grep "NWN.Native" ../SWLOR.Game.Server.csproj
```

The version should match the NWN.Native package reference in the project file.

### 2. Navigate to Docker Directory

```bash
cd SWLOR.Game.Server/Docker
```

### 3. Pull Latest NWNXEE Unified Image

```bash
docker pull nwnxee/unified:latest
```

This ensures you have the latest NWNXEE unified image as the base.

### 4. Build New Custom Image

Create a new image with an appropriate version tag:

```bash
# Format: zunath/nwn-dotnet:{NWN_VERSION}-{BUILD_NUMBER}
docker build -t zunath/nwn-dotnet:8193.37.3-1 .
```

**Version Naming Convention:**
- Use the NWN.Native package version from the project file
- Add a build number (e.g., -1, -2) for multiple builds of the same version
- Example: `8193.37.3-1` for NWN version 8193.37.3, build 1

### 5. Verify the New Image

```bash
# List all zunath/nwn-dotnet images
docker images zunath/nwn-dotnet

# Test the new image
docker run --rm zunath/nwn-dotnet:8193.37.3-1 /nwn/bin/linux-x86/nwserver-linux --help
```

### 6. Update Docker Compose File

Edit `docker-compose.yml` to use the new image:

```yaml
swlor-server:
    hostname: nwnx-server
    stop_signal: SIGINT
    image: zunath/nwn-dotnet:8193.37.3-1  # Update this line
    env_file: ${PWD-.}/swlor.env
    # ... rest of configuration
```

### 7. Test the Updated Setup

```bash
# Start the services with the new image
docker-compose up -d

# Check logs to ensure everything is working
docker-compose logs swlor-server
```

## Dockerfile Details

The Dockerfile (`Dockerfile`) contains:

```dockerfile
FROM nwnxee/unified:latest
RUN wget https://aka.ms/getvsdbgsh -O - 2>/dev/null | /bin/sh /dev/stdin -v vs2019 -l /remote_debugger/vsdbg
RUN chmod 777 -R /remote_debugger/vsdbg
```

**What this does:**
- Uses the latest NWNXEE unified image as base
- Installs Visual Studio debugger for remote debugging
- Sets proper permissions for the debugger directory

## Version History

| Version | NWN Version | Build Date | Notes |
|---------|-------------|------------|-------|
| 8193.37.3-1 | 8193.37.3 | Current | Latest NWNXEE unified image |
| 8193.35.40-2 | 8193.35.40 | Previous | Older version (swlor/server) |

## Troubleshooting

### Image Build Issues

If the build fails:

1. **Check Docker daemon is running:**
   ```bash
   docker info
   ```

2. **Clear Docker cache:**
   ```bash
   docker system prune -a
   ```

3. **Check network connectivity:**
   ```bash
   docker pull nwnxee/unified:latest
   ```

### Runtime Issues

If the server doesn't start:

1. **Check image exists:**
   ```bash
   docker images zunath/nwn-dotnet
   ```

2. **Verify docker-compose configuration:**
   ```bash
   docker-compose config
   ```

3. **Check container logs:**
   ```bash
   docker-compose logs swlor-server
   ```

## Best Practices

1. **Always test new images** before deploying to production
2. **Keep version history** for rollback purposes
3. **Document changes** when updating versions
4. **Use semantic versioning** for image tags
5. **Clean up old images** periodically to save disk space

## Cleanup

To remove old images:

```bash
# Remove specific old image
docker rmi zunath/nwn-dotnet:8193.35.40-2

# Remove all unused images
docker image prune -a
```

## Additional Resources

- [NWNXEE Documentation](https://nwnxee.github.io/)
- [Docker Documentation](https://docs.docker.com/)
- [SWLOR Project Repository](https://github.com/SWLOR/SWLOR)

---

**Note**: Always ensure compatibility between the NWN.Native package version in the project and the Docker image version before deploying to production. 