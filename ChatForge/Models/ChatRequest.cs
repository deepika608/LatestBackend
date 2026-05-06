public class ChatRequest
{
    public string Message { get; set; }

    // ✅ MAKE OPTIONAL
    public string? ConversationId { get; set; }
}