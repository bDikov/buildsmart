$ErrorActionPreference = "Stop"

# Get all tests
$TestList = dotnet test BuildSmart.Tests.sln --list-tests
Write-Host "TestList: $TestList"

$Tests = $TestList | Select-String -Pattern "BuildSmart.Api.Tests"
Write-Host "Tests: $Tests"

foreach ($Test in $Tests) {
    $TestName = $Test.ToString().Trim()
    Write-Host "Running test: $TestName"
    Push-Location docker
    ./run-single-test.ps1 -TestName $TestName
    Pop-Location
}
