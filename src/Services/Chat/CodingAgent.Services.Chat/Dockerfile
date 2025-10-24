# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["src/Services/Chat/CodingAgent.Services.Chat/CodingAgent.Services.Chat.csproj", "src/Services/Chat/CodingAgent.Services.Chat/"]
RUN dotnet restore "src/Services/Chat/CodingAgent.Services.Chat/CodingAgent.Services.Chat.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/src/Services/Chat/CodingAgent.Services.Chat"
RUN dotnet build "CodingAgent.Services.Chat.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "CodingAgent.Services.Chat.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 5001
EXPOSE 5501
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CodingAgent.Services.Chat.dll"]
