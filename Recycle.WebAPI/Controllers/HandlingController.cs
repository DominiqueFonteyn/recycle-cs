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

        var idCardRegistered = request.History.Single(x => x.Type == nameof(IdCardRegistered)) as Event<IdCardRegistered>;
        var city = idCardRegistered?.Payload.City;
        
        var weightPerFractionType = new Dictionary<string, double>();
        var runningPrice = 0d;
        var fractionPrice = 0d;
        var previousWeight = 0d;
        var isFirstWeight = true;
        string fractionType = "";
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
                            if (!weightPerFractionType.ContainsKey(fractionType)) 
                                weightPerFractionType.Add(fractionType, 0);
                            weightPerFractionType[fractionType] += weightMeasured;
                            // runningPrice += weightMeasured * fractionPrice;
                        }

                        previousWeight = wwm.Payload.Weight;
                        break;
                    }
                case nameof(FractionWasSelected):
                    {
                        var fws = evt as Event<FractionWasSelected>;
                        fractionType = fws.Payload.FractionType;
                        // fractionPrice = GetFractionPrice(fws, city);
                        break;
                    }
            }
        
        //TODO: subtract exemptions from weightPerFactionType

        double total = weightPerFractionType
            .Sum(keyValuePair => keyValuePair.Value * GetFractionPrice(keyValuePair.Key, city));

        var response = new Event<PriceWasCalculated>
        {
            EventId = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.Now,
            Payload = new PriceWasCalculated
            {
                CardId = "123",
                PriceAmount = total,
                PriceCurrency = "EUR"
            }
        };

        LogResponse(response);
        return response;
    }

    private static double GetFractionPrice(string fws, string? city)
    {
        var prices = new Dictionary<string, Dictionary<string, double>>
        {
            {
                "Pineville", new Dictionary<string, double>
                {
                    { FractionTypes.ConstructionWaste, 0.18d },
                    { FractionTypes.GreenWaste, 0.12d }
                }
            },
            {
                "Moon Village", new Dictionary<string, double>
                {
                    { FractionTypes.ConstructionWaste, 0.15d },
                    { FractionTypes.GreenWaste, 0.09d }
                }
            }
        };
        
        return prices[city][fws];
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