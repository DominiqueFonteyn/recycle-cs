using Microsoft.Extensions.Logging;
using NSubstitute;
using Recycle.WebAPI.Controllers;
using Recycle.WebAPI.Messages;

namespace Recycle.Tests;

public class ScenarioTests
{
    private HandlingController _controller;

    [SetUp]
    public void SetUp()
    {
        _controller = new HandlingController(Substitute.For<ILogger<HandlingController>>());
    }

    [Test]
    public void NoFractionsDelivered()
    {
        var request = new RecycleRequest
        {
            Command = CalculatePrice(),
            History = new List<Event>
            {
                IdCardRegistered("Moon Village")
            }
        };

        var evt = _controller.Handle(request) as Event<PriceWasCalculated>;

        Assert.AreEqual(0, evt.Payload.PriceAmount);
    }

    [Test]
    public void SingleFractionWasDelivered()
    {
        var request = new RecycleRequest
        {
            Command = CalculatePrice(),
            History = new List<Event>
            {
                IdCardRegistered("Moon Village"),
                WeightWasMeasured(487),
                new Event<FractionWasSelected>
                {
                    Payload = new FractionWasSelected
                    {
                        FractionType = "Construction waste"
                    }
                },
                WeightWasMeasured(422)
            }
        };

        var evt = _controller.Handle(request) as Event<PriceWasCalculated>;

        Assert.AreEqual(9.75, evt.Payload.PriceAmount);
    }

    [Test]
    public void MultipleFractionsDelivered()
    {
        var request = new RecycleRequest
        {
            Command = CalculatePrice(),
            History = new List<Event>
            {
                IdCardRegistered("Moon Village"),
                WeightWasMeasured(487),
                FractionWasSelected(FractionTypes.ConstructionWaste),
                WeightWasMeasured(422),
                FractionWasSelected(FractionTypes.GreenWaste),
                WeightWasMeasured(375)
            }
        };

        var evt = _controller.Handle(request) as Event<PriceWasCalculated>;

        Assert.AreEqual(13.98, evt.Payload.PriceAmount);
    }
    
    [Test]
    public void HarryDelivers_200Kg_ConstructionWaste()
    {
        var request = new RecycleRequest
        {
            Command = CalculatePrice(),
            History = new List<Event>
            {
                IdCardRegistered("Pineville"),
                WeightWasMeasured(1280),
                FractionWasSelected(FractionTypes.ConstructionWaste),
                WeightWasMeasured(1080)
            }
        };

        var evt = _controller.Handle(request) as Event<PriceWasCalculated>;

        Assert.AreEqual(18, evt.Payload.PriceAmount);
    }

    private static Command<CalculatePrice> CalculatePrice()
    {
        return new Command<CalculatePrice>
        {
            Payload = new CalculatePrice
            {
                CardId = "123"
            }
        };
    }

    private static Event<WeightWasMeasured> WeightWasMeasured(int weight)
    {
        return new Event<WeightWasMeasured>
        {
            Payload = new WeightWasMeasured
            {
                Weight = weight
            }
        };
    }

    private static Event<FractionWasSelected> FractionWasSelected(string fractionType)
    {
        return new Event<FractionWasSelected>
        {
            Payload = new FractionWasSelected
            {
                FractionType = fractionType
            }
        };
    }
    private static Event<IdCardRegistered> IdCardRegistered(string city)
    {
        return new Event<IdCardRegistered>
        {
            Payload = new IdCardRegistered
            {
                City = city
            }
        };
    }
}
