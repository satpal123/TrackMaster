using ElectronNET.API;
using ElectronNET.API.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using TrackMaster.Helper;
using TrackMaster.Hubs;
using TrackMaster.Services.DiscordServices;
using TrackMaster.Services.Sniffy;
using TrackMaster.Services.TwitchServices;

namespace TrackMaster
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        public class DataFieldsInstance
        {
            public DataFields dataFields = new(); 
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.AddSignalR();
            services.AddElectron();
            services.AddSingleton(Configuration);            
            services.AddSingleton<IHostedService, Sniffy>();
            services.AddSingleton<DataFieldsInstance>();
            services.AddSingleton<TwitchBot>();
            services.AddSingleton<DiscordBot>();
            services.AddSingleton<IHostedService, TwitchBot>(serviceProvider =>
            {
                return TwitchBot.Instance;
            });
            services.AddSingleton<IHostedService, DiscordBot>(serviceProvider =>
            {
                return DiscordBot.Instance;
            });
            services.AddHostedService<TwitchBot>();
            services.AddHostedService<DiscordBot>();   

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {            
            if (env.IsDevelopment())
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

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapHub<TrackistHub>("/trackisthub");
            });

            Task.Run(async () =>
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
        }
    }
}
