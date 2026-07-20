# Build from the backend directory. Render should use `backend` as the root directory.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/OrgWiki.Domain/OrgWiki.Domain.csproj", "src/OrgWiki.Domain/"]
COPY ["src/OrgWiki.Application/OrgWiki.Application.csproj", "src/OrgWiki.Application/"]
COPY ["src/OrgWiki.Infrastructure/OrgWiki.Infrastructure.csproj", "src/OrgWiki.Infrastructure/"]
COPY ["src/OrgWiki.API/OrgWiki.API.csproj", "src/OrgWiki.API/"]

RUN dotnet restore "src/OrgWiki.API/OrgWiki.API.csproj"

COPY . .
WORKDIR /src/src/OrgWiki.API
RUN dotnet publish "OrgWiki.API.csproj" --configuration Release --output /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Render's default web-service port is 10000. Database migrations remain a manual operation.
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:10000 \
    INGESTION__LOCALSTORAGEPATH=/var/orgwiki/storage \
    DOTNET_EnableDiagnostics=0

RUN mkdir -p /var/orgwiki/storage && chown -R app:app /var/orgwiki
COPY --from=build --chown=app:app /app/publish .

USER app
EXPOSE 10000

ENTRYPOINT ["dotnet", "OrgWiki.API.dll"]
