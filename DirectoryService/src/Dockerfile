FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
ARG GITHUB_PACKAGES_USERNAME
ARG GITHUB_PACKAGES_TOKEN
WORKDIR /src
COPY ["src/DirectoryService.Presentation/DirectoryService.Presentation.csproj", "src/DirectoryService.Presentation/"]
COPY ["src/DirectoryService.Application/DirectoryService.Application.csproj", "src/DirectoryService.Application/"]
COPY ["src/DirectoryService.Contracts/DirectoryService.Contracts.csproj", "src/DirectoryService.Contracts/"]
COPY ["src/DirectoryService.Domain/DirectoryService.Domain.csproj", "src/DirectoryService.Domain/"]
COPY ["src/DirectoryService.Infrastructure.Postgres/DirectoryService.Infrastructure.Postgres.csproj", "src/DirectoryService.Infrastructure.Postgres/"]
RUN dotnet nuget add source "https://nuget.pkg.github.com/FrMks/index.json" \
    --name github \
    --username "$GITHUB_PACKAGES_USERNAME" \
    --password "$GITHUB_PACKAGES_TOKEN" \
    --store-password-in-clear-text
RUN dotnet restore "src/DirectoryService.Presentation/DirectoryService.Presentation.csproj"
COPY . .
WORKDIR "/src/src/DirectoryService.Presentation"
RUN dotnet build "./DirectoryService.Presentation.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR "/src/src/DirectoryService.Presentation"
RUN dotnet publish "./DirectoryService.Presentation.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "DirectoryService.Presentation.dll"]
