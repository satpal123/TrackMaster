using ElectronNET.API;
using ElectronNET.API.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using TrackMaster.Hubs;
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

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.AddSignalR();
            services.AddSingleton(Configuration);
            services.AddSingleton<IHostedService, Sniffy>();
            services.AddSingleton<IHostedService, TwitchBot>();
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
                    }
                };
                browserWindowOptions.Center = true;
                browserWindowOptions.Height = 800;
                browserWindowOptions.Width = 1400;     
                browserWindowOptions.AutoHideMenuBar = true;
                browserWindowOptions.Resizable = false;
                
                await Electron.WindowManager.CreateWindowAsync(browserWindowOptions);
            });

        }
    }
}
