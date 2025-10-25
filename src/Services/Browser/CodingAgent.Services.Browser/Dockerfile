# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY CodingAgent.Services.Browser.csproj .
RUN dotnet restore CodingAgent.Services.Browser.csproj

# Copy source code and build
COPY . .
RUN dotnet build CodingAgent.Services.Browser.csproj -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish CodingAgent.Services.Browser.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Install Playwright dependencies for Chromium
RUN apt-get update && apt-get install -y \
    libnss3 \
    libnspr4 \
    libatk1.0-0 \
    libatk-bridge2.0-0 \
    libcups2 \
    libdrm2 \
    libdbus-1-3 \
    libxkbcommon0 \
    libxcomposite1 \
    libxdamage1 \
    libxfixes3 \
    libxrandr2 \
    libgbm1 \
    libasound2 \
    libpango-1.0-0 \
    libcairo2 \
    && rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=publish /app/publish .

# Install Playwright browsers (will be done at runtime)
ENV PLAYWRIGHT_BROWSERS_PATH=/ms-playwright

EXPOSE 5004

ENTRYPOINT ["dotnet", "CodingAgent.Services.Browser.dll"]
