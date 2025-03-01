using Discord;
using Discord.WebSocket;
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
    public class DiscordBot : BackgroundService
    {
        private DiscordSocketClient client;
        private readonly IHubContext<TrackistHub> _tracklisthubContext;
        private ulong _discordChannelId;
        private string _discordToken;
        private readonly ILogger<DiscordBot> _logger;
        private readonly DataFields _dataFields;
        private readonly SemaphoreSlim _doWorkLock = new(1, 1);
        private IUserMessage _dailyMessage;
        private DateTime _dailyMessageDate = DateTime.MinValue;
        private readonly MainSettingsModel _mainSettings;

        public DiscordBot(IHubContext<TrackistHub> synchub, ILogger<DiscordBot> logger, DataFields dataFields, MainSettingsModel mainSettings)
        {
            _tracklisthubContext = synchub;
            _dataFields = dataFields;
            _logger = logger;
            _mainSettings = mainSettings;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DiscordBot Service is starting.");

            // Attempt an initial connection if not connected.
            if (_dataFields.IsConnectedDiscord)
            {
                await SafeSendAsync("DeviceAndTwitchStatus", 4, "Connected to Discord Bot!", stoppingToken);
            }
            else
            {
                await DoWorkAsync(stoppingToken);
            }

            // Use a PeriodicTimer to periodically check connection status.
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    if (_dataFields.IsConnectedDiscord)
                    {
                        await SafeSendAsync("DeviceAndTwitchStatus", 4, "Connected to Discord Bot!", stoppingToken);
                    }
                    else
                    {
                        await DoWorkAsync(stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when the service is stopping.
            }
            _logger.LogInformation("DiscordBot Service is stopping.");
        }

        private async Task DoWorkAsync(CancellationToken token)
        {
            _logger.LogInformation("DiscordBot background work triggered.");

            if (!_dataFields.IsConnectedDiscord)
            {
                // Ensure only one DoWorkAsync runs at a time.
                if (!await _doWorkLock.WaitAsync(0, token))
                    return;

                try
                {                    
                    if (_mainSettings.DiscordCredentials != null)
                    {
                        _discordChannelId = _mainSettings.DiscordCredentials.ChannelId;
                        _discordToken = _mainSettings.DiscordCredentials.DiscordToken;

                        await BotAsync(token);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in DiscordBot DoWorkAsync");
                }
                finally
                {
                    _doWorkLock.Release();
                }
            }
        }

        private async Task BotAsync(CancellationToken token)
        {
            try
            {
                if ((_discordChannelId == 0) || string.IsNullOrEmpty(_discordToken))
                    throw new ArgumentException("Discord credentials are missing.");

                // If client is already connected, do not reconnect.
                if (client != null && client.LoginState == LoginState.LoggedIn)
                {
                    _logger.LogInformation("Discord client already connected.");
                    return;
                }

                client = new DiscordSocketClient();

                client.Log += LogAsync;
                client.Ready += async () =>
                {
                    _dataFields.IsConnectedDiscord = true;
                    await SafeSendAsync("DeviceAndTwitchStatus", 4, "Connected to Discord Bot!", token);
                };

                await client.LoginAsync(TokenType.Bot, _discordToken);
                await client.StartAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError("Discord Bot not configured: " + ex.Message);
                await SafeSendAsync("DeviceAndTwitchStatus", 4, ex.Message, token);
                _dataFields.IsConnectedDiscord = false;
            }
        }

        private Task LogAsync(LogMessage log)
        {
            _logger.LogInformation(log.ToString());
            return Task.CompletedTask;
        }

        /// <summary>
        /// Safely sends a SignalR message by catching and logging exceptions.
        /// </summary>
        private async Task SafeSendAsync(string method, object arg1, object arg2, CancellationToken token = default)
        {
            try
            {
                await _tracklisthubContext.Clients.All.SendAsync(method, arg1, arg2, token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending message '{method}'");
            }
        }

        public async Task SendMessageToDiscord(string message)
        {
            if (client == null)
            {
                _logger.LogWarning("Discord client is not initialized.");
                return;
            }

            if (client.GetChannel(_discordChannelId) is ITextChannel channel)
            {
                // Check if a daily message exists and is from today (using UTC date).
                if (_dailyMessage == null || _dailyMessageDate.Date != DateTime.UtcNow.Date)
                {
                    // Send a new message and store it.
                    _dailyMessage = await channel.SendMessageAsync(message,
                        options: new RequestOptions { RetryMode = RetryMode.RetryRatelimit },
                        flags: MessageFlags.SuppressNotification);
                    _dailyMessageDate = DateTime.UtcNow;
                }
                else
                {
                    // Append new content to the existing message.
                    // You might also choose to format the message differently (e.g. add a newline).
                    string newContent = _dailyMessage.Content + "\n" + message;
                    await channel.ModifyMessageAsync(_dailyMessage.Id, props => props.Content = newContent);
                }
            }
            else
            {
                _logger.LogWarning("Discord channel not found for ID: " + _discordChannelId);
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _dataFields.IsConnectedDiscord = false;
            _dataFields.DiscordBotManuallyStopped = true;
            _logger.LogInformation("DiscordBot Service is stopping.");

            if (client != null)
            {
                await client.StopAsync();
                await client.LogoutAsync();
                client.Dispose();
                client = null;
            }

            await base.StopAsync(cancellationToken);
        }
    }
}
