using Discord;
using Discord.WebSocket;
using ElectronNET.API;
using ElectronNET.API.Entities;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using TrackMaster.Helper;
using TrackMaster.Hubs;
using TrackMaster.Models;

namespace TrackMaster.Services.DiscordServices
{   
    public class DiscordBot : IHostedService
    {
        private DiscordSocketClient client;
        private readonly IHubContext<TrackistHub> _tracklisthubContext;
        private ulong _discordChannelId;
        private string _discordToken;
        private readonly ILogger _logger;
        private static Timer _timer;
        private readonly DataFields _dataFields;

        private static DiscordBot _instance;
        public static DiscordBot Instance => _instance;

        public DiscordBot(IHubContext<TrackistHub> synchub, ILogger<DiscordBot> logger, DataFields dataFields)
        {
            _instance ??= this;
            _tracklisthubContext = synchub;
            _dataFields = dataFields;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("DiscordBot Service is Starting");

            Task.Run(async () =>
            {
                await DoWork(cancellationToken);

            }, cancellationToken);

            _timer = new Timer(CheckStatus, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));

            return Task.CompletedTask;
        }

        private void CheckStatus(object state)
        {
            if (_dataFields.IsConnectedDiscord)
            {
                _tracklisthubContext.Clients.All.SendAsync("DeviceAndTwitchStatus", 4, "Connected to Discord Bot!");
            }
            else
            {
                Task.Run(async () =>
                {
                    await DoWork(state);

                });
            }
        }

        private async Task DoWork(object state)
        {
            _logger.LogInformation("Timed Background Service is working.");

            if (!_dataFields.IsConnectedDiscord)
            {
                var result = await GetSetDiscordCredentials();

                if (result.DiscordCredentials != null)
                {
                    _discordChannelId = result.DiscordCredentials.ChannelId;
                    _discordToken = result.DiscordCredentials.DiscordToken;

                    await Bot();
                }
            }
        }

        private async Task Bot()
        {
            try
            {
                if ((_discordChannelId == 0) || string.IsNullOrEmpty(_discordToken))
                    throw new ArgumentException();

                client = new DiscordSocketClient();

                client.Log += LogAsync;
                client.Ready += ReadyAsync;

                await client.LoginAsync(TokenType.Bot, _discordToken);
                await client.StartAsync();

                client.Ready += () =>
                {
                    _dataFields.IsConnectedDiscord = true;
                    _tracklisthubContext.Clients.All.SendAsync("DeviceAndTwitchStatus", 4, "Connected to Discord Bot!");

                    return Task.CompletedTask;
                };
                //await Task.Delay(-1);
            }
            catch (Exception ex)
            {
                _logger.LogError("Discord Bot not configured!" + ex.Message);
                await _tracklisthubContext.Clients.All.SendAsync("DeviceAndTwitchStatus", 4, ex.Message);
                _dataFields.IsConnectedDiscord = false;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _dataFields.IsConnectedDiscord = false;
            _dataFields.DiscordBotManuallyStopped = true;
            _logger.LogError("DiscordBot Service is Stopping");
            return Task.CompletedTask;
        }

        private Task LogAsync(LogMessage log)
        {
            return Task.CompletedTask;
        }

        private Task ReadyAsync()
        {
            _tracklisthubContext.Clients.All.SendAsync("DeviceAndTwitchStatus", 4, "Connected to Discord Bot!");
            return Task.CompletedTask;
        }

        public async void SendMessageToDiscord(string message)
        {
            var channel = client.GetChannel(_discordChannelId) as ITextChannel;
            await channel.SendMessageAsync(message, false, flags: MessageFlags.SuppressNotification);
        }

        private async Task<MainSettingsModel> GetSetDiscordCredentials()
        {
            SettingsHelper settingsHelper = new(_dataFields);

            if (HybridSupport.IsElectronActive)
            {
                string path = await Electron.App.GetPathAsync(PathName.UserData);
                Console.WriteLine("Discord: " + path);
                _dataFields.Appfullpath = path + @"\Settings.json";
                return settingsHelper.GetSettings(_dataFields.Appfullpath);
            }
            else
            {
                var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                _dataFields.Appfullpath = appDataPath + @"\Electron\Settings.json";
                return settingsHelper.GetSettings(_dataFields.Appfullpath);
            }
        }
    }
}
