using FriendZoneHub.Server.Data;
using FriendZoneHub.Server.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Encodings.Web;
using NLog;
using NLog.Web;

var builder = WebApplication.CreateBuilder(args);
// add NLog to ASP.NET Core
builder.Logging.ClearProviders();
builder.Host.UseNLog();


// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//builder.Services.AddCors(

//    options => options.AddPolicy("CorsPolicy", builder =>
//    {
//        builder.WithOrigins("http://chat.mikaelmykha.dev").AllowAnyHeader().AllowAnyMethod().AllowCredentials();
//    }));

//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("AllowAll",
//        builder => builder
//            .AllowAnyOrigin()
//            .AllowAnyMethod()
//            .AllowAnyHeader());
//});

//builder.Services.AddCors(options =>
//{
//    options.AddDefaultPolicy(
//        builder => { builder
//            .WithOrigins("https://localhost:5173") // Allow your frontend origin
//            .AllowAnyMethod()
//            .WithMethods("GET", "POST")
//            .AllowCredentials(); // Allow credentials (cookies, tokens, etc.)
//});
//});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost",
        builder =>
        {
            builder.WithOrigins("https://localhost:5173") // Your React frontend origin
                   .AllowAnyMethod()
                   .AllowAnyHeader()
                   .AllowCredentials(); // Enable credentials if needed
        });
});



builder.Services.AddDbContext<ChatAppContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"),
                     ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };

    // SignalR negotiation
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];

            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) &&
                (path.StartsWithSegments("/chathub")))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});


builder.Services.AddSignalR().AddJsonProtocol(options =>
{
    options.PayloadSerializerOptions.PropertyNamingPolicy = null;
    options.PayloadSerializerOptions.DictionaryKeyPolicy = null;
   

});
//builder.Logging.ClearProviders(); builder.Host.UseNLog();
//builder.Services.AddCors(options => {
//    options.AddPolicy("CorsPolicy", builder =>
//    { builder.WithOrigins("http://chat.mikaelmykha.dev").AllowAnyHeader().AllowAnyMethod().AllowCredentials(); });
//});
var app = builder.Build();

//app.UseDefaultFiles();
//app.UseStaticFiles();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(
        options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "FriendZoneHub.Server v1"));
}
//app.UseCors("CorsPolicy");
app.UseCors("AllowLocalhost");

app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();

app.MapControllers();
app.MapHub<ChatHub>("/chathub");

app.MapFallbackToFile("/index.html");

app.Run();