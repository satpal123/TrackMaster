namespace TrackMaster.Helper
{
    public class TrackMetadataDetails
    {
        public static string GetTrackMetaData(string trackpath)
        {            
            var tfile = TagLib.File.Create(trackpath);
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
