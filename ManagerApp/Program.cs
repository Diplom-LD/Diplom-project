using ManagerApp.Clients;
using ManagerApp.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Net.WebSockets;
using System.Text.Json.Serialization;

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
builder.Services.AddControllersWithViews()
    .AddJsonOptions(opt =>
    {
        opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
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
//builder.Services.AddHttpClient<IOrderServiceClient, OrderServiceClient>(client =>
//{
//    client.BaseAddress = new Uri(orderServiceBaseUrl);
//});

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
app.UseWebSockets();
app.UseRouting();

app.Use(async (context, next) =>
{
    if (context.WebSockets.IsWebSocketRequest &&
        context.Request.Path.StartsWithSegments("/technicians/orders"))
    {
        Console.WriteLine($"🌐 Проксируем WebSocket: {context.Request.Path}");

        var orderServiceUri = new Uri($"ws://order-service:8080{context.Request.Path}{context.Request.QueryString}");

        using var clientWebSocket = new ClientWebSocket();
        await clientWebSocket.ConnectAsync(orderServiceUri, CancellationToken.None);

        using var serverWebSocket = await context.WebSockets.AcceptWebSocketAsync();

        await ProxyWebSocket(clientWebSocket, serverWebSocket);
        return;
    }

    await next();
});

static async Task ProxyWebSocket(ClientWebSocket from, WebSocket to)
{
    var buffer = new byte[8192];

    var fromTo = Task.Run(async () =>
    {
        while (from.State == WebSocketState.Open)
        {
            var result = await from.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            if (result.CloseStatus.HasValue) break;
            await to.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None);
        }
    });

    var toFrom = Task.Run(async () =>
    {
        while (to.State == WebSocketState.Open)
        {
            var result = await to.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            if (result.CloseStatus.HasValue) break;
            await from.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None);
        }
    });

    await Task.WhenAny(fromTo, toFrom);

    await from.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
    await to.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
}

// Добавляем аутентификацию перед авторизацией
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Auth}/{id?}");

app.Run();


