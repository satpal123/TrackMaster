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
    public class TwitchBot : BackgroundService
    {
        private TwitchClient client;
        private readonly IHubContext<TrackistHub> _tracklisthubContext;
        private string _twitchUsername;
        private string _twitchPassword;
        private string _twitchChannel;
        private readonly ILogger _logger;
        private readonly DataFields _dataFields;
        private readonly SemaphoreSlim _doWorkLock = new(1, 1);
        private readonly MainSettingsModel _mainSettings;

        public TwitchBot(IHubContext<TrackistHub> synchub, ILogger<TwitchBot> logger, DataFields dataFields, MainSettingsModel mainSettings)
        {
            _tracklisthubContext = synchub;
            _dataFields = dataFields;
            _logger = logger;
            _mainSettings = mainSettings;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("TwitchBot Service is Starting");

            // If already connected, notify; otherwise, attempt to connect.
            if (_dataFields.IsConnectedTwitch)
            {
                await SafeSendAsync("DeviceAndTwitchStatus", 2, "Connected to Twitch Bot!");
            }
            else
            {
                await DoWorkAsync(stoppingToken);
            }

            // Use PeriodicTimer (available in .NET 6+ and in .NET 8) for periodic checks.
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    if (_dataFields.IsConnectedTwitch)
                    {
                        await SafeSendAsync("DeviceAndTwitchStatus", 2, "Connected to Twitch Bot!");
                    }
                    else
                    {
                        await DoWorkAsync(stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected on cancellation.
            }
            _logger.LogInformation("TwitchBot Service is stopping.");
        }

        private async Task DoWorkAsync(CancellationToken token)
        {
            // Ensure only one instance of DoWork runs at a time.
            if (!_dataFields.IsConnectedTwitch)
            {
                if (!await _doWorkLock.WaitAsync(0, token))
                    return;

                try
                {                   
                    if (_mainSettings.TwitchCredentials != null)
                    {
                        _twitchUsername = _mainSettings.TwitchCredentials.Username;
                        _twitchPassword = _mainSettings.TwitchCredentials.Password;
                        _twitchChannel = _mainSettings.TwitchCredentials.Channel;
                        _dataFields.AutopostTracktoTwitch = _mainSettings.OtherSettings.AutopostTracktoTwitch;

                        await BotAsync(token);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error in DoWork: {ex.Message}");
                }
                finally
                {
                    _doWorkLock.Release();
                }
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _dataFields.IsConnectedTwitch = false;
            _dataFields.TwitchBotManuallyStopped = true;
            client?.Disconnect();
            _logger.LogInformation("TwitchBot Service is stopping.");
            await base.StopAsync(cancellationToken);
        }

        private async Task BotAsync(CancellationToken token)
        {
            try
            {
                if (string.IsNullOrEmpty(_twitchUsername) || string.IsNullOrEmpty(_twitchPassword))
                    throw new ArgumentException("Twitch credentials are missing.");

                ConnectionCredentials credentials = new(_twitchUsername, _twitchPassword);
                var clientOptions = new ClientOptions
                {
                    MessagesAllowedInPeriod = 750,
                    ThrottlingPeriod = TimeSpan.FromSeconds(30)
                };
                WebSocketClient customClient = new(clientOptions);
                client = new TwitchClient(customClient);
                client.Initialize(credentials, _twitchChannel);

                // Attach event handlers.
                client.OnMessageReceived += Client_OnMessageReceived;
                client.OnConnectionError += Client_OnConnectionError;
                client.OnIncorrectLogin += Client_OnIncorrectLogin;
                client.OnConnected += Client_OnConnected;
                client.OnJoinedChannel += Client_OnJoinedChannel;

                client.Connect();
            }
            catch (Exception ex)
            {
                _logger.LogError("Twitch Bot not configured: " + ex.Message);
                await SafeSendAsync("DeviceAndTwitchStatus", 2, ex.Message);
                _dataFields.IsConnectedTwitch = false;
            }
        }

        private async void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            await SafeSendAsync("ReceiveMessage", "twitch", $"Connected to {e.Channel}");
        }

        private async void Client_OnIncorrectLogin(object sender, OnIncorrectLoginArgs e)
        {
            _logger.LogError("Twitch Bot incorrect login");

            await SafeSendAsync("DeviceAndTwitchStatus", 2, "Twitch Bot incorrect login!");
        }

        private async void Client_OnConnectionError(object sender, OnConnectionErrorArgs e)
        {
            _logger.LogError("Twitch Bot connection retry");

            await SafeSendAsync("DeviceAndTwitchStatus", 2, "Twitch Bot connection retry!");
        }

        private async void Client_OnConnected(object sender, OnConnectedArgs e)
        {
            if (!_dataFields.TwitchBotManuallyStopped)
            {
                _dataFields.IsConnectedTwitch = true;
                await SafeSendAsync("DeviceAndTwitchStatus", 2, "Connected to Twitch Bot!");
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
            if (_dataFields.AutopostTracktoTwitch)
            {
                client.SendMessage(_twitchChannel, "Currently Playing: " + message);
            }
        }

        /// <summary>
        /// Helper method to safely send SignalR messages.
        /// </summary>
        private async Task SafeSendAsync(string method, object arg1, object arg2)
        {
            try
            {
                await _tracklisthubContext.Clients.All.SendAsync(method, arg1, arg2);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending message '{method}': {ex.Message}");
            }
        }
    }
}
