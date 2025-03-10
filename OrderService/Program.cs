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

// ��������� JWT
var jwtSettings = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSettings["Key"];
var jwtIssuer = jwtSettings["Issuer"];
var jwtAudience = jwtSettings["Audience"];

if (string.IsNullOrEmpty(jwtKey) || string.IsNullOrEmpty(jwtIssuer) || string.IsNullOrEmpty(jwtAudience))
{
    throw new InvalidOperationException("JWT ��������� �� ������ � ������������.");
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

// ��������� MongoDB
builder.Services.Configure<MongoSettings>(builder.Configuration.GetSection("MongoDB"));
builder.Services.AddSingleton(sp =>
{
    var settings = builder.Configuration.GetSection("MongoDB").Get<MongoSettings>();
    if (settings == null || string.IsNullOrEmpty(settings.ConnectionString) || string.IsNullOrEmpty(settings.DatabaseName))
    {
        throw new InvalidOperationException("MongoDB ��������� �� ������ � ������������.");
    }
    var client = new MongoClient(settings.ConnectionString);
    return client.GetDatabase(settings.DatabaseName);
});
builder.Services.AddSingleton<WarehouseMongoContext>();

// ��������� Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect("redis:6379"));
builder.Services.AddSingleton<TechnicianRedisRepository>();


// ����������� RabbitMQ Consumer
builder.Services.AddHostedService<RabbitMqConsumerService>();

// ����������� �������
builder.Services.AddScoped<IStockRepository<Warehouse>, WarehouseRepository>();
builder.Services.AddScoped<IStockRepository<EquipmentStock>, EquipmentStockRepository>();
builder.Services.AddScoped<IStockRepository<MaterialsStock>, MaterialsStockRepository>();
builder.Services.AddScoped<IStockRepository<ToolsStock>, ToolsStockRepository>();

// ������� �������
builder.Services.AddScoped<WarehouseService>();
builder.Services.AddScoped<EquipmentStockService>();
builder.Services.AddScoped<MaterialsStockService>();
builder.Services.AddScoped<ToolsStockService>();

// ����������
builder.Services.AddHttpClient<OptimizedRouteService>();
builder.Services.AddSingleton<OptimizedRouteService>();
builder.Services.AddHttpClient<IGeoCodingService, GeoCodingService>();
builder.Services.AddScoped<NearestLocationFinderService>();

// ������������� �������� ������
builder.Services.AddSingleton<WarehouseSeeder>();

// ���������� ������������
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ��������� Swagger ��� JWT-��������������
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "������� JWT ����� � �������: Bearer {�����}",
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

// ������������� �������� ������
using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;
    var seeder = serviceProvider.GetRequiredService<WarehouseSeeder>();
    await seeder.SeedAsync(serviceProvider);
}

// ��������� Swagger
app.UseSwagger();
app.UseSwaggerUI();

// ��������� Middleware
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
