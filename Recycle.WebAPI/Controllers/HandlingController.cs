using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Recycle.WebAPI.Messages;
using Recycle.WebAPI.Middleware;

namespace Recycle.WebAPI.Controllers;

[Route("/handle-command")]
public class HandlingController
{
    private readonly ILogger<HandlingController> logger;

    public HandlingController(ILogger<HandlingController> logger)
    {
        this.logger = logger;
    }

    [HttpPost]
    public Event Handle([FromBody] RecycleRequest request)
    {
        logger.Log(LogLevel.Information,
            "/handle-command request => " + JsonSerializer.Serialize(request, JsonSerializationConfiguration.Default));

        var measuredEvents = request.History
            .Where(x => x.Type == "WeightWasMeasured")
            .OrderBy(x => x.CreatedAt)
            .Cast<Event<WeightWasMeasured>>()
            .Select(x => x.Payload)
            .ToArray();

        var runningPrice = 0d;
        var fractionPrice = 0d;
        var previousWeight = 0d;
        var firstWeight = true;
        foreach (var evt in request.History)
            if (evt.Type == nameof(WeightWasMeasured))
            {
                var wwm = evt as Event<WeightWasMeasured>;
                if (firstWeight)
                {
                    firstWeight = false;
                }
                else
                {
                    var weightMeasured = previousWeight - wwm.Payload.Weight;
                    runningPrice += weightMeasured * fractionPrice;
                }

                previousWeight = wwm.Payload.Weight;
            }
            else if (evt.Type == nameof(FractionWasSelected))
            {
                var fws = evt as Event<FractionWasSelected>;
                fractionPrice = GetFractionPrice(fws);
            }

        var response = new Event<PriceWasCalculated>
        {
            EventId = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.Now,
            Payload = new PriceWasCalculated { CardId = "123", PriceAmount = runningPrice, PriceCurrency = "EUR" }
        };

        logger.Log(LogLevel.Information,
            "/handle-command response => "
            + JsonSerializer.Serialize(response, JsonSerializationConfiguration.Default));
        return response;
    }

    private static double GetFractionPrice(Event<FractionWasSelected> fws)
    {
        return fws.Payload.FractionType switch
        {
            "Construction waste" => 0.15,
            "Green waste" => 0.09,
            _ => 0
        };
    }
}
