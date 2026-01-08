param(
    [string]$TestName
)

$ErrorActionPreference = "Stop"

$TestId = [guid]::NewGuid().ToString().Substring(0, 8)
$DbContainerName = "buildsmart-db-$TestId"
$TestContainerName = "buildsmart-test-$TestId"
$NetworkName = "buildsmart-test-net-$TestId"

try {
    # Create a new network for the test
    docker network create $NetworkName

    # Start the database container
    docker run -d --name $DbContainerName --network $NetworkName -e POSTGRES_USER=testuser -e POSTGRES_PASSWORD=testpassword -e POSTGRES_DB=buildsmart_test postgres:15-alpine

    # Build the base image if it doesn't exist
    if (-not (docker images -q buildsmart-base-test)) {
        docker build -t buildsmart-base-test -f docker/Dockerfile.base .
    }

    # Run the test in a new container
    $Command = "cd /src && dotnet build && dotnet test --no-build --filter 'FullyQualifiedName=$TestName'"
    docker run --name $TestContainerName --network $NetworkName -w /src -e ConnectionStrings__DefaultConnection="Host=$DbContainerName;Port=5432;Database=buildsmart_test;Username=testuser;Password=testpassword" `
        -e UpdateSnapshots=true `
        buildsmart-base-test `
        sh -c $Command
}
finally {
    # Clean up the containers and network
    docker stop $TestContainerName
    docker rm $TestContainerName
    docker stop $DbContainerName
    docker rm $DbContainerName
    docker network rm $NetworkName
}