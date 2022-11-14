using ElectronNET.API;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using TrackMaster.Helper;
using TrackMaster.Services.Sniffy;
using TrackMaster.Services.TwitchServices;

namespace TrackMaster
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
            .WriteTo.RollingFile("TrackMaster-log-{Date}.txt")
            .MinimumLevel.Error()
            .CreateLogger();

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseElectron(args);
                    webBuilder.UseKestrel();
                    webBuilder.UseUrls("http://*:8888");
                    webBuilder.UseStartup<Startup>();
                })
            .UseSerilog()
            .ConfigureServices(services =>
                {                    
                    services.AddHostedService<TwitchBot>(); //Connect to Twitch               
                    //Add more services to connect to here and create a service class under services.
                    services.AddHostedService<Sniffy>();
                    services.AddSingleton<DataFields>();
                });
    }
}
