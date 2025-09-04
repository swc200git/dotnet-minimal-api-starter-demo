using Api.Data;
using Api.Models;
using Api.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// smart database provider selection based on connection string pattern
// default to SQLite with data folder
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? Environment.GetEnvironmentVariable("ConnectionStrings_Default")
    ?? "Data Source=data/app.db";

builder.Services.AddDbContext<AppDb>(options =>
{
    if (connectionString.Contains("Server=")
        || connectionString.Contains("Data Source=")
        && connectionString.Contains(','))
    {
        // SQL Server for production
        options.UseSqlServer(connectionString);
        Console.WriteLine("Using SQL Server database");
    }
    else
    {
        // SQLite for development
        options.UseSqlite(connectionString);
        Console.WriteLine("Using SQLite database");
    }
});

// JWT Authentication configuration with secure defaults
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? "development_secret_change_me_minimum_32_characters";
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false, // Disable for demo; enable in production
            ValidateAudience = false, // Disable for demo; enable in production
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true, 
            IssuerSigningKey = signingKey, 
            ClockSkew = TimeSpan.FromMinutes(2) // Allow 2 minutes clock drift
        };
    });

builder.Services.AddAuthorization();

// CORS policy to allow local development; restrict in production
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocal", policy =>
    {
        // allow for React and Vite default development server
        policy.WithOrigins(
                "http://localhost:3000",
                "http://127.0.0.1:3000", 
                "http://localhost:5173",
                "http://127.0.0.1:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// .NET 9 OpenAPI configuration with JWT Bearer support
builder.Services.AddOpenApi(options =>
{
    // Add JWT auth to Swagger
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();  
});

// Health checks for monitoring and load balancers
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure request pipeline
if (app.Environment.IsDevelopment())
{
    // Enable OpenAPI endpoint
    app.MapOpenApi();                           
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Minimal API v1");
    });
}

app.UseCors("AllowLocal");
// Enable JWT authentication and authorization middleware                       
app.UseAuthentication();                      
app.UseAuthorization();                        

// Health check endpoint for monitoring
app.MapHealthChecks("/health");

// Public API endpoints (no authentication required)
app.MapGet("/todos", async (AppDb db) =>
    // AsNoTracking for read-only operations
    await db.Todos.AsNoTracking().ToListAsync())  
    .WithName("GetTodos")
    .WithOpenApi();

app.MapPost("/todos", async (AppDb db, Todo todo) =>
{
    db.Todos.Add(todo);                           
    await db.SaveChangesAsync();
    // Return 201 Created with location header                  
    return Results.Created($"/todos/", todo); 
})
.WithName("CreateTodo")
.WithOpenApi();

// Secured endpoint requiring JWT authentication
app.MapGet("/secure/todos", async (AppDb db) =>
    await db.Todos.AsNoTracking().ToListAsync())
    .WithName("GetSecureTodos")
    .WithOpenApi()
    .RequireAuthorization();                      

// Authentication endpoint to get JWT tokens
app.MapPost("/auth/token", (UserLogin login) =>
{
    // Demo credentials check; replace with real user validation
    if (login.Username != "demo" || login.Password != "demo")
        return Results.Unauthorized();

    var claims = new[] { new Claim(ClaimTypes.Name, login.Username) };  
    var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        claims: claims,
        expires: DateTime.UtcNow.AddHours(1),      // 1 hour expiration
        signingCredentials: creds);

    var jwt = new JwtSecurityTokenHandler().WriteToken(token);
    return Results.Ok(new { token = jwt });        
})
.WithName("GetToken")
.WithOpenApi();

// Ensure database exists on startup (works for both SQLite and SQL Server)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDb>();
    try
    {
        await db.Database.EnsureCreatedAsync();     
        Console.WriteLine("Database initialized successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database initialization failed: {ex.Message}");        
    }
}

app.Run();
