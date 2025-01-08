using FishyFlip.Models;

namespace minstances.Models
{
    public class RecordedLike
    {
        public Like like { get; set; }
        public bool Added { get; set; }
        public RecordedLike()
        {
        }
    }

    public class LikeProfile
    {
        public LikeProfile()
        {
            Uri = string.Empty;
            Likes = new List<RecordedLike>();
        }
        public LikeProfile(string uri)
        {

            Uri = uri;
        }
        public List<RecordedLike> Likes { get; set; }

        public string Uri { get; set; }

        public bool Added { get; set; }

    }
}
