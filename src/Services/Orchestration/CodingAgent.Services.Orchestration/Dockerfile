# Build stage
# syntax=docker/dockerfile:1.6
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILDKIT_INLINE_CACHE=1
WORKDIR /src

# Copy solution-level files that may affect restore
COPY global.json ./
COPY Directory.Build.props ./

# Copy SharedKernel
COPY src/SharedKernel/CodingAgent.SharedKernel/CodingAgent.SharedKernel.csproj src/SharedKernel/CodingAgent.SharedKernel/

# Copy service project file and restore dependencies
COPY src/Services/Orchestration/CodingAgent.Services.Orchestration/CodingAgent.Services.Orchestration.csproj src/Services/Orchestration/CodingAgent.Services.Orchestration/
RUN --mount=type=cache,id=nuget-cache,sharing=locked,target=/root/.nuget/packages \
  dotnet restore src/Services/Orchestration/CodingAgent.Services.Orchestration/CodingAgent.Services.Orchestration.csproj

# Copy all source files
COPY src/SharedKernel/CodingAgent.SharedKernel/ src/SharedKernel/CodingAgent.SharedKernel/
COPY src/Services/Orchestration/CodingAgent.Services.Orchestration/ src/Services/Orchestration/CodingAgent.Services.Orchestration/

# Build the project
WORKDIR /src/src/Services/Orchestration/CodingAgent.Services.Orchestration
RUN --mount=type=cache,id=nuget-cache,sharing=locked,target=/root/.nuget/packages \
  dotnet build CodingAgent.Services.Orchestration.csproj -c Release -o /app/build --no-restore

# Publish stage
FROM build AS publish
RUN --mount=type=cache,id=nuget-cache,sharing=locked,target=/root/.nuget/packages \
  dotnet publish CodingAgent.Services.Orchestration.csproj -c Release -o /app/publish /p:UseAppHost=false --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 5002
EXPOSE 5003

# Copy published output
COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5002
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:5002/health || exit 1

ENTRYPOINT ["dotnet", "CodingAgent.Services.Orchestration.dll"]
