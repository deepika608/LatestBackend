using ChatForge.Models;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly MongoService _mongo;
    private readonly JwtService _jwt;

    public AuthController(MongoService mongo, JwtService jwt)
    {
        _mongo = mongo;
        _jwt = jwt;
    }

    // 🔹 REGISTER
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            if (request == null)
                return BadRequest(new { error = "Request is null" });

            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { error = "Email, Username or Password is missing" });
            }

            var existing = await _mongo.Users
                .Find(x => x.Email == request.Email)
                .FirstOrDefaultAsync();

            if (existing != null)
                return BadRequest(new { message = "User already exists" });

            var user = new User
            {
                Email = request.Email,
               // Username = request.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
            };

            await _mongo.Users.InsertOneAsync(user);

            return Ok(new { message = "Registration successful" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // 🔹 LOGIN
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] RegisterRequest request)
    {
        try
        {
            if (request == null)
                return BadRequest(new { error = "Request is null" });

            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { error = "Email or Password is missing" });
            }

            var dbUser = await _mongo.Users
                .Find(x => x.Email == request.Email)
                .FirstOrDefaultAsync();

            if (dbUser == null)
                return Unauthorized(new { error = "User not found" });

            // 🔐 Verify password
            if (!BCrypt.Net.BCrypt.Verify(request.Password, dbUser.PasswordHash))
                return Unauthorized(new { error = "Invalid password" });

            var token = _jwt.GenerateToken(dbUser.Email);

            Response.Cookies.Append("token", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = false, // ⚠️ set TRUE in production (HTTPS)
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddDays(7)
            });

            return Ok(new
            {
                message = "Login success",
                token
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // 🔹 LOGOUT
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("token", new CookieOptions
        {
            SameSite = SameSiteMode.None,
            Secure = false
        });

        return Ok(new { message = "Logged out successfully" });
    }
}

//using ChatForge.Models;
//using Microsoft.AspNetCore.Mvc;
//using MongoDB.Driver;

//[ApiController]
//[Route("api/auth")]
//public class AuthController : ControllerBase
//{
//    private readonly MongoService _mongo;
//    private readonly JwtService _jwt;

//    public AuthController(MongoService mongo, JwtService jwt)
//    {
//        _mongo = mongo;
//        _jwt = jwt;
//    }

//    // 🔹 REGISTER
//    [HttpPost("register")]
//    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
//    {
//        Console.WriteLine($"Email: {request?.Email}, Password: {request?.Password}");

//        if (request == null)
//            return BadRequest(new { error = "Request is null" });

//        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
//        {
//            return BadRequest(new { error = "Email or Password is missing" });
//        }

//        var existing = await _mongo.Users
//            .Find(x => x.Email == request.Email)
//            .FirstOrDefaultAsync();

//        if (existing != null)
//            return BadRequest(new { message = "User already exists" });

//        var user = new User
//        {
//            Email = request.Email,
//            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
//        };

//        await _mongo.Users.InsertOneAsync(user);

//        return Ok(new { message = "Registration successful" });
//    }

//    // 🔹 LOGIN
//    [HttpPost("login")]
//    public async Task<IActionResult> Login(User req)
//    {
//        try
//        {
//            if (string.IsNullOrEmpty(req.Email) || string.IsNullOrEmpty(req.PasswordHash))
//            {
//                return BadRequest(new { error = "Email or Password is missing" });
//            }

//            // ✅ Find user
//            var dbUser = await _mongo.Users
//                .Find(x => x.Email == req.Email)
//                .FirstOrDefaultAsync();

//            if (dbUser == null)
//                return Unauthorized(new { error = "User not found" });

//            // 🔐 Verify password
//            if (!BCrypt.Net.BCrypt.Verify(req.PasswordHash, dbUser.PasswordHash))
//                return Unauthorized(new { error = "Invalid password" });

//            // ✅ Generate JWT token
//            var token = _jwt.GenerateToken(dbUser.Email);

//            // 🍪 Save token in cookie (UPDATED)
//            Response.Cookies.Append("token", token, new CookieOptions
//            {
//                HttpOnly = true,                 // 🔒 JS cannot access
//                Secure = false,                  // ⚠️ true in production (HTTPS)
//                SameSite = SameSiteMode.None,   // 🔥 IMPORTANT for frontend (CORS)
//                Expires = DateTime.UtcNow.AddDays(7)
//            });

//            return Ok(new
//            {
//                message = "Login success",
//                token = token
//            });
//        }
//        catch (Exception ex)
//        {
//            return StatusCode(500, new { error = ex.Message });
//        }
//    }

//    // 🔹 LOGOUT
//    [HttpPost("logout")]
//    public IActionResult Logout()
//    {
//        // ❌ Remove cookie
//        Response.Cookies.Delete("token", new CookieOptions
//        {
//            SameSite = SameSiteMode.None,
//            Secure = false
//        });

//        return Ok(new { message = "Logged out successfully" });
//    }
//}