using System.Collections.Generic;

namespace TrackMaster.Helper
{
    public class DataFields
    {
        //Deck 1 Data Fields
        public int Globalplayernumber1 { get; set; }
        public string Globalplayerstatus1 { get; set; }
        public string Globalplayerloadeddevice1 { get; set; }
        public string Globalplayermaster1 { get; set; }
        public string Globalplayerfader1 { get; set; }

        //Deck 2 Data Fields
        public int Globalplayernumber2 { get; set; }
        public string Globalplayerstatus2 { get; set; }
        public string Globalplayerloadeddevice2 { get; set; }
        public string Globalplayermaster2 { get; set; }
        public string Globalplayerfader2 { get; set; }

        //Common Data Fields
        public string Playername { get; set; }
        public string Appfullpath { get; set; }
        public bool ControllerFound { get; set; } = false;
        public string ControllerIP { get; set; }
        public List<string> TrackList { get; set; } = new List<string>();
        public bool Player1a { get; set; } = false;
        public bool Player2a { get; set; } = false;
        public bool IsConnected { get; set; }
        public bool BotManuallyStopped { get; set; }

        //Metadata Related Data Fields
        public string Trackpath { get; set; }
        public string Trackpath2 { get; set; }
        public string Tracktitle1 { get; set; }
        public string Trackartist1 { get; set; }
        public string Tracktitle2 { get; set; }
        public string Trackartist2 { get; set; }
        public string Albumartid1 { get; set; }
        public string Albumartid2 { get; set; }
        public string Duration1 { get; set; }
        public string Duration2 { get; set; }
        public string Key1 { get; set; }
        public string Key2 { get; set; }
        public string Genre1 { get; set; }
        public string Genre2 { get; set; }
    }
}
