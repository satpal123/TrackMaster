using System;

namespace TrackMaster.Helper
{
    public class OverlayChangeObserver
    {
        private static int elapsedTime_Player1 = 0;
        private static int elapsedTime_Player2 = 0;

        public event EventHandler<MixStatusChangedEventArgs> MixStatusChanged;
        private readonly DataFields _dataFields;

        public OverlayChangeObserver(DataFields dataFields)
        {
            _dataFields = dataFields;
        }

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
                if (_dataFields.Globalplayernumber1 == 11 & _dataFields.Globalplayerfader1 == "Fader open" & _dataFields.Globalplayerstatus1 == "Player is playing normally" & _dataFields.Player1a == false)
                {
                    elapsedTime_Player1++;

                    if (elapsedTime_Player1 == 30)
                    {   
                        Player1 = true;
                        _dataFields.Player1a = true;
                        hasChanges = true;
                        elapsedTime_Player1 = 0;
                    }                   
                }
                else if (_dataFields.Globalplayerfader1 == "Fader closed" || _dataFields.Globalplayerstatus1 == "Player is paused at the cue point")
                {
                    elapsedTime_Player1 = 0;
                    Player1 = false;
                    _dataFields.Player1a = false;
                    hasChanges = true;
                }

                //Player 2
                if (_dataFields.Globalplayernumber2 == 12 & _dataFields.Globalplayerfader2 == "Fader open" & _dataFields.Globalplayerstatus2 == "Player is playing normally" & _dataFields.Player2a == false)
                {
                    elapsedTime_Player2++;

                    if (elapsedTime_Player2 == 30)
                    {
                        Player2 = true;
                        _dataFields.Player2a = true;
                        hasChanges = true;
                        elapsedTime_Player2 = 0;
                    }                                
                }
                else if (_dataFields.Globalplayerfader2 == "Fader closed" || _dataFields.Globalplayerstatus2 == "Player is paused at the cue point")
                {
                    elapsedTime_Player2 = 0;
                    Player2 = false;
                    _dataFields.Player2a = false;
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
