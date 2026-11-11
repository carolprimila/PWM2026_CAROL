# Build a partir da raiz do repositório:  docker build -f src/ProjetoClubes.Api/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY NuGet.Config ./
COPY src/ProjetoClubes.Api/ProjetoClubes.Api.csproj src/ProjetoClubes.Api/
RUN dotnet restore src/ProjetoClubes.Api/ProjetoClubes.Api.csproj
COPY src/ProjetoClubes.Api/ src/ProjetoClubes.Api/
RUN dotnet publish src/ProjetoClubes.Api/ProjetoClubes.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "ProjetoClubes.Api.dll"]
