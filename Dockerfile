FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env
WORKDIR /src

COPY ["UniParser/UniParser.csproj", "UniParser/"]
RUN dotnet restore "UniParser/UniParser.csproj"

COPY . .
WORKDIR "/src/UniParser"
RUN dotnet publish "UniParser.csproj" -c Release -o /app/out

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app

COPY --from=build-env /app/out .

RUN apt-get update && apt-get install -y \
    nodejs \
    npm \
    && rm -rf /var/lib/apt/lists/*

RUN npm install -g playwright

RUN npx playwright install --with-deps chromium

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV PLAYWRIGHT_BLINK_HEADLESS=true

ENTRYPOINT ["dotnet", "UniParser.dll"]