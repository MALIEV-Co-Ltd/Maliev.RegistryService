# syntax=docker/dockerfile:1.4
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy nuget.config and project files
COPY ["nuget.config", "."]
COPY ["Maliev.RegistryService.Api/Maliev.RegistryService.Api.csproj", "Maliev.RegistryService.Api/"]
COPY ["Maliev.RegistryService.Data/Maliev.RegistryService.Data.csproj", "Maliev.RegistryService.Data/"]

# Restore with GitHub Packages authentication using BuildKit secrets
# We set GITHUB_ACTIONS=true to trigger PackageReference usage in csproj
RUN --mount=type=secret,id=nuget_username \
    --mount=type=secret,id=nuget_password \
    GITHUB_ACTIONS=true \
    NUGET_USERNAME=$(cat /run/secrets/nuget_username) \
    NUGET_PASSWORD=$(cat /run/secrets/nuget_password) \
    dotnet restore "./Maliev.RegistryService.Api/Maliev.RegistryService.Api.csproj"

COPY . .
WORKDIR "/src/Maliev.RegistryService.Api"
RUN GITHUB_ACTIONS=true dotnet build "./Maliev.RegistryService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN GITHUB_ACTIONS=true dotnet publish "./Maliev.RegistryService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
RUN chown -R app:app /app
USER app
COPY --chown=app:app --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/registry/liveness || exit 1

ENTRYPOINT ["dotnet", "Maliev.RegistryService.Api.dll"]
