using BookCatalog.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens; // to use the class = TokenValidationParameters
using System.Text;
using Serilog; //added this for the logs
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Net;



var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();
builder.Host.UseSerilog();

// Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<BookService>(); 
builder.Services.AddSingleton<TokenService>(); 

var jwt = builder.Configuration.GetSection("Jwt");
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));


builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true, 
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = signingKey,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(opts =>
{
    opts.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
});

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
                         .CreateLogger("GlobalExceptionHandler");

        var ex = context.Features.Get<IExceptionHandlerPathFeature>()?.Error;

        var (status, title) = ex switch
        {
            ArgumentNullException or ArgumentException => (HttpStatusCode.BadRequest, "Bad Request"),
            UnauthorizedAccessException                => (HttpStatusCode.Unauthorized, "Unauthorized"),
            KeyNotFoundException                       => (HttpStatusCode.NotFound, "Not Found"),
            InvalidOperationException                  => (HttpStatusCode.Conflict, "Conflict"),
            _                                          => (HttpStatusCode.InternalServerError, "Internal Server Error")
        };

        logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);

        var problem = new ProblemDetails
        {
            Status   = (int)status,
            Title    = title,
            Type     = $"https://httpstatuses.com/{(int)status}",
            Instance = context.Request.Path
        };

        problem.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.StatusCode  = (int)status;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problem);
    });
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
