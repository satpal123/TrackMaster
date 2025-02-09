using ElectronNET.API;
using ElectronNET.API.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Threading.Tasks;
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

// Access configuration
var configuration = builder.Configuration;

// Configure Electron.NET and hosting options
builder.WebHost.UseElectron(args)
               .UseKestrel()
               .UseUrls("http://*:8888");

// Add services to the container
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddElectron();
builder.Services.AddSingleton(configuration);
builder.Services.AddSingleton<IHostedService, Sniffy>();
builder.Services.AddSingleton<DataFields>();
builder.Services.AddSingleton<TwitchBot>();
builder.Services.AddSingleton<DiscordBot>();

// Add hosted services
builder.Services.AddSingleton<IHostedService, TwitchBot>(serviceProvider => TwitchBot.Instance);
builder.Services.AddSingleton<IHostedService, DiscordBot>(serviceProvider => DiscordBot.Instance);
builder.Services.AddHostedService<TwitchBot>();
builder.Services.AddHostedService<DiscordBot>();

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

// Configure Electron-specific logic
var task = Task.Run(async () =>
{
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
});

// Run the app
app.Run();
