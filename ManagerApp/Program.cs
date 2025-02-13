using ManagerApp.Clients;
using ManagerApp.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// ��������� ������ ������ (����� ������ � ����������)
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(@"/var/data-protection"))
    .SetApplicationName("ManagerApp");

// ��������� �������������� � Cookie
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

// �������� ������ � ������������� � https
app.UseStaticFiles();
app.UseRouting();

// ��������� �������������� ����� ������������
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Auth}/{id?}");

app.Run();
