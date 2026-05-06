namespace ChatForge.Models
{
    public class ChatMessage
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string Question { get; set; }
        public string Answer { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string Role { get; set; }
        public string Content { get; set; }

    }
}
