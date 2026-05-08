FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY SWLOR.BackgroundServices/SWLOR.BackgroundServices.csproj SWLOR.BackgroundServices/
RUN dotnet restore SWLOR.BackgroundServices/SWLOR.BackgroundServices.csproj

COPY SWLOR.BackgroundServices/ SWLOR.BackgroundServices/
RUN dotnet publish SWLOR.BackgroundServices/SWLOR.BackgroundServices.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SWLOR.BackgroundServices.dll"]
