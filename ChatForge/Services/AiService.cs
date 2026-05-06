using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

public class AiService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;

    public AiService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    public async Task<string> GetReply(string message)
    {
        var apiKey = _config["OpenRouter:ApiKey"];

        if (string.IsNullOrEmpty(apiKey))
            return "API key missing";

        var requestBody = new
        {
            model = "openai/gpt-3.5-turbo",
            messages = new[]
            {
                new { role = "user", content = message }
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://openrouter.ai/api/v1/chat/completions");

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Headers.Add("HTTP-Referer", "http://localhost");
        request.Headers.Add("X-Title", "AIChatApp");

        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _http.SendAsync(request);

        var raw = await response.Content.ReadAsStringAsync();

        // 🔍 Debug (optional)
        Console.WriteLine("AI RAW RESPONSE:");
        Console.WriteLine(raw);

        if (!response.IsSuccessStatusCode)
        {
            return "AI Error: " + raw;
        }

        try
        {
            using var doc = JsonDocument.Parse(raw);

            var root = doc.RootElement;

            var reply = root
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return reply ?? "No response from AI";
        }
        catch
        {
            return "Error parsing AI response";
        }
    }
}