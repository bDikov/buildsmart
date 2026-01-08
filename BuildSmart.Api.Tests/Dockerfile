FROM mcr.microsoft.com/dotnet/sdk:9.0

WORKDIR /src

# Copy solution and project files
COPY BuildSmart.sln .
COPY BuildSmart.Api/BuildSmart.Api.csproj ./BuildSmart.Api/
COPY BuildSmart.Api.Tests/BuildSmart.Api.Tests.csproj ./BuildSmart.Api.Tests/
COPY BuildSmart.Core.Application/BuildSmart.Core.Application.csproj ./BuildSmart.Core.Application/
COPY BuildSmart.Core.Domain/BuildSmart.Core.Domain.csproj ./BuildSmart.Core.Domain/
COPY BuildSmart.Infrastructure/BuildSmart.Infrastructure.csproj ./BuildSmart.Infrastructure/

# Restore dependencies
RUN dotnet restore

# Copy the rest of the source code
COPY . .

WORKDIR /src/BuildSmart.Api.Tests

# Run the tests
CMD ["dotnet", "test"]