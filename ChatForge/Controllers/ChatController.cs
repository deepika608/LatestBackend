using ChatForge.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson; // ✅ IMPORTANTs
using MongoDB.Driver;
using static System.Runtime.InteropServices.JavaScript.JSType;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly MongoService _mongo;
    private readonly AiService _ai;

    public ChatController(MongoService mongo, AiService ai)
    {
        _mongo = mongo;
        _ai = ai;
    }

    // ✅ POST: send message (FIXED)
    [HttpPost]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
    {
        var userEmail = User.Identity?.Name;

        if (string.IsNullOrEmpty(userEmail))
            return Unauthorized();

        if (request == null || string.IsNullOrWhiteSpace(request.Message))
            return BadRequest("Message cannot be empty");

        Conversation convo;

        // 🔥 CONTINUE EXISTING CHAT (ONLY if valid ObjectId)
        if (!string.IsNullOrWhiteSpace(request.ConversationId) &&
            ObjectId.TryParse(request.ConversationId, out _)) // ✅ FIX
        {
            convo = await _mongo.Conversations
                .Find(x => x.Id == request.ConversationId && x.UserEmail == userEmail)
                .FirstOrDefaultAsync();

            if (convo == null)
                return BadRequest("Conversation not found");
        }
        else
        {
            // 🔥 CREATE NEW CHAT (AUTO ID)
            convo = new Conversation
            {
                // ✅ Mongo will generate Id automatically
                UserEmail = userEmail,
                Title = request.Message.Substring(0, Math.Min(20, request.Message.Length)),
                Messages = new List<ChatMessage>(),
                CreatedAt = DateTime.UtcNow
            };
        }

        // ✅ save user message
        convo.Messages.Add(new ChatMessage
        {
            Role = "user",
            Content = request.Message,
            CreatedAt = DateTime.UtcNow
        });

        // ✅ AI response
        var reply = await _ai.GetReply(request.Message);

        // ✅ save AI message
        convo.Messages.Add(new ChatMessage
        {
            Role = "ai",
            Content = reply,
            CreatedAt = DateTime.UtcNow
        });

        // ✅ save to DB
        if (string.IsNullOrWhiteSpace(convo.Id)) // ✅ FIX (not request.ConversationId)
        {
            await _mongo.Conversations.InsertOneAsync(convo); // new chat
        }
        else
        {
            await _mongo.Conversations.ReplaceOneAsync(
                x => x.Id == convo.Id && x.UserEmail == userEmail,
                convo
            );
        }

        // ✅ return response + conversationId
        return Ok(new
        {
            response = reply,
            conversationId = convo.Id
        });
    }

    // ✅ GET: history
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory()
    {
        var userEmail = User.Identity?.Name;

        if (string.IsNullOrEmpty(userEmail))
            return Unauthorized();

        var convo = await _mongo.Conversations
            .Find(x => x.UserEmail == userEmail)
            .FirstOrDefaultAsync();

        return Ok(convo?.Messages ?? new List<ChatMessage>());
    }

    // ✅ GET: all conversations (sidebar)
    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations()
    {
        var userEmail = User.Identity?.Name;

        if (string.IsNullOrEmpty(userEmail))
            return Unauthorized();

        var chats = await _mongo.Conversations
            .Find(x => x.UserEmail == userEmail)
            .SortByDescending(x => x.CreatedAt)
            .ToListAsync();

        return Ok(chats);
    }
}