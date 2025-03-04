using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OrderService.Config;
using OrderService.Data.Warehouses;
using OrderService.Repositories.Warehouses;
using OrderService.Services.Warehouses;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// JWT настройки
var jwtSettings = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSettings["Key"];
var jwtIssuer = jwtSettings["Issuer"];
var jwtAudience = jwtSettings["Audience"];

if (string.IsNullOrEmpty(jwtKey) || string.IsNullOrEmpty(jwtIssuer) || string.IsNullOrEmpty(jwtAudience))
{
    throw new InvalidOperationException("JWT параметры не заданы в конфигурации.");
}

// Настраиваем аутентификацию с JWT
//builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//    .AddJwtBearer(options =>
//    {
//        options.TokenValidationParameters = new TokenValidationParameters
//        {
//            ValidateIssuer = true,
//            ValidateAudience = true,
//            ValidateLifetime = true,
//            ValidateIssuerSigningKey = true,
//            ValidIssuer = jwtIssuer,
//            ValidAudience = jwtAudience,
//            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
//        };
//    });
//builder.Services.AddAuthorization();


// MongoDBContext
builder.Services.Configure<MongoSettings>(builder.Configuration.GetSection("MongoDB"));
builder.Services.AddSingleton<WarehouseMongoContext>();

// WarehouseRepo
builder.Services.AddScoped<IWarehouseRepository, WarehouseRepository>();
builder.Services.AddScoped<IEquipmentStockRepository, EquipmentStockRepository>();
builder.Services.AddScoped<IMaterialsStockRepository, MaterialsStockRepository>();
builder.Services.AddScoped<IToolsStockRepository, ToolsStockRepository>();

// WarehouseService
builder.Services.AddScoped<IWarehouseService, WarehouseService>();
builder.Services.AddScoped<IEquipmentStockService, EquipmentStockService>();
builder.Services.AddScoped<IMaterialsStockService, MaterialsStockService>();
builder.Services.AddScoped<IToolsStockService, ToolsStockService>();



// Добавляем контроллеры и Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Настроим Swagger, чтобы можно было вводить JWT-токен
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "Введите JWT токен в формате: Bearer {токен}",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Если в режиме разработки, то Swagger включён
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Подключаем JWT-аутентификацию и авторизацию
//app.UseAuthentication();
//app.UseAuthorization();

app.MapControllers();

app.Run();
