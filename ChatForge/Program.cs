using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

// ✅ Create Builder
var builder = WebApplication.CreateBuilder(args);

// ✅ Render Port Support
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(8080);
});

// ✅ Add Controllers
builder.Services.AddControllers();

// ✅ CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "https://chat-frontend-eta-three.vercel.app",
                "http://localhost:5173",
                "http://localhost:5174"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// ✅ Services
builder.Services.AddSingleton<MongoService>();
builder.Services.AddSingleton<JwtService>();
builder.Services.AddHttpClient<AiService>();

// ✅ JWT Key
var jwtKey = builder.Configuration["Jwt:Key"];

if (string.IsNullOrEmpty(jwtKey))
{
    throw new Exception("JWT Key missing in configuration");
}

// ✅ Authentication
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

        // ✅ Read JWT from Cookie OR Authorization Header
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // 🔹 Read from Cookie
                var token = context.Request.Cookies["token"];

                if (!string.IsNullOrEmpty(token))
                {
                    context.Token = token;
                }

                // 🔹 Fallback: Read from Authorization Header
                var authHeader = context.Request.Headers["Authorization"]
                    .FirstOrDefault();

                if (!string.IsNullOrEmpty(authHeader) &&
                    authHeader.StartsWith("Bearer "))
                {
                    context.Token = authHeader.Substring("Bearer ".Length);
                }

                return Task.CompletedTask;
            }
        };
    });

// ✅ Authorization
builder.Services.AddAuthorization();

// ✅ Swagger + JWT
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Enter JWT Token like: Bearer {your token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
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
            Array.Empty<string>()
        }
    });
});

// ✅ Build App
var app = builder.Build();

// ✅ Swagger
app.UseSwagger();
app.UseSwaggerUI();

// ✅ HTTPS
app.UseHttpsRedirection();

// ✅ CORS (must be before auth)
app.UseCors("AllowFrontend");

// ✅ Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// ✅ Controllers
app.MapControllers();

// ✅ Run App
app.Run();