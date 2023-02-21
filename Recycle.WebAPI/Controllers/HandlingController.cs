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
        LogRequest(request);

        var runningPrice = 0d;
        var fractionPrice = 0d;
        var previousWeight = 0d;
        var isFirstWeight = true;
        foreach (var evt in request.History)
            switch (evt.Type)
            {
                case nameof(WeightWasMeasured):
                {
                    var wwm = evt as Event<WeightWasMeasured>;
                    if (isFirstWeight)
                    {
                        isFirstWeight = false;
                    }
                    else
                    {
                        var weightMeasured = previousWeight - wwm.Payload.Weight;
                        runningPrice += weightMeasured * fractionPrice;
                    }

                    previousWeight = wwm.Payload.Weight;
                    break;
                }
                case nameof(FractionWasSelected):
                {
                    var fws = evt as Event<FractionWasSelected>;
                    fractionPrice = GetFractionPrice(fws);
                    break;
                }
            }

        var response = new Event<PriceWasCalculated>
        {
            EventId = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.Now,
            Payload = new PriceWasCalculated
            {
                CardId = "123",
                PriceAmount = runningPrice,
                PriceCurrency = "EUR"
            }
        };

        LogResponse(response);
        return response;
    }

    private static double GetFractionPrice(Event<FractionWasSelected> fws)
    {
        return fws.Payload.FractionType switch
        {
            FractionTypes.ConstructionWaste => 0.15,
            FractionTypes.GreenWaste => 0.09,
            _ => 0
        };
    }

    private void LogRequest(RecycleRequest request)
    {
        logger.Log(LogLevel.Information,
            "/handle-command request => " + JsonSerializer.Serialize(request, JsonSerializationConfiguration.Default));
    }

    private void LogResponse(Event<PriceWasCalculated> response)
    {
        logger.Log(LogLevel.Information,
            "/handle-command response => "
            + JsonSerializer.Serialize(response, JsonSerializationConfiguration.Default));
    }
}
