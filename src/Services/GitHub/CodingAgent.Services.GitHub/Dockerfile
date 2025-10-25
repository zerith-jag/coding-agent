# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY CodingAgent.Services.GitHub.csproj .
RUN dotnet restore CodingAgent.Services.GitHub.csproj

# Copy source code and build
COPY . .
RUN dotnet build CodingAgent.Services.GitHub.csproj -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish CodingAgent.Services.GitHub.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 5003
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CodingAgent.Services.GitHub.dll"]
