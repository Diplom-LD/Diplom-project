using OrderService.Config;
using OrderService.Data.Orders;
using OrderService.Data.Warehouses;
using OrderService.Repositories.Warehouses;
using OrderService.Repositories.Orders;
using OrderService.Repositories.Users;
using OrderService.SeedData.Warehouses;
using OrderService.Services.GeoLocation;
using OrderService.Services.GeoLocation.HTTPClient;
using OrderService.Services.Orders;
using OrderService.Services.Technicians;
using OrderService.Services.Warehouses;
using OrderService.Services.RabbitMq;
using OrderService.Services.GeoLocation.RouteServiceClient;
using StackExchange.Redis;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MongoDB.Driver;
using Microsoft.EntityFrameworkCore;
using OrderService.Models.Warehouses;
using RabbitMQ.Client;
using OrderService.Services.GeoLocation.GeoCodingClient;

var builder = WebApplication.CreateBuilder(args);

// Настройки JWT
var jwtSettings = builder.Configuration.GetRequiredSection("Jwt");
var jwtKey = jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key not found.");
var jwtIssuer = jwtSettings["Issuer"] ?? throw new InvalidOperationException("JWT Issuer not found.");
var jwtAudience = jwtSettings["Audience"] ?? throw new InvalidOperationException("JWT Audience not found.");

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

// Настройка PostgreSQL (OrderDbContext)
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgresConnection")));

// Настройка MongoDB
builder.Services.Configure<MongoSettings>(builder.Configuration.GetRequiredSection("MongoDB"));
builder.Services.AddSingleton(sp =>
{
    var settings = builder.Configuration.GetRequiredSection("MongoDB").Get<MongoSettings>()
                   ?? throw new InvalidOperationException("MongoDB settings not found.");
    var client = new MongoClient(settings.ConnectionString);
    return client.GetDatabase(settings.DatabaseName);
});
builder.Services.AddSingleton<WarehouseMongoContext>();

// Настройка Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect("redis:6379"));

// Регистрация RabbitMQ Connection
builder.Services.AddSingleton<IConnection>(sp =>
{
    var factory = new ConnectionFactory
    {
        HostName = "rabbitmq",
        Port = 5672,
        UserName = "guest",
        Password = "guest",
    };

    return Task.Run(() => factory.CreateConnectionAsync()).GetAwaiter().GetResult(); 
});

// Подключение RabbitMQ Consumer (Менеджеры, Клиенты, Техники)
builder.Services.AddSingleton<TechnicianConsumer>();
builder.Services.AddSingleton<ManagerConsumer>();
builder.Services.AddSingleton<ClientConsumer>();
builder.Services.AddHostedService<RabbitMqConsumerService>();

// Репозитории
builder.Services.AddScoped<OrderRepository>();
builder.Services.AddScoped<UserPostgreRepository>();
builder.Services.AddScoped<UserRedisRepository>();
builder.Services.AddScoped<IStockRepository<EquipmentStock>, EquipmentStockRepository>();
builder.Services.AddScoped<IStockRepository<MaterialsStock>, MaterialsStockRepository>();
builder.Services.AddScoped<IStockRepository<ToolsStock>, ToolsStockRepository>();
builder.Services.AddScoped<IStockRepository<Warehouse>, WarehouseRepository>();

// Инициализация тестовых данных
builder.Services.AddSingleton<WarehouseSeeder>();

// Сервисы
// Геолокация
builder.Services.AddHttpClient<IGeoCodingService, GeoCodingService>();
builder.Services.AddScoped<GeoCodingService>();
builder.Services.AddHttpClient<IHttpClient, HttpClientWrapper>();
builder.Services.AddScoped<IRouteServiceClient, OpenRouteServiceClient>();
builder.Services.AddScoped<IOptimizedRouteService, OptimizedRouteService>();
builder.Services.AddScoped<NearestLocationFinderService>();
builder.Services.AddScoped<OptimizedRouteService>();

// Заявки
builder.Services.AddScoped<WarehouseAvailabilityService>();
builder.Services.AddScoped<OrderServiceManager>();
builder.Services.AddScoped<OrderServiceClient>();

// Техники
builder.Services.AddScoped<TechnicianAvailabilityService>();
builder.Services.AddScoped<TechnicianRouteSaveService>();

// Сервисы складов
builder.Services.AddScoped<EquipmentStockService>();
builder.Services.AddScoped<MaterialsStockService>();
builder.Services.AddScoped<ToolsStockService>();
builder.Services.AddScoped<WarehouseService>();

// Отслеживание техников
builder.Services.AddSingleton<TechnicianSimulationService>();
builder.Services.AddSingleton<TechnicianTrackingService>();

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

// Автоматические миграции PostgreSQL
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var orderDbContext = services.GetRequiredService<OrderDbContext>();
        orderDbContext.Database.Migrate();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Ошибка при миграции базы данных.");
    }
}

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
app.UseWebSockets();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
