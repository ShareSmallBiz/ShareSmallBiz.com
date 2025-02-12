﻿namespace ShareSmallBiz.Aspire.ApiService.WeatherService;

public interface IWeatherForecastService
{
    WeatherForecast[] GetForecast();
}

public class WeatherForecastService : IWeatherForecastService
{
    private static readonly string[] Summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

    public WeatherForecast[] GetForecast()
    {
        return Enumerable.Range(1, 5).Select(index =>
            new WeatherForecast
            (
                DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                Random.Shared.Next(-20, 55),
                Summaries[Random.Shared.Next(Summaries.Length)]
            ))
            .ToArray();
    }
}

public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
public static class WeatherForecastExtensions
{
    public static void RegisterWeatherServices(this IServiceCollection services)
    {
        // Register the WeatherForecastService
        services.AddSingleton<IWeatherForecastService, WeatherForecastService>();
    }

    public static void MapWeatherEndpoints(this IEndpointRouteBuilder app)
    {
        // Register the /weatherforecast endpoint
        app.MapGet("/weatherforecast", (IWeatherForecastService weatherService) =>
        {
            return weatherService.GetForecast();
        })
        .WithName("GetWeatherForecast");
    }
}