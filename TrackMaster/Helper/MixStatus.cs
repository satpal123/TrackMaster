using TrackMaster.Services.Sniffy;
using System.Linq;
using System.Collections.Generic;
using System;

namespace TrackMaster.Helper
{
    public class MixStatus
    {
        private string player1;
        private string player2;
        private string player1_history;
        private string player2_history;
        private string tracksplaying;
        private string returnTrackHistory;
        private readonly DataFields _dataFields;
        
        public MixStatus(DataFields dataFields)
        {
            _dataFields = dataFields;
        }
        public string Mixstatus()
        {
            player1 = "";
            player2 = "";
            tracksplaying = "No tracks are currently playing! or DJ is playing from USB or Vinyl";            

            //Player 1
            if (_dataFields.Globalplayernumber1 == 11 & _dataFields.Globalplayerfader1 == "Fader open" & _dataFields.Globalplayerstatus1 == "Player is playing normally")
            {
                player1 = _dataFields.Trackpath;
            }

            //Player 2
            if (_dataFields.Globalplayernumber2 == 12 & _dataFields.Globalplayerfader2 == "Fader open" & _dataFields.Globalplayerstatus2 == "Player is playing normally")
            {
                player2 = _dataFields.Trackpath2;
            }

            if (player1 != "" & player2 != "")
            {
                tracksplaying = "Deck 1 : " + player1 + " <> Deck 2 : " + player2;
            }
            if (player1 != "" & player2 == "")
            {
                tracksplaying = "Deck 1 : " + player1;
            }
            if (player2 != "" & player1 == "")
            {
                tracksplaying = "Deck 2 : " + player2;
            }
            return tracksplaying;
        }

        public string TrackHistory()
        {
            player1_history = _dataFields.Trackpath;
            player2_history = _dataFields.Trackpath2;

            //Player 1
            if (_dataFields.Globalplayernumber1 == 11 & _dataFields.Globalplayerfader1 != "Fader open" & _dataFields.Globalplayerstatus1 != "Player is playing normally")
            {
                player1_history = null;
            }

            //Player 2
            if (_dataFields.Globalplayernumber2 == 12 & _dataFields.Globalplayerfader2 != "Fader open" & _dataFields.Globalplayerstatus2 != "Player is playing normally")
            {
                player2_history = null;
            }

            var trackListHistory = (from tr in _dataFields.TrackList
                                   where tr.ToString() != player1_history & tr.ToString() != player2_history
                                    select tr).Distinct().ToList();

            if (trackListHistory.Count > 3)
            {
                _dataFields.TrackList.RemoveAt(0);
                trackListHistory.RemoveAt(0);
            }

            if (trackListHistory.Count == 0)
            {
                returnTrackHistory = "No Tracks are in the History";
                return returnTrackHistory;
            }

            List<string> trackListHistorySeq = new();
            var x = 0;

            foreach (var track in trackListHistory)
            {
                if (trackListHistory != null)
                {
                    x++;
                    trackListHistorySeq.Add(x + ". " + track.ToString());
                }
            }

            returnTrackHistory = string.Join(", ", trackListHistorySeq.Select(x => x.ToString()).ToArray().Reverse());

            return returnTrackHistory;
        }
    }
}
