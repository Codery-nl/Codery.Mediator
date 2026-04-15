using Codery.Mediator;

namespace Codery.Mediator.Sample.Api.Features.GetWeather;

public sealed record GetWeatherQuery(string City) : IRequest<WeatherResponse>;

public sealed record WeatherResponse(string City, int TemperatureC, string Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
