# Базовый образ ASP.NET Core для финального контейнера
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Используем SDK-образ для сборки проекта
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем только .csproj для кэширования зависимостей
COPY AuthService/AuthService.csproj AuthService/
RUN dotnet restore ./AuthService/AuthService.csproj --disable-parallel --no-cache

# Копируем весь код после `restore`
COPY ./AuthService ./AuthService/
WORKDIR /src/AuthService

# Сборка проекта
RUN dotnet build -c Release --no-restore

# Публикация проекта
FROM build AS publish
RUN dotnet publish -c Release --no-restore -o /app/publish

# Создаём слой для миграций
FROM build AS migrations
WORKDIR /src/AuthService
RUN dotnet tool install --global dotnet-ef \
    && export PATH="$PATH:/root/.dotnet/tools" \
    && dotnet ef database update

# Финальный контейнер с минимальным размером
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Запускаем приложение
ENTRYPOINT ["dotnet", "AuthService.dll"]
