# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY CodingAgent.Services.CICDMonitor.csproj .
RUN dotnet restore CodingAgent.Services.CICDMonitor.csproj

# Copy source code and build
COPY . .
RUN dotnet build CodingAgent.Services.CICDMonitor.csproj -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish CodingAgent.Services.CICDMonitor.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 5005
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CodingAgent.Services.CICDMonitor.dll"]
