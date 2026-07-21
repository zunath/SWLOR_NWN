FROM nwnxee/unified:build8193.37.15
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        dotnet-apphost-pack-10.0 \
        dotnet-runtime-10.0 \
    && rm -rf /var/lib/apt/lists/*

RUN wget https://aka.ms/getvsdbgsh -O - 2>/dev/null | /bin/sh /dev/stdin -v vs2022 -l /remote_debugger/vsdbg

RUN chmod 777 -R /remote_debugger/vsdbg
