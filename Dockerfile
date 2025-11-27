FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/FileStore.API/FileStore.API.csproj", "FileStore.API/"]
COPY ["src/FileStore.Infrastructure/FileStore.Infrastructure.csproj", "FileStore.Infrastructure/"]
COPY ["src/FileStore.Core/FileStore.Core.csproj", "FileStore.Core/"]

RUN dotnet restore "FileStore.API/FileStore.API.csproj"

COPY src/ .

WORKDIR "/src/FileStore.API"
RUN dotnet build "FileStore.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FileStore.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

RUN mkdir -p /app/storage/hot /app/storage/cold

ENTRYPOINT ["dotnet", "FileStore.API.dll"]
