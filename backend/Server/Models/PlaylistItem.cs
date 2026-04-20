namespace Server.Models
{
    public class PlaylistItem
    {
        public int PlaylistItemId { get; set; }
        public int PlaylistId { get; set; }
        public int PoiId { get; set; }
        public int DisplayOrder { get; set; }

        // Navigation
        public Playlist Playlist { get; set; } = null!;
        public POI POI { get; set; } = null!;
    }
}