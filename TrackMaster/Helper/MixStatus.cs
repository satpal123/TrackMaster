using TrackMaster.Services.Sniffy;

namespace TrackMaster.Helper
{
    public class MixStatus
    {
        private string player1;
        private string player2;
        private string tracksplaying;
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
    }
}
