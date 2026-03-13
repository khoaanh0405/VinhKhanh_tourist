using System;
namespace Server.DTOs
{
    public class ReviewDto
    {
        public int ReviewId { get; set; }
        public string UserName { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
