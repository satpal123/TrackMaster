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
    public interface ITimerHostedService : IHostedService {}
    public class TwitchBot : ITimerHostedService
    {
        private TwitchClient client;
        private readonly IHubContext<TrackistHub> _tracklisthubContext;
        private string _twitchUsername;
        private string _twitchPassword;
        private string _twitchChannel;
        private readonly ILogger _logger;
        private Timer _timer;
        private readonly DataFields _dataFields;


        private static TwitchBot _instance;

        public static TwitchBot Instance => _instance;

        public TwitchBot(IHubContext<TrackistHub> synchub, ILogger<TwitchBot> logger, DataFields dataFields)
        {
            _instance ??= this;
            _tracklisthubContext = synchub;
            _dataFields = dataFields;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("TwitchBot Service is Starting");

            DoWork();

            _timer = new Timer(CheckStatus, null, TimeSpan.Zero, TimeSpan.FromSeconds(20));

            return Task.CompletedTask;
        }

        private void CheckStatus(object state)
        {
            if(_dataFields.IsConnectedTwitch)
            {
                _tracklisthubContext.Clients.All.SendAsync("DeviceAndTwitchStatus", 2, "Connected to Twitch Bot!");
            }
        }

        private async void DoWork()
        {
            _logger.LogInformation("Timed Background Service is working.");

            if (!_dataFields.IsConnectedTwitch)
            {
                var result = await GetSetTwitchCredentials();

                if (result.TwitchCredentials != null)
                {
                    _twitchUsername = result.TwitchCredentials.Username;
                    _twitchPassword = result.TwitchCredentials.Password;
                    _twitchChannel = result.TwitchCredentials.Channel;

                    await Bot();
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _dataFields.IsConnectedTwitch = false;
            _dataFields.TwitchBotManuallyStopped = true;
            client.Disconnect();
            _logger.LogError("TwitchBot Service is Stopping");
            return Task.CompletedTask;
        }

        private async Task Bot()
        {
            try
            {
                if (string.IsNullOrEmpty(_twitchUsername) || string.IsNullOrEmpty(_twitchPassword))
                    throw new ArgumentException();

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
                client.OnConnectionError += Client_OnConnectionError;
                client.OnIncorrectLogin += Client_OnIncorrectLogin;
                client.OnConnected += Client_OnConnected;
                client.OnJoinedChannel += Client_OnJoinedChannel;
                client.Connect();

            }
            catch (Exception ex)
            {
                Console.WriteLine("Twitch Bot not configured! ");
                _logger.LogError("Twitch Bot not configured!" + ex.Message);
                await _tracklisthubContext.Clients.All.SendAsync("DeviceAndTwitchStatus", 2, ex.Message);
                _dataFields.IsConnectedTwitch = false;
            }
        }

        private void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            _tracklisthubContext.Clients.All.SendAsync("ReceiveMessage", "twitch", $"Connected to {e.Channel}");
        }

        private void Client_OnIncorrectLogin(object sender, OnIncorrectLoginArgs e)
        {
            _logger.LogError("Twitch Bot incorrect login");

            _tracklisthubContext.Clients.All.SendAsync("DeviceAndTwitchStatus", 2, "Twitch Bot incorrect login!");
        }

        private void Client_OnConnectionError(object sender, OnConnectionErrorArgs e)
        {
            _logger.LogError("Twitch Bot connection retry");

            _tracklisthubContext.Clients.All.SendAsync("DeviceAndTwitchStatus", 2, "Twitch Bot connection retry!");
        }

        private void Client_OnConnected(object sender, OnConnectedArgs e)
        {
            if (!_dataFields.TwitchBotManuallyStopped)
            {
                _dataFields.IsConnectedTwitch = true;
                _tracklisthubContext.Clients.All.SendAsync("DeviceAndTwitchStatus", 2, "Connected to Twitch Bot!");
            }
        }

        private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            MixStatus mixStatus = new(_dataFields);

            if (e.ChatMessage.Message == "!tr")
                client.SendMessage(e.ChatMessage.Channel, mixStatus.Mixstatus());

            if (e.ChatMessage.Message == "!last3tr")
                client.SendMessage(e.ChatMessage.Channel, mixStatus.TrackHistory());
        }

        public void CurrentTrackPlaying(string message)
        {
            MixStatus mixStatus = new(_dataFields);
            client.SendMessage(_twitchChannel, mixStatus.Mixstatus());
        }

        private async Task<MainSettingsModel> GetSetTwitchCredentials()
        {
            SettingsHelper settingsHelper = new(_dataFields);

            if (HybridSupport.IsElectronActive)
            {
                string path = await Electron.App.GetPathAsync(PathName.UserData);
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
