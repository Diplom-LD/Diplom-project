FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY . .  

RUN dotnet restore "OrderService/OrderService.csproj" --disable-parallel --ignore-failed-sources

WORKDIR "/src/OrderService"
RUN dotnet build -c $BUILD_CONFIGURATION --no-restore

FROM build AS publish
RUN dotnet publish -c $BUILD_CONFIGURATION --no-restore -o /app/publish

FROM build AS final  
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "OrderService.dll"]
CMD ["dotnet", "OrderService.dll"]
