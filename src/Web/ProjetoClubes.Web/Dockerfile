# Build a partir da raiz do repositório:  docker build -f src/Web/ProjetoClubes.Web/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/Web/ProjetoClubes.Web/ProjetoClubes.Web.csproj src/Web/ProjetoClubes.Web/
RUN dotnet restore src/Web/ProjetoClubes.Web/ProjetoClubes.Web.csproj
COPY src/Web/ProjetoClubes.Web/ src/Web/ProjetoClubes.Web/
RUN dotnet publish src/Web/ProjetoClubes.Web/ProjetoClubes.Web.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "ProjetoClubes.Web.dll"]
