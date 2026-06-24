# =========================
# BUILD STAGE
# =========================

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Nur Projektdateien kopieren (besseres Docker-Caching)
COPY StockTvBlazor.slnx .
COPY StockTvBlazor/*.csproj StockTvBlazor/

# Restore
RUN dotnet restore StockTvBlazor/StockTvBlazor.csproj

# Restliche Dateien kopieren
COPY . .

# Publish
RUN dotnet publish 
StockTvBlazor/StockTvBlazor.csproj 
-c Release 
-o /app/publish 
-p:UseAppHost=false

# =========================
# RUNTIME STAGE
# =========================

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "StockTvBlazor.dll"]
