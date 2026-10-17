# syntax=docker/dockerfile:1

# --- build & publish -------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY LinePulse.Simulator.csproj ./
RUN dotnet restore LinePulse.Simulator.csproj

COPY . ./
RUN dotnet publish LinePulse.Simulator.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# --- runtime -----------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish ./

# Non-root "app" user baked into the base image (APP_UID is provided by the
# .NET runtime images since .NET 8).
USER $APP_UID

ENTRYPOINT ["dotnet", "LinePulse.Simulator.dll"]
