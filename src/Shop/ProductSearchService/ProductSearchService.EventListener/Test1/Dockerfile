FROM mcr.microsoft.com/dotnet/core/aspnet:3.0-stretch-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.0-stretch AS build
WORKDIR /src
COPY ["Test1/Test1.csproj", "Test1/"]
RUN dotnet restore "Test1/Test1.csproj"
COPY . .
WORKDIR "/src/Test1"
RUN dotnet build "Test1.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "Test1.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Test1.dll"]