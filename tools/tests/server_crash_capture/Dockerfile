ARG SERVER_IMAGE=swlor-crash-capture:local
FROM debian:bookworm-slim AS fixture-builder
RUN apt-get update \
    && apt-get install -y --no-install-recommends gcc libc6-dev \
    && rm -rf /var/lib/apt/lists/*
COPY fixture.c /tmp/crash-fixture.c
RUN gcc -g -O0 -rdynamic /tmp/crash-fixture.c -o /crash-fixture \
    && rm /tmp/crash-fixture.c

FROM ${SERVER_IMAGE}
COPY --from=fixture-builder /crash-fixture /usr/local/bin/crash-fixture
# The fixture exports its own fatal handler and is not an NWN executable.
ENV NWN_LD_PRELOAD="" NWN_LD_LIBRARY_PATH=""
ENTRYPOINT ["/usr/bin/tini", "--", "/usr/local/bin/swlor-server-launcher", "/usr/local/bin/crash-fixture"]
