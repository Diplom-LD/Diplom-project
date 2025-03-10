using OrderService.Config;
using OrderService.Data.Warehouses;
using OrderService.Repositories.Technicians;
using OrderService.Repositories.Warehouses;
using OrderService.SeedData.Warehouses;
using OrderService.Services.GeoLocation;
using OrderService.Services.RabbitMq;
using OrderService.Services.Warehouses;
using StackExchange.Redis;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MongoDB.Driver;
using OrderService.Models.Warehouses;

var builder = WebApplication.CreateBuilder(args);

// Настройки JWT
var jwtSettings = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSettings["Key"];
var jwtIssuer = jwtSettings["Issuer"];
var jwtAudience = jwtSettings["Audience"];

if (string.IsNullOrEmpty(jwtKey) || string.IsNullOrEmpty(jwtIssuer) || string.IsNullOrEmpty(jwtAudience))
{
    throw new InvalidOperationException("JWT параметры не заданы в конфигурации.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });
builder.Services.AddAuthorization();

// Настройка MongoDB
builder.Services.Configure<MongoSettings>(builder.Configuration.GetSection("MongoDB"));
builder.Services.AddSingleton(sp =>
{
    var settings = builder.Configuration.GetSection("MongoDB").Get<MongoSettings>();
    if (settings == null || string.IsNullOrEmpty(settings.ConnectionString) || string.IsNullOrEmpty(settings.DatabaseName))
    {
        throw new InvalidOperationException("MongoDB параметры не заданы в конфигурации.");
    }
    var client = new MongoClient(settings.ConnectionString);
    return client.GetDatabase(settings.DatabaseName);
});
builder.Services.AddSingleton<WarehouseMongoContext>();

// Настройка Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect("redis:6379"));
builder.Services.AddSingleton<TechnicianRedisRepository>();


// Подключение RabbitMQ Consumer
builder.Services.AddHostedService<RabbitMqConsumerService>();

// Репозитории складов
builder.Services.AddScoped<IStockRepository<Warehouse>, WarehouseRepository>();
builder.Services.AddScoped<IStockRepository<EquipmentStock>, EquipmentStockRepository>();
builder.Services.AddScoped<IStockRepository<MaterialsStock>, MaterialsStockRepository>();
builder.Services.AddScoped<IStockRepository<ToolsStock>, ToolsStockRepository>();

// Сервисы складов
builder.Services.AddScoped<WarehouseService>();
builder.Services.AddScoped<EquipmentStockService>();
builder.Services.AddScoped<MaterialsStockService>();
builder.Services.AddScoped<ToolsStockService>();

// Геолокация
builder.Services.AddHttpClient<OptimizedRouteService>();
builder.Services.AddSingleton<OptimizedRouteService>();
builder.Services.AddHttpClient<IGeoCodingService, GeoCodingService>();
builder.Services.AddScoped<NearestLocationFinderService>();

// Инициализация тестовых данных
builder.Services.AddSingleton<WarehouseSeeder>();

// Добавление контроллеров
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Настройка Swagger для JWT-аутентификации
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Введите JWT токен в формате: Bearer {токен}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Инициализация тестовых данных
using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;
    var seeder = serviceProvider.GetRequiredService<WarehouseSeeder>();
    await seeder.SeedAsync(serviceProvider);
}

// Включение Swagger
app.UseSwagger();
app.UseSwaggerUI();

// Настройки Middleware
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
