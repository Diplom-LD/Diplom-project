@echo off
docker-compose down

docker volume rm diplom-project_postgre_data
docker volume rm diplom-project_mongo_data
docker volume rm diplom-project_redis_data

docker-compose build
docker-compose up -d

cd AuthService
set "folder=Migrations"
if exist "%folder%\" (
    rmdir /s /q "%folder%"
)
dotnet ef migrations add InitialCreateAuthService
cd ..

cd OrderService
set "folder=Migrations"
if exist "%folder%\" (
    rmdir /s /q "%folder%"
)
dotnet ef migrations add InitialCreateOrderService --context OrderDbContext
cd ..

echo ✅ Ready
pause
