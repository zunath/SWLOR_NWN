FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY SWLOR.BackgroundServices/SWLOR.BackgroundServices.csproj SWLOR.BackgroundServices/
RUN dotnet restore SWLOR.BackgroundServices/SWLOR.BackgroundServices.csproj

COPY SWLOR.BackgroundServices/ SWLOR.BackgroundServices/
RUN dotnet publish SWLOR.BackgroundServices/SWLOR.BackgroundServices.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:8.0
RUN groupadd --gid 10001 appgroup \
    && useradd --uid 10001 --gid appgroup --create-home --shell /usr/sbin/nologin appuser
WORKDIR /app
COPY --from=build /app/publish .
RUN chown -R appuser:appgroup /app
USER appuser
ENTRYPOINT ["dotnet", "SWLOR.BackgroundServices.dll"]
