using System;
using System.IO;

namespace TrackMaster.Helper
{
    public class TrackMetadataDetails
    {
        public static string GetTrackMetaDataFromFile(string trackpath)
        {
            string image = null;

            if (File.Exists(trackpath))
            {
                var tfile = TagLib.File.Create(trackpath);

                if (tfile.Tag.Pictures.Length != 0)
                {
                    image = Convert.ToBase64String(tfile.Tag.Pictures[0].Data.Data);
                }

                if (image != null)
                {
                    return image;
                }
            }
            return null;
        }
    }
}
