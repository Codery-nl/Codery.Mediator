using System.Runtime.CompilerServices;
using Codery.Mediator;

namespace Codery.Mediator.Sample.Api.Features.GetWeather;

public sealed record GetWeatherStreamQuery(string City) : IStreamRequest<WeatherResponse>;

public sealed class GetWeatherStreamHandler : IStreamRequestHandler<GetWeatherStreamQuery, WeatherResponse>
{
    private static readonly string[] Summaries =
        ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

    public async IAsyncEnumerable<WeatherResponse> Handle(
        GetWeatherStreamQuery request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (int i = 0; i < 5; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(200, cancellationToken);
            var temperatureC = Random.Shared.Next(-20, 55);
            yield return new WeatherResponse(request.City, temperatureC, Summaries[Random.Shared.Next(Summaries.Length)]);
        }
    }
}
