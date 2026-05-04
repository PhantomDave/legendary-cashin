FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /app

COPY . .

RUN dotnet publish src/WhereIsMyMoney.Api/WhereIsMyMoney.Api.csproj -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "WhereIsMyMoney.Api.dll"]

EXPOSE 5080
