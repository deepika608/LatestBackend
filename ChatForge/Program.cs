using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

// ✅ FIRST create builder
var builder = WebApplication.CreateBuilder(args);

// ✅ Add Controllers
builder.Services.AddControllers();

// ✅ CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "http://localhost:5174"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // 🔥 required for cookies
    });
});

// ✅ Services
builder.Services.AddSingleton<MongoService>();
builder.Services.AddSingleton<JwtService>();
builder.Services.AddHttpClient<AiService>();

// 🔐 JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"];

if (string.IsNullOrEmpty(jwtKey))
{
    throw new Exception("JWT Key missing in appsettings.json");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)
            )
        };

        // ✅ Read token from cookie OR header
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // 1️⃣ Check cookie
                var token = context.Request.Cookies["token"];

                if (!string.IsNullOrEmpty(token))
                {
                    context.Token = token;
                }

                // 2️⃣ Fallback: Authorization header
                var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
                {
                    context.Token = authHeader.Substring("Bearer ".Length);
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// ✅ Swagger + JWT Support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Enter: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

var app = builder.Build();

// ✅ Middleware Order (correct)

// Swagger
app.UseSwagger();
app.UseSwaggerUI();

// CORS (before auth)
app.UseCors("AllowFrontend");

// HTTPS
app.UseHttpsRedirection();

// Auth
app.UseAuthentication();
app.UseAuthorization();

// Controllers
app.MapControllers();

app.Run();