# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["src/Services/CICDMonitor/CodingAgent.Services.CICDMonitor/CodingAgent.Services.CICDMonitor.csproj", "src/Services/CICDMonitor/CodingAgent.Services.CICDMonitor/"]
RUN dotnet restore "src/Services/CICDMonitor/CodingAgent.Services.CICDMonitor/CodingAgent.Services.CICDMonitor.csproj"

# Copy source code and build
COPY src/Services/CICDMonitor/CodingAgent.Services.CICDMonitor/ src/Services/CICDMonitor/CodingAgent.Services.CICDMonitor/
WORKDIR "/src/src/Services/CICDMonitor/CodingAgent.Services.CICDMonitor"
RUN dotnet build "CodingAgent.Services.CICDMonitor.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "CodingAgent.Services.CICDMonitor.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 5005
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CodingAgent.Services.CICDMonitor.dll"]
