FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ENV DOTNET_ROLL_FORWARD=Major
WORKDIR /src

# Copy csproj files for restore
COPY ["BuildSmart.Web/BuildSmart.Web.csproj", "BuildSmart.Web/"]
COPY ["BuildSmart.SharedUI/BuildSmart.SharedUI.csproj", "BuildSmart.SharedUI/"]
COPY ["BuildSmart.Core.Application/BuildSmart.Core.Application.csproj", "BuildSmart.Core.Application/"]
COPY ["BuildSmart.Core.Domain/BuildSmart.Core.Domain.csproj", "BuildSmart.Core.Domain/"]

RUN dotnet restore "BuildSmart.Web/BuildSmart.Web.csproj"

# Copy all source files
COPY . .
WORKDIR "/src/BuildSmart.Web"
RUN dotnet build "BuildSmart.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BuildSmart.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BuildSmart.Web.dll"]