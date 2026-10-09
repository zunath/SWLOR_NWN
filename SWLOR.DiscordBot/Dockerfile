FROM mcr.microsoft.com/dotnet/sdk:10.0.401@sha256:e70cdb7f80b0348f5cb85f19a8f670fca061f033d57eed12fa003d58b0e06317 AS build
WORKDIR /src
COPY SWLOR.DiscordBot.csproj ./
RUN dotnet restore SWLOR.DiscordBot.csproj
COPY . ./
RUN dotnet publish SWLOR.DiscordBot.csproj --configuration Release --no-restore --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0.12@sha256:222759b391a1aaf241166672c8f99b2d4ada452e7b5319f3c6e8f265a37b5ad4 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
RUN mkdir -p /data/archives && chown -R "$APP_UID":"$APP_UID" /data
ENV DOTNET_EnableDiagnostics=0 SWLOR_BOT_READY_FILE=/data/bot.ready
USER $APP_UID
ENTRYPOINT ["dotnet", "SWLOR.DiscordBot.dll"]
CMD ["--config", "/config/bot.json"]
