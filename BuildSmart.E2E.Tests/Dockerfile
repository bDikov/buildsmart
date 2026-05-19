FROM mcr.microsoft.com/dotnet/sdk:9.0

WORKDIR /src

# Copy everything
COPY . .

WORKDIR /src/BuildSmart.E2E.Tests

# Build the E2E tests
RUN dotnet build -c Release

# Install Playwright CLI and Browsers with dependencies
RUN dotnet tool install --global Microsoft.Playwright.CLI
ENV PATH="${PATH}:/root/.dotnet/tools"
RUN playwright install --with-deps

CMD ["dotnet", "test", "-c", "Release", "--no-build"]