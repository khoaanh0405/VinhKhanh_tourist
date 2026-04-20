namespace Server.Models
{
    public class Playlist
    {
        public int PlaylistId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        // Navigation
        public ICollection<PlaylistItem> Items { get; set; } = new List<PlaylistItem>();
        public ICollection<QRCode> QRCodes { get; set; } = new List<QRCode>();
    }
}