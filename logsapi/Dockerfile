# syntax=docker/dockerfile:1

# --- build & publish -------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY LinePulse.LogsApi.csproj ./
RUN dotnet restore LinePulse.LogsApi.csproj

COPY . ./
RUN dotnet publish LinePulse.LogsApi.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# --- runtime -----------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish ./

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Non-root "app" user baked into the base image (APP_UID is provided by the
# .NET runtime images since .NET 8).
USER $APP_UID

ENTRYPOINT ["dotnet", "LinePulse.LogsApi.dll"]
