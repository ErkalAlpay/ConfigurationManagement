# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Configuration.Core ./Configuration.Core
COPY Configuration.Api ./Configuration.Api

RUN dotnet restore Configuration.Api/Configuration.Api.csproj
RUN dotnet build Configuration.Api/Configuration.Api.csproj -c Release -o /app/build

# Publish stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/build ./

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Configuration.Api.dll"]
