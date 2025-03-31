@echo off
docker-compose down
docker-compose build
docker-compose up -d

echo MIGRATIONS BLEATI!


cd AuthService
set "folder=Migrations"
if exist "%folder%\" (
rmdir /s /q "%folder%"
)
dotnet ef migrations add InitialCreateAuthService
cd ..
cd OrderService
set "folder=Migrations"
if exists %folder\% (
rmdif /s /q "%folder%"
)
dotnet ef migrations add InitialCreateOrderService --context OrderDbContext