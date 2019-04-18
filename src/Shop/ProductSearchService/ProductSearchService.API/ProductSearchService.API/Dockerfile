FROM mcr.microsoft.com/dotnet/core/aspnet:3.0-stretch-slim AS base
WORKDIR /app
EXPOSE 5100
ENV ASPNETCORE_URLS http://*:5100
HEALTHCHECK --interval=30s --timeout=3s --retries=1 CMD curl --silent --fail http://localhost:5100/health || exit 1
ENV WEBSITE_SITE_NAME ProductSearchService.API

FROM mcr.microsoft.com/dotnet/core/sdk:3.0-stretch AS build
WORKDIR /src
COPY . .
RUN dotnet restore "ProductSearchService.API.csproj"
RUN dotnet build "ProductSearchService.API.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "ProductSearchService.API.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "ProductSearchService.API.dll"]
