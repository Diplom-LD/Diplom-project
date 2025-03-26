# Базовый образ ASP.NET Core для финального контейнера
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Используем SDK-образ для сборки проекта
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем только .csproj, чтобы избежать пересборки зависимостей
COPY ManagerApp/ManagerApp.csproj ManagerApp/
RUN dotnet restore ManagerApp/ManagerApp.csproj

# Копируем весь исходный код после restore
COPY . .
WORKDIR /src/ManagerApp
RUN dotnet publish -c Release -o /app/publish --no-restore

# Финальный образ для запуска
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# Запуск приложения
ENTRYPOINT ["dotnet", "ManagerApp.dll"]
