using ManagerApp.Clients;
using ManagerApp.Services;
using Microsoft.AspNetCore.DataProtection;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Настройка защиты данных (ключи храним в контейнере)
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(@"/var/data-protection"))
    .SetApplicationName("ManagerApp");

builder.Services.AddHttpClient();
builder.Services.AddControllersWithViews();
builder.Services.AddAuthorization();
builder.Services.AddHttpClient<AuthServiceClient>();
builder.Services.AddSingleton<JsonService>();


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
app.UseAuthorization();
app.UseAntiforgery();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Auth}/{id?}");

app.Run();
