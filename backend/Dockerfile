FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /app

COPY . .

RUN dotnet publish src/WhereIsMyMoney.Api/WhereIsMyMoney.Api.csproj -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

# curl is needed for the docker-compose healthcheck
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

WORKDIR /app

COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "WhereIsMyMoney.Api.dll"]

EXPOSE 5080
