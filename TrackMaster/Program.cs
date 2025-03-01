using ElectronNET.API;
using ElectronNET.API.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using TrackMaster.Helper;
using TrackMaster.Hubs;
using TrackMaster.Services.DiscordServices;
using TrackMaster.Services.Sniffy;
using TrackMaster.Services.TwitchServices;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.RollingFile("TrackMaster-log-{Date}.txt")
    .MinimumLevel.Error()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Logging.AddConsole();

// Configure Electron.NET and hosting options
builder.WebHost.UseElectron(args)
               .UseKestrel()
               .UseUrls("http://*:8888");

var dataFields = new DataFields();

var mainSettings = await BotSettingsHelper.GetSettingsAsync(dataFields);

builder.Services.AddSingleton(mainSettings);

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddElectron();

// The Sniffy service
builder.Services.AddSingleton<IHostedService, Sniffy>();
builder.Services.AddSingleton(dataFields); 

// Register TwitchBot as a singleton and as a hosted service using the same instance.
builder.Services.AddSingleton<TwitchBot>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<TwitchBot>());

// Register DiscordBot as a singleton and as a hosted service using the same instance.
builder.Services.AddSingleton<DiscordBot>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<DiscordBot>());

// Build the app
var app = builder.Build();

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// Map routes and hubs
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapHub<TrackistHub>("/trackisthub");

// Create the Electron browser window
var browserWindowOptions = new BrowserWindowOptions
{
    WebPreferences = new WebPreferences
    {
        NodeIntegration = false
    },
    Center = true,
    Height = 880,
    Width = 1450,
    AutoHideMenuBar = true,
    Resizable = true,
    HasShadow = true
};

var browserWindow = await Electron.WindowManager.CreateWindowAsync(browserWindowOptions);
browserWindow.Show();
browserWindow.Reload();

app.Run();
