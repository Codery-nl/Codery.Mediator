using Codery.Mediator;
using Codery.Mediator.Sample.Api.Behaviors;
using Codery.Mediator.Sample.Api.Features.GetWeather;
using Codery.Mediator.Sample.Api.Features.PlaceOrder;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddCoderyMediator(
    opts => opts.AddOpenBehavior(typeof(LoggingBehavior<,>)),
    typeof(Program).Assembly);

var app = builder.Build();

app.MapOpenApi();

if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference();
}

app.MapGet("/weather/{city}", async (string city, ISender sender) =>
        await sender.Send(new GetWeatherQuery(city)))
    .WithName("GetWeather")
    .WithSummary("Get weather forecast for a city")
    .Produces<WeatherResponse>();

app.MapPost("/orders", async (PlaceOrderCommand command, ISender sender) =>
    {
        await sender.Send(command);
        return Results.Accepted();
    })
    .WithName("PlaceOrder")
    .WithSummary("Place a new order")
    .Accepts<PlaceOrderCommand>("application/json")
    .Produces(StatusCodes.Status202Accepted);

app.Run();
