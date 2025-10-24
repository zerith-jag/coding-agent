# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["src/Services/Dashboard/CodingAgent.Services.Dashboard/CodingAgent.Services.Dashboard.csproj", "src/Services/Dashboard/CodingAgent.Services.Dashboard/"]
RUN dotnet restore "src/Services/Dashboard/CodingAgent.Services.Dashboard/CodingAgent.Services.Dashboard.csproj"

# Copy source code and build
COPY src/Services/Dashboard/CodingAgent.Services.Dashboard/ src/Services/Dashboard/CodingAgent.Services.Dashboard/
WORKDIR "/src/src/Services/Dashboard/CodingAgent.Services.Dashboard"
RUN dotnet build "CodingAgent.Services.Dashboard.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "CodingAgent.Services.Dashboard.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 5006
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CodingAgent.Services.Dashboard.dll"]
