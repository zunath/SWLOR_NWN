FROM mcr.microsoft.com/dotnet/sdk:10.0-noble@sha256:e70cdb7f80b0348f5cb85f19a8f670fca061f033d57eed12fa003d58b0e06317
# Test-only tools; none of these packages are installed on the production host.
RUN apt-get update && apt-get install -y --no-install-recommends \
    jq unzip shellcheck python3 python3-yaml git && rm -rf /var/lib/apt/lists/*
WORKDIR /tmp
