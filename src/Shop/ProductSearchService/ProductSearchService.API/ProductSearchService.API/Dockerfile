FROM mcr.microsoft.com/dotnet/core/aspnet:3.0-stretch-slim AS base

ARG PORT=5100

WORKDIR /app
EXPOSE ${PORT}
ENV ASPNETCORE_URLS http://*:${PORT}
HEALTHCHECK --interval=5s --timeout=10s --retries=3 CMD curl --silent --fail http://localhost:${PORT}/health || exit 1
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
