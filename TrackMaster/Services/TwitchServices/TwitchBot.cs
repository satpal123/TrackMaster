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
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace TrackMaster.Services.TwitchServices
{
    public class TwitchBot : IHostedService
    {
        TwitchClient client;
        private readonly IHubContext<TrackistHub> _tracklisthubContext;
        private string _twitchUsername;
        private string _twitchPassword;
        private string _twitchChannel;
        private readonly ILogger _logger;
        private Timer _timer;
        private readonly DataFields _dataFields;

        public TwitchBot(IHubContext<TrackistHub> synchub, ILogger<TwitchBot> logger, DataFields dataFields)
        {
            _tracklisthubContext = synchub;
            _dataFields = dataFields;
            _logger = logger;            
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("TwitchBot Service is Starting");

            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
            
            return Task.CompletedTask;
        }

        private async void DoWork(object state)
        {
            _logger.LogInformation("Timed Background Service is working.");

            if (!_dataFields.IsConnected)
            {
                Console.WriteLine("Twitch Bot connection retry!");
                _logger.LogError("Twitch Bot connection retry");

                await _tracklisthubContext.Clients.All.SendAsync("DeviceAndTwitchStatus", 2, "Twitch Bot connection retry!");

                var result = await GetSetTwitchCredentials();

                if (result.TwitchCredentials != null)
                {
                    _twitchUsername = result.TwitchCredentials.Username;
                    _twitchPassword = result.TwitchCredentials.Password;
                    _twitchChannel = result.TwitchCredentials.Channel;

                    await Bot();
                }
            }
            else
            {
                await _tracklisthubContext.Clients.All.SendAsync("DeviceAndTwitchStatus", 2, "Connected to Twitch Bot!");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("TwitchBot Service is Stopping");
            return Task.CompletedTask;
        }

        private async Task Bot()
        { 
            try
            {
                if (string.IsNullOrEmpty(_twitchUsername) || string.IsNullOrEmpty(_twitchPassword))
                    throw new ArgumentException();

                await Task.Run(() =>
                {
                    ConnectionCredentials credentials = new(_twitchUsername, _twitchPassword);
                    var clientOptions = new ClientOptions
                    {
                        MessagesAllowedInPeriod = 750,
                        ThrottlingPeriod = TimeSpan.FromSeconds(30)
                    };
                    WebSocketClient customClient = new(clientOptions);
                    client = new TwitchClient(customClient);
                    client.Initialize(credentials, _twitchChannel);
                    client.OnMessageReceived += Client_OnMessageReceived;
                    client.OnConnected += Client_OnConnected;
                    client.Connect();
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Twitch Bot not configured! ");
                _logger.LogError("Twitch Bot not configured!" + ex.Message);
                await _tracklisthubContext.Clients.All.SendAsync("DeviceAndTwitchStatus", 2, ex.Message);
                _dataFields.IsConnected = false;
            }            
        }
        private void Client_OnConnected(object sender, OnConnectedArgs e)
        {
            _dataFields.IsConnected = true;
            _tracklisthubContext.Clients.All.SendAsync("ReceiveMessage", "twitch", $"Connected to {e.AutoJoinChannel}");            
        }

        private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            MixStatus mixStatus = new(_dataFields);

            if (e.ChatMessage.Message == "!tr")
                client.SendMessage(e.ChatMessage.Channel, mixStatus.Mixstatus());

            if (e.ChatMessage.Message == "!last3tr")
                client.SendMessage(e.ChatMessage.Channel, mixStatus.TrackHistory());
        }

        private async Task<Root> GetSetTwitchCredentials()
        {
            SettingsHelper settingsHelper = new(_dataFields);

            if (HybridSupport.IsElectronActive)
            {
                string path = await Electron.App.GetPathAsync(PathName.UserData);
                _dataFields.Appfullpath = path + @"\Settings.json";
                return settingsHelper.GetTwitchCredentials(_dataFields.Appfullpath);
            }
            else
            {
                _dataFields.Appfullpath = @"C:\Users\satpa\AppData\Roaming\Electron\Settings.json";
                return settingsHelper.GetTwitchCredentials(_dataFields.Appfullpath);
            }
        }
    }
}
