FROM microsoft/dotnet:3.0-sdk-stretch AS base
WORKDIR /app

FROM microsoft/dotnet:3.0-sdk-stretch AS build
WORKDIR /src
COPY . .
RUN dotnet restore "ProductSearchService.EventListener.csproj"
RUN dotnet build "ProductSearchService.EventListener.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "ProductSearchService.EventListener.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "ProductSearchService.EventListener.dll"]