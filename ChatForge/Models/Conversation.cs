using ChatForge.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ChatForge.Models
{
    public class Conversation
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        public string UserEmail { get; set; } = string.Empty;   // ✅ FIX
        public string Title { get; set; } = string.Empty;

        public List<ChatMessage> Messages { get; set; } = new(); // ✅ FIX

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}