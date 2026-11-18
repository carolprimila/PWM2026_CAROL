# --- build ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props ./
COPY Clubes.Domain/Clubes.Domain.csproj Clubes.Domain/
COPY Clubes.Application/Clubes.Application.csproj Clubes.Application/
COPY Clubes.Infrastructure/Clubes.Infrastructure.csproj Clubes.Infrastructure/
COPY Clubes.API/Clubes.API.csproj Clubes.API/
RUN dotnet restore Clubes.API/Clubes.API.csproj
COPY Clubes.Domain/ Clubes.Domain/
COPY Clubes.Application/ Clubes.Application/
COPY Clubes.Infrastructure/ Clubes.Infrastructure/
COPY Clubes.API/ Clubes.API/
RUN dotnet publish Clubes.API/Clubes.API.csproj -c Release -o /app --no-restore

# --- runtime ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0
RUN apt-get update && apt-get install -y --no-install-recommends wget tzdata libfontconfig1 && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
# A API roda sem privilégios; arquivos enviados e logs ficam em volumes.
RUN mkdir -p /data/storage /data/logs && chown -R $APP_UID /data
USER $APP_UID
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Clubes.API.dll"]
