FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

# Install dependencies required by PuppeteerSharp / Headless Chrome
RUN apt-get update && apt-get install -y \
    wget \
    gnupg \
    ca-certificates \
    libgconf-2-4 \
    libnss3 \
    libxss1 \
    libasound2 \
    libatk1.0-0 \
    libatk-bridge2.0-0 \
    libc6 \
    libcairo2 \
    libcups2 \
    libdbus-1-3 \
    libexpat1 \
    libfontconfig1 \
    libgbm1 \
    libgcc1 \
    libglib2.0-0 \
    libgtk-3-0 \
    libpango-1.0-0 \
    libpangocairo-1.0-0 \
    libstdc++6 \
    libx11-6 \
    libx11-xcb1 \
    libxcb1 \
    libxcomposite1 \
    libxcursor1 \
    libxdamage1 \
    libxext6 \
    libxfixes3 \
    libxi6 \
    libxrandr2 \
    libxrender1 \
    libxtst6 \
    lsb-release \
    xdg-utils \
    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ENV DOTNET_ROLL_FORWARD=Major
WORKDIR /src

# Copy csproj files for restore
COPY ["BuildSmart.Api/BuildSmart.Api.csproj", "BuildSmart.Api/"]
COPY ["BuildSmart.Core.Application/BuildSmart.Core.Application.csproj", "BuildSmart.Core.Application/"]
COPY ["BuildSmart.Infrastructure/BuildSmart.Infrastructure.csproj", "BuildSmart.Infrastructure/"]
COPY ["BuildSmart.Core.Domain/BuildSmart.Core.Domain.csproj", "BuildSmart.Core.Domain/"]
COPY ["BuildSmart.SharedUI/BuildSmart.SharedUI.csproj", "BuildSmart.SharedUI/"]

RUN dotnet restore "BuildSmart.Api/BuildSmart.Api.csproj"

# Copy all source files
COPY . .
WORKDIR "/src/BuildSmart.Api"
RUN dotnet build "BuildSmart.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BuildSmart.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BuildSmart.Api.dll"]