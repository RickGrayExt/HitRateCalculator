FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ../../HitRate.sln ./
COPY ../Contracts/Contracts.csproj ../Contracts/
COPY SkuGrouping.Service.csproj ./
RUN dotnet restore ./SkuGrouping.Service.csproj
COPY . .
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet","SkuGrouping.Service.dll"]
