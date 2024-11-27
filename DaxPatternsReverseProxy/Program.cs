using DaxPatternsReverseProxy;  // Make sure this matches the namespace where CustomProxy is defined
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Yarp.ReverseProxy;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add Firebase Admin SDK service
builder.Services.AddSingleton<FirebaseAuthService>();

// Add reverse proxy services (YARP)
builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Firebase token validation middleware
app.UseMiddleware<FirebaseTokenValidationMiddleware>();

// Use Custom Proxy middleware to check roles and authorization before proxying
app.UseMiddleware<CustomProxy>();

// Enable Swagger in development environment
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Define a test route for weather forecast (you can add more routes later)
var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

// Configure YARP reverse proxy routes
// app.UseEndpoints(endpoints =>
// {
//     endpoints.MapReverseProxy();
// });
app.MapReverseProxy();

// Run the app
app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

public class FirebaseAuthService
{
    public FirebaseAuthService()
    {
        FirebaseApp.Create(new AppOptions()
        {
            Credential = GoogleCredential.FromFile("/Users/uek/RiderProjects/DaxPatternsReverseProxy/firebaseConfig.json")
        });
    }

    public async Task<bool> ValidateTokenAsync(string idToken)
    {
        try
        {
            var decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken);
            return decodedToken != null;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
//www.api/getthisimage.png

public class FirebaseTokenValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly FirebaseAuthService _firebaseAuthService;

    public FirebaseTokenValidationMiddleware(RequestDelegate next, FirebaseAuthService firebaseAuthService)
    {
        _next = next;
        _firebaseAuthService = firebaseAuthService;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        // Look for Authorization header
        if (httpContext.Request.Headers.ContainsKey("Authorization"))
        {
            var token = httpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "").Trim();

            if (await _firebaseAuthService.ValidateTokenAsync(token))
            {
                // Token is valid, continue processing
                await _next(httpContext);
                return;
            }
        }

        // If validation fails, return Unauthorized
        httpContext.Response.StatusCode = 401;
        await httpContext.Response.WriteAsync("Unauthorized");
    }
}
