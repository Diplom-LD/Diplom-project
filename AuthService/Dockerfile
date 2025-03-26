# Используем образ ASP.NET Core для финального контейнера
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Используем SDK-образ для сборки проекта
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем только .csproj, чтобы избежать пересборки зависимостей
COPY AuthService/AuthService.csproj AuthService/
RUN dotnet restore AuthService/AuthService.csproj

# Копируем все файлы проекта после `restore`
COPY . .
WORKDIR /src/AuthService
RUN dotnet publish -c Release -o /app/publish

# Создаём слой для миграций
FROM build AS migrations
WORKDIR /src/AuthService
RUN dotnet tool install --global dotnet-ef \
    && export PATH="$PATH:/root/.dotnet/tools" \
    && dotnet ef database update

# Финальный образ
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# Запускаем приложение
ENTRYPOINT ["dotnet", "AuthService.dll"]
