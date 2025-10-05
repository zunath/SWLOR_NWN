#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["SWLOR.App.Admin/SWLOR.App.Admin.csproj", "SWLOR.App.Admin/"]
RUN dotnet restore "SWLOR.App.Admin/SWLOR.App.Admin.csproj"
COPY . .
WORKDIR "/src/SWLOR.App.Admin"
RUN dotnet build "SWLOR.App.Admin.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SWLOR.App.Admin.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SWLOR.App.Admin.dll"]