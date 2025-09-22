using Dal;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.WebSockets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NuGet.Protocol.Core.Types;
using System;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using towing_services.Hubs;
using towing_services.Models;
using static towing_services.Controllers.HomeController;



var builder = WebApplication.CreateBuilder(args);

Console.OutputEncoding = System.Text.Encoding.UTF8;




// إعدادات الخدمات
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.IsEssential = true;
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
    });

//
builder.Services.AddSignalR();
//
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.None;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

builder.Services.AddSignalR();
 

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "X-CSRF-TOKEN";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.None;
});



builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;//true
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
    {
        "application/json",
        "text/html",
        "application/xml",
        "text/css",
        "application/javascript"
    });
    options.Providers.Add<GzipCompressionProvider>();
});

builder.Services.AddIdentityCore<Admin>(options =>
{
    options.Password.RequireDigit = true;
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

})
.AddRoles<IdentityRole<int>>()
.AddSignInManager<SignInManager<Admin>>()
.AddEntityFrameworkStores<Towing_Collection>()
.AddDefaultTokenProviders();

//builder.Services.AddIdentity<Driver, IdentityRole<int>>()
//    .AddEntityFrameworkStores<Towing_Collection>()
//    .AddDefaultTokenProviders();

builder.Services.AddIdentity<Driver, IdentityRole<int>>(options =>
{
    // السماح بجميع الأحرف والرموز والأرقام باستثناء الرمز '@'
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._+/ ";
})
.AddEntityFrameworkStores<Towing_Collection>()
.AddDefaultTokenProviders();

builder.Services.AddScoped<RoleManager<IdentityRole<int>>>();
builder.Services.AddScoped<UserManager<Admin>>();
builder.Services.AddScoped<SignInManager<Admin>>();
builder.Services.AddScoped<UserManager<Driver>>();
builder.Services.AddScoped<SignInManager<Driver>>();
///


builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Home/Login";
    options.AccessDeniedPath = "/Home/NotFound1";
});

builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    options.TokenLifespan = TimeSpan.FromMinutes(30);
});

builder.Services.AddDbContext<Towing_Collection>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("Towing"));
});

var smtpConfig = builder.Configuration.GetSection("Smtp").Get<SmtpSettings>();
builder.Services.AddSingleton(smtpConfig);
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });


builder.Services.AddControllersWithViews()
    .AddRazorRuntimeCompilation()
    ////.AddMvcOptions(options => options.EnableEndpointRouting = false)////important
    .AddDataAnnotationsLocalization();

builder.Services.AddRazorPages();
builder.Services.AddMemoryCache();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Noound");
    app.UseHsts();
}


app.UseSession();
app.UseCors("AllowAllOrigins");
//app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseWebSockets();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

//using (var scope = app.Services.CreateScope())
//{
//    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Admin>>();
//    await CreateAdminUser(userManager);
//}
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<Towing_Collection>();
    dbContext.Database.Migrate();  // تطبيق الهجرات تلقائيًا إذا كانت هناك تغييرات جديدة في قاعدة البيانات

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Admin>>();
    await CreateAdminUser(userManager);  // إنشاء مستخدم الأدمن إذا لم يكن موجودًا
}


app.UseResponseCompression();

app.Use(async (context, next) =>
{
    if (context.Request.Path == "/ws" && context.WebSockets.IsWebSocketRequest)
    {
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        await HandleWebSocketAsync(webSocket);
    }
    else
    {
        await next();
    }
});

app.MapHub<OrderHub>("/orderHub"); 
Console.WriteLine("orderHub mapped to '/orderHub'");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Home}/{id?}"
    );


await app.RunAsync();

async Task HandleWebSocketAsync(WebSocket webSocket)
{
    var buffer = new byte[1024 * 4];
    WebSocketReceiveResult result;

    do
    {
        result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        if (result.MessageType == WebSocketMessageType.Text)
        {
            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
            Console.WriteLine($"Received: {message}");

            var responseMessage = "Hello from server!";
            var encodedMessage = Encoding.UTF8.GetBytes(responseMessage);
            await webSocket.SendAsync(new ArraySegment<byte>(encodedMessage), WebSocketMessageType.Text, true, CancellationToken.None);
        }
        else if (result.MessageType == WebSocketMessageType.Close)
        {
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
        }
    } while (!result.CloseStatus.HasValue);
}

async Task CreateAdminUser(UserManager<Admin> userManager)
{
    var admin = await userManager.FindByEmailAsync("jerjeous@gmail.com");
    if (admin == null)
    {
        admin = new Admin
        {
            UserName = "jerjeous",
            Email = "jerjeous@gmail.com"
        };

        var result = await userManager.CreateAsync(admin, "YourSecure@1");

        if (result.Succeeded)
        {
            Console.WriteLine("Admin user created successfully.");
            admin.IsAdmin = true;
            await userManager.UpdateAsync(admin);
            Console.WriteLine("Admin user is marked as 'IsAdmin'.");
        }
        else
        {
            Console.WriteLine($"Error creating admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }
    else
    {
        Console.WriteLine("Admin user already exists.");
    }
}