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
        private string tracksplaying;
        private string returnTrackHistory;

        public string Mixstatus()
        {
            player1 = "";
            player2 = "";
            tracksplaying = "No tracks are currently playing! or DJ is playing from USB or Vinyl";

            //Player 1
            if (Sniffy.globalplayernumber1 == 11 & Sniffy.globalplayerfader1 == "Fader open" & Sniffy.globalplayerstatus1 == "Player is playing normally")
            {
                player1 = Sniffy.trackpath;
            }

            //Player 2
            if (Sniffy.globalplayernumber2 == 12 & Sniffy.globalplayerfader2 == "Fader open" & Sniffy.globalplayerstatus2 == "Player is playing normally")
            {
                player2 = Sniffy.trackpath2;
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
            //Player 1
            if (Sniffy.globalplayernumber1 == 11 & Sniffy.globalplayerfader1 != "Fader open" & Sniffy.globalplayerstatus1 != "Player is playing normally")
            {
                Sniffy.trackpath = null;
            }

            //Player 2
            if (Sniffy.globalplayernumber2 == 12 & Sniffy.globalplayerfader2 != "Fader open" & Sniffy.globalplayerstatus2 != "Player is playing normally")
            {
                Sniffy.trackpath2 = null;
            }

            var trackListHistory = (from tr in Sniffy.trackList
                                   where tr.ToString() != Sniffy.trackpath & tr.ToString() != Sniffy.trackpath2
                                   select tr).ToList();

            if (trackListHistory.Count > 3)
            {
                Sniffy.trackList.RemoveAt(0);
                trackListHistory.RemoveAt(0);
            }

            if (trackListHistory.Count == 0)
            {
                returnTrackHistory = "No Tracks are in the History";
                return returnTrackHistory;
            }

            List<string> trackListHistorySeq = new List<string>();
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
