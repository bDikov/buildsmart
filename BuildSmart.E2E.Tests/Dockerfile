FROM mcr.microsoft.com/dotnet/sdk:9.0-noble

WORKDIR /src

# Allow .NET 8 tools (like StrawberryShake) to run on the .NET 9 runtime
ENV DOTNET_ROLL_FORWARD=Major

# Install required system dependencies for Playwright
RUN apt-get update && apt-get install -y \
    wget \
    gnupg \
    && rm -rf /var/lib/apt/lists/*

# Copy solution and project files
COPY BuildSmart.sln .
COPY BuildSmart.Api/BuildSmart.Api.csproj ./BuildSmart.Api/
COPY BuildSmart.Web/BuildSmart.Web.csproj ./BuildSmart.Web/
COPY BuildSmart.Core.Application/BuildSmart.Core.Application.csproj ./BuildSmart.Core.Application/
COPY BuildSmart.Core.Domain/BuildSmart.Core.Domain.csproj ./BuildSmart.Core.Domain/
COPY BuildSmart.Infrastructure/BuildSmart.Infrastructure.csproj ./BuildSmart.Infrastructure/
COPY BuildSmart.E2E.Tests/BuildSmart.E2E.Tests.csproj ./BuildSmart.E2E.Tests/
COPY BuildSmart.SharedUI/BuildSmart.SharedUI.csproj ./BuildSmart.SharedUI/

# Restore dependencies
RUN dotnet restore BuildSmart.E2E.Tests/BuildSmart.E2E.Tests.csproj

# Copy the rest of the source code
COPY . .

WORKDIR /src/BuildSmart.E2E.Tests

# Build the tests
RUN dotnet build -c Release

# Install Playwright dependencies and browsers
RUN pwsh bin/Release/net9.0/playwright.ps1 install --with-deps

CMD ["dotnet", "test", "-c", "Release", "--no-build"]