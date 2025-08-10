# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy project files for better caching
COPY src/RinhaBackend2025/*.csproj ./RinhaBackend2025/
RUN dotnet restore ./RinhaBackend2025/RinhaBackend2025.csproj

# Copy source and build
COPY src/RinhaBackend2025/ ./RinhaBackend2025/
WORKDIR /src/RinhaBackend2025

# Build with restore (removemos --no-restore)
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine
WORKDIR /app

# Performance environment variables
ENV DOTNET_EnableDiagnostics=0
ENV DOTNET_TieredPGO=1
ENV DOTNET_TC_QuickJitForLoops=1
ENV DOTNET_ReadyToRun=1
ENV DOTNET_gcServer=1
ENV DOTNET_GCHeapHardLimit=150000000
ENV ASPNETCORE_URLS=http://+:8080

# Create data directory for SQLite
RUN mkdir -p /app/data && \
   adduser -D -s /bin/sh appuser && \
   chown -R appuser:appuser /app

# Copy application
COPY --from=build /app/publish .

USER appuser
EXPOSE 8080

ENTRYPOINT ["dotnet", "RinhaBackend2025.dll"]