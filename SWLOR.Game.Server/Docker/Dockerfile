FROM nwnxee/unified:build8193.37.15
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        dotnet-apphost-pack-10.0 \
        dotnet-runtime-10.0 \
    && apt-get purge -y \
        dotnet-apphost-pack-7.0 \
        dotnet-apphost-pack-8.0 \
        dotnet-hostfxr-7.0 \
        dotnet-hostfxr-8.0 \
        dotnet-runtime-7.0 \
        dotnet-runtime-8.0 \
        dotnet-runtime-deps-7.0 \
        dotnet-runtime-deps-8.0 \
    && rm -rf /var/lib/apt/lists/*

RUN wget https://aka.ms/getvsdbgsh -O - 2>/dev/null | /bin/sh /dev/stdin -v vs2022 -l /remote_debugger/vsdbg

RUN chmod 777 -R /remote_debugger/vsdbg
