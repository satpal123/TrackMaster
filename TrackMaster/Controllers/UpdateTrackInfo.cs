using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TrackMaster.Helper;
using TrackMaster.Hubs;
using TrackMaster.Services.DiscordServices;
using TrackMaster.Services.Sniffy;
using TrackMaster.Services.TwitchServices;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace TrackMaster.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UpdateTrackInfo : ControllerBase
    {
        private readonly IHubContext<TrackistHub> _tracklisthubContext;
        private readonly DataFields _dataFields;
        private readonly ILogger _logger;
        private readonly DiscordBot _discordBot;
        private readonly TwitchBot _twitchBot;

        public UpdateTrackInfo(IConfiguration configuration, IHubContext<TrackistHub> synchub, ILogger<Sniffy> logger, DataFields dataFields, TwitchBot twitchBot, DiscordBot discordBot)
        {
            _tracklisthubContext = synchub;
            _dataFields = dataFields;
            _logger = logger;
            _twitchBot = twitchBot;
            _discordBot = discordBot;
        }

        // POST api/<UpdateTrackInfo>
        [HttpPost]
        public void Post([FromBody] TrackModel track)
        {
            _dataFields.Vinyl = true;

            if (track != null) 
            {
                _dataFields.VinylTrackPlaying = string.Format("{0} - {1}", track.TrackArtist, track.TrackTitle);

                _tracklisthubContext.Clients.All.SendAsync("NowPlaying", track.TrackArtist, track.TrackTitle, null, false);

                _discordBot.SendMessageToDiscord(_dataFields.VinylTrackPlaying);
                TrackHistory(_dataFields.VinylTrackPlaying);
            }
        }

        private List<string> TrackHistory(string trackMetadata)
        {
            var checkHistoryTrackExists = (from tr in _dataFields.TrackList
                                           where tr.ToString() == trackMetadata
                                           select tr).ToList();

            if (checkHistoryTrackExists.Count > 0)
            {
                int getDuplicateTrackIndex = _dataFields.TrackList.IndexOf(checkHistoryTrackExists.FirstOrDefault());
                _dataFields.TrackList.RemoveAt(getDuplicateTrackIndex);
            }

            if (_dataFields.TrackList.Count > 3)
            {
                _dataFields.TrackList.RemoveAt(0);
            }

            _dataFields.TrackList.Add(trackMetadata);

            return _dataFields.TrackList;
        }
    }

    public class TrackModel
    {
        public string TrackArtist { get; set; }
        public string TrackTitle { get; set; }
    }
}
