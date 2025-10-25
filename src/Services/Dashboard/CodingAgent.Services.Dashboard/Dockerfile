# Build stage
# syntax=docker/dockerfile:1.6
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILDKIT_INLINE_CACHE=1
WORKDIR /src

# Copy solution-level files that may affect restore
COPY global.json ./
COPY Directory.Build.props ./

# Copy project file and restore dependencies
COPY src/Services/Dashboard/CodingAgent.Services.Dashboard/CodingAgent.Services.Dashboard.csproj src/Services/Dashboard/CodingAgent.Services.Dashboard/
RUN --mount=type=cache,id=nuget-cache,sharing=locked,target=/root/.nuget/packages \
	dotnet restore src/Services/Dashboard/CodingAgent.Services.Dashboard/CodingAgent.Services.Dashboard.csproj

# Copy source code and build
COPY src/Services/Dashboard/CodingAgent.Services.Dashboard/ src/Services/Dashboard/CodingAgent.Services.Dashboard/
WORKDIR /src/src/Services/Dashboard/CodingAgent.Services.Dashboard
RUN --mount=type=cache,id=nuget-cache,sharing=locked,target=/root/.nuget/packages \
	dotnet build CodingAgent.Services.Dashboard.csproj -c Release -o /app/build --no-restore

# Publish stage
FROM build AS publish
RUN --mount=type=cache,id=nuget-cache,sharing=locked,target=/root/.nuget/packages \
	dotnet publish CodingAgent.Services.Dashboard.csproj -c Release -o /app/publish /p:UseAppHost=false --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 5006
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CodingAgent.Services.Dashboard.dll"]
