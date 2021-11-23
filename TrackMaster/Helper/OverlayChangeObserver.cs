using System;
using System.Collections.Generic;
using System.Diagnostics;
using TrackMaster.Services.Sniffy;

namespace TrackMaster.Helper
{
    public class OverlayChangeObserver
    {
        private static int elapsedTime_Player1 = 0;
        private static int elapsedTime_Player2 = 0;

        public static bool _Player1 = false;
        public static bool _Player2 = false;

        public event EventHandler<MixStatusChangedEventArgs> MixStatusChanged;

        public void Start()
        {
            UpdateMixStatus();
        }

        private void UpdateMixStatus()
        {
            bool hasChanges = false;    
            bool Player1 = false;
            bool Player2 = false;

            if (!hasChanges)
            {
                //Player 1
                if (Sniffy.globalplayernumber1 == 11 & Sniffy.globalplayerfader1 == "Fader open" & Sniffy.globalplayerstatus1 == "Player is playing normally" & _Player1 == false)
                {
                    elapsedTime_Player1++;
                    Console.WriteLine("Player 1 : " + elapsedTime_Player1);

                    if (elapsedTime_Player1 == 20)
                    {   
                        Player1 = true;
                        _Player1 = true;
                        hasChanges = true;
                        elapsedTime_Player1 = 0;
                    }                   
                }
                else if (Sniffy.globalplayerfader1 == "Fader closed" || Sniffy.globalplayerstatus1 == "Player is paused at the cue point")
                {
                    elapsedTime_Player1 = 0;
                    Player1 = false;
                    _Player1 = false;
                    hasChanges = true;
                }

                //Player 2
                if (Sniffy.globalplayernumber2 == 12 & Sniffy.globalplayerfader2 == "Fader open" & Sniffy.globalplayerstatus2 == "Player is playing normally" & _Player2 == false)
                {
                    elapsedTime_Player2++;
                    Console.WriteLine("Player 2 : " + elapsedTime_Player2);

                    if (elapsedTime_Player2 == 20)
                    {
                        Player2 = true;
                        _Player2 = true;
                        hasChanges = true;
                        elapsedTime_Player2 = 0;
                    }                                
                }
                else if (Sniffy.globalplayerfader2 == "Fader closed" || Sniffy.globalplayerstatus2 == "Player is paused at the cue point")
                {
                    elapsedTime_Player2 = 0;
                    Player2 = false;
                    _Player2 = false;
                    hasChanges = true;
                }
            }
           
            if (hasChanges)
            {
                RaiseNetworkChanged(Player1, Player2);
            }
        }

        private void RaiseNetworkChanged(bool Player1, bool Player2)
        {
            if (MixStatusChanged != null)
            {
                MixStatusChanged.Invoke(this, new MixStatusChangedEventArgs() { Player1 = Player1, Player2= Player2 });
            }
        }
        public class MixStatusChangedEventArgs : EventArgs
        {
            public bool Player1;
            public bool Player2;
        }
    }
   
}
