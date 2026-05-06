using ChatForge.Models;
using MongoDB.Driver;

public class MongoService
{
    private readonly IMongoDatabase _db;

    public MongoService(IConfiguration config)
    {
        var connectionString = config["Mongo:ConnectionString"];
        var databaseName = config["Mongo:Database"] ?? "AIChatBotDb";

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new Exception("MongoDB connection string is missing!");
        }

        var client = new MongoClient(connectionString);
        _db = client.GetDatabase(databaseName);
    }

    public IMongoCollection<Conversation> Conversations =>
        _db.GetCollection<Conversation>("Conversations");

    public IMongoCollection<User> Users =>
        _db.GetCollection<User>("Users");

    public IMongoCollection<ChatMessage> Messages =>
        _db.GetCollection<ChatMessage>("Messages");
}