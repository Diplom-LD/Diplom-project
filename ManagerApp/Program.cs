using ManagerApp.Clients;
using ManagerApp.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Настройка защиты данных (ключи храним в контейнере)
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(@"/var/data-protection"))
    .SetApplicationName("ManagerApp");

// Добавляем аутентификацию с Cookie
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Auth";
        options.LogoutPath = "/ManagerHome/Logout";
        options.AccessDeniedPath = "/Auth/Auth"; 
        options.ExpireTimeSpan = TimeSpan.FromDays(7); 
    });

builder.Services.AddAuthorization();
builder.Services.AddHttpClient();
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient<AuthServiceClient>();
builder.Services.AddScoped<BTUCalcServiceClient>();
builder.Services.AddSingleton<JsonService>();

// Подключение OrderServiceClient
var orderServiceBaseUrl = builder.Configuration["OrderService:BaseUrl"];
if (string.IsNullOrEmpty(orderServiceBaseUrl))
{
    throw new InvalidOperationException("OrderService__BaseUrl is not configured.");
}

// Подключение WarehouseServiceClient
builder.Services.AddHttpClient<WarehouseServiceClient>(client =>
{
    var baseUrl = builder.Configuration["WarehouseService:BaseUrl"];
    if (string.IsNullOrEmpty(baseUrl))
        throw new InvalidOperationException("WarehouseService BaseUrl is missing!");

    client.BaseAddress = new Uri(baseUrl);
});


builder.Services.AddHttpClient<OrderServiceClient>(client =>
{
    client.BaseAddress = new Uri(orderServiceBaseUrl);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

// Добавить работу с сертификатами и https
app.UseStaticFiles();
app.UseRouting();

// Добавляем аутентификацию перед авторизацией
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Auth}/{id?}");

app.Run();
