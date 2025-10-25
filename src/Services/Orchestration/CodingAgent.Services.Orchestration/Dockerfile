# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY CodingAgent.Services.Orchestration.csproj .
RUN dotnet restore CodingAgent.Services.Orchestration.csproj

# Copy all source files
COPY . .

# Build the project
RUN dotnet build CodingAgent.Services.Orchestration.csproj -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish CodingAgent.Services.Orchestration.csproj -c Release -o /app/publish /p:UseAppHost=false

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
