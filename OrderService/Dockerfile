# Базовый образ ASP.NET Core для финального контейнера
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Используем SDK-образ для сборки проекта
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Копируем только csproj-файл для кэширования зависимостей
COPY OrderService/OrderService.csproj OrderService/
RUN dotnet restore "OrderService/OrderService.csproj" --disable-parallel --no-cache

# Копируем весь исходный код после `restore`
COPY . .
WORKDIR "/src/OrderService"

# Компиляция проекта
RUN dotnet build -c $BUILD_CONFIGURATION --no-restore

# Публикация проекта
FROM build AS publish
RUN dotnet publish -c $BUILD_CONFIGURATION --no-restore -o /app/publish

# Финальный контейнер для запуска
FROM base AS final  
WORKDIR /app
COPY --from=publish /app/publish .

# Запуск сервиса
ENTRYPOINT ["dotnet", "OrderService.dll"]
