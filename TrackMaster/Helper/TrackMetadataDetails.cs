using System;
using System.IO;

namespace TrackMaster.Helper
{
    public class TrackMetadataDetails
    {
        public static string GetTrackMetaData(string trackpath)
        {            
            var tfile = TagLib.File.Create(trackpath);

            //MemoryStream ms = new MemoryStream(tfile.Tag.Pictures[0].Data.Data);
            //System.Drawing.Image image = System.Drawing.Image.FromStream(ms);

            var image = Convert.ToBase64String(tfile.Tag.Pictures[0].Data.Data);

            string title = tfile.Tag.Title;
            string artist = tfile.Tag.FirstPerformer;

            if (artist == null)
            {
                return title;
            }
            else
            {
                return artist + " - " + title;
            }
        }
    }
}
