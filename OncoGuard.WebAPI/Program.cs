using Microsoft.EntityFrameworkCore;
using OncoGuard.Application.Interfaces.Auth;
using OncoGuard.Application.Interfaces.DailyLogs;
using OncoGuard.Application.Interfaces.FoodLogs;
using OncoGuard.Application.Interfaces.Labs;
using OncoGuard.Infrastructure.Persistence;
using OncoGuard.Infrastructure.Services.Auth;
using OncoGuard.Infrastructure.Services.DailyLogs;
using OncoGuard.Infrastructure.Services.FoodLogs;
using OncoGuard.Infrastructure.Services.Labs;
using OncoGuard.Application.Interfaces.Features;
using OncoGuard.Infrastructure.Services.Features;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddScoped<ILabService, LabService>();

builder.Services.AddScoped<IDailyLogService, DailyLogService>();

builder.Services.AddScoped<IFoodLogService, FoodLogService>();

builder.Services.AddScoped<IFeatureEngineeringService, FeatureEngineeringService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
