using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using StrongTowing.Infrastructure.Data;
using StrongTowing.Core.Entities;
using StrongTowing.API.Services;

var builder = WebApplication.CreateBuilder(args);

// IIS Integration
builder.Services.Configure<IISServerOptions>(options =>
{
    options.AutomaticAuthentication = false;
});

// Forwarded Headers (for IIS reverse proxy)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | 
                                ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// 1. Database Connection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.CommandTimeout(30); // 30 second timeout
    });
    if (!builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging(false);
    }
});

// 2. Identity (Auth)
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    
    // User settings
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// 3. JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] 
    ?? throw new InvalidOperationException("JWT SecretKey not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"] ?? "StrongTowingAPI",
        ValidAudience = jwtSettings["Audience"] ?? "StrongTowingClient",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

// 4. Register JWT Service
builder.Services.AddScoped<IJwtService, JwtService>();

// 5. Register Role Seeder Service
builder.Services.AddScoped<RoleSeederService>();

// 6. Add Controllers with validation
builder.Services.AddControllers(options =>
{
    // Return 400 Bad Request for invalid model state
    options.ModelValidatorProviders.Clear();
})
.ConfigureApiBehaviorOptions(options =>
{
    // Custom error response for invalid model state
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(x => x.Value?.Errors.Count > 0)
            .SelectMany(x => x.Value!.Errors.Select(e => new 
            { 
                field = x.Key.ToLower(), 
                message = string.IsNullOrEmpty(e.ErrorMessage) ? "Invalid value" : e.ErrorMessage 
            }))
            .ToList();

        return new BadRequestObjectResult(new
        {
            error = "Validation Error",
            message = "One or more validation errors occurred",
            errors = errors
        });
    };
    
    // Handle unsupported content types
    options.SuppressModelStateInvalidFilter = false;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// IIS Forwarded Headers (must be first)
app.UseForwardedHeaders();

// CORS (must be before UseAuthentication and UseAuthorization)
app.UseCors("AllowAngularApp");

// 7. Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// 7. Auto-migrate Database on Startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        logger.LogInformation("Applying database migrations...");
        dbContext.Database.Migrate();
        logger.LogInformation("Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating the database");
        // Don't throw - allow app to start even if migration fails
    }
}

// 8. Seed Roles on Startup
using (var scope = app.Services.CreateScope())
{
    var roleSeeder = scope.ServiceProvider.GetRequiredService<RoleSeederService>();
    try
    {
        await roleSeeder.SeedRolesAsync();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding roles");
    }
}

// 9. Health Check Endpoint
app.MapGet("/api/health", () => Results.Ok(new { Status = "Live", ServerTime = DateTime.UtcNow }));

app.Run();