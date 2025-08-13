# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["src/RinhaBackend2025/RinhaBackend2025.csproj", "src/RinhaBackend2025/"]
RUN dotnet restore "src/RinhaBackend2025/RinhaBackend2025.csproj"

# Copy everything else and build
COPY src/RinhaBackend2025/ ./RinhaBackend2025/
WORKDIR /src/RinhaBackend2025
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

# Create app user and data directory
RUN mkdir -p /app/data && \
    adduser -D -s /bin/sh appuser && \
    chown -R appuser:appuser /app

# Copy published app
COPY --from=build /app/publish .

USER appuser
EXPOSE 8080

ENTRYPOINT ["dotnet", "RinhaBackend2025.dll"]
