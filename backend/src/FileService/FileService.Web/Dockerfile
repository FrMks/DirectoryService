FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
ARG GITHUB_PACKAGES_USERNAME
ARG GITHUB_PACKAGES_TOKEN
WORKDIR /src

COPY ["src/FileService/FileService.Web/FileService.Web.csproj", "src/FileService/FileService.Web/"]
COPY ["src/FileService/FileService.Core/FileService.Core.csproj", "src/FileService/FileService.Core/"]
COPY ["src/FileService/FileService.Contracts/FileService.Contracts.csproj", "src/FileService/FileService.Contracts/"]
COPY ["src/FileService/FileService.Domain/FileService.Domain.csproj", "src/FileService/FileService.Domain/"]
COPY ["src/FileService/FileService.Infrastructure.Postgres/FileService.Infrastructure.Postgres.csproj", "src/FileService/FileService.Infrastructure.Postgres/"]
COPY ["src/FileService/FileService.Infrastructure.S3/FileService.Infrastructure.S3.csproj", "src/FileService/FileService.Infrastructure.S3/"]

RUN dotnet nuget add source "https://nuget.pkg.github.com/FrMks/index.json" \
    --name github \
    --username "$GITHUB_PACKAGES_USERNAME" \
    --password "$GITHUB_PACKAGES_TOKEN" \
    --store-password-in-clear-text

RUN dotnet restore "src/FileService/FileService.Web/FileService.Web.csproj"
COPY . .
WORKDIR "/src/src/FileService/FileService.Web"
RUN dotnet build "./FileService.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR "/src/src/FileService/FileService.Web"
RUN dotnet publish "./FileService.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "FileService.Web.dll"]
