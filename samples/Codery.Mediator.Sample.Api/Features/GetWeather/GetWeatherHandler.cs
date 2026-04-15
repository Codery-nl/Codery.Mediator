using Codery.Mediator;

namespace Codery.Mediator.Sample.Api.Features.GetWeather;

public sealed class GetWeatherHandler : IRequestHandler<GetWeatherQuery, WeatherResponse>
{
    private static readonly string[] Summaries =
        ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

    public Task<WeatherResponse> Handle(GetWeatherQuery request, CancellationToken cancellationToken)
    {
        var random = Random.Shared;
        var response = new WeatherResponse(
            request.City,
            random.Next(-20, 55),
            Summaries[random.Next(Summaries.Length)]);

        return Task.FromResult(response);
    }
}
