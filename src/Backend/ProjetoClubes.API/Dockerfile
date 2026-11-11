# Build a partir da raiz do repositório:  docker build -f src/Backend/ProjetoClubes.API/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/Backend/ProjetoClubes.Domain/ProjetoClubes.Domain.csproj src/Backend/ProjetoClubes.Domain/
COPY src/Backend/ProjetoClubes.Application/ProjetoClubes.Application.csproj src/Backend/ProjetoClubes.Application/
COPY src/Backend/ProjetoClubes.Infrastructure/ProjetoClubes.Infrastructure.csproj src/Backend/ProjetoClubes.Infrastructure/
COPY src/Backend/ProjetoClubes.API/ProjetoClubes.API.csproj src/Backend/ProjetoClubes.API/
RUN dotnet restore src/Backend/ProjetoClubes.API/ProjetoClubes.API.csproj
COPY src/Backend/ src/Backend/
RUN dotnet publish src/Backend/ProjetoClubes.API/ProjetoClubes.API.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "ProjetoClubes.API.dll"]
