param(
    [string]$TestName
)

$ErrorActionPreference = "Stop"

# This script is now running inside the container
dotnet test BuildSmart.Tests.sln --no-restore --filter "FullyQualifiedName=$TestName"