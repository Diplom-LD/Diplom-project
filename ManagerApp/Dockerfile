FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["ManagerApp/ManagerApp.csproj", "ManagerApp/"]
RUN dotnet restore "ManagerApp/ManagerApp.csproj"
COPY . .
WORKDIR "/src/ManagerApp"
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ManagerApp.dll"]