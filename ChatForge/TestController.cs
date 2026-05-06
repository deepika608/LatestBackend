using ChatForge.Models;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
    private readonly MongoService _mongoService;

    public TestController(MongoService mongoService)
    {
        _mongoService = mongoService;
    }

    [HttpGet("seed")]
    public async Task<IActionResult> Seed()
    {
        var user = new User
        {
            Username = "test",
            PasswordHash = "test"
        };

        await _mongoService.Users.InsertOneAsync(user);

        return Ok("Inserted!");
    }
}