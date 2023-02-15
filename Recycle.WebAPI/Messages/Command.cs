using System.Text.Json.Serialization;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Recycle.WebAPI.Messages;

public class Command
{
    [JsonPropertyName("type")] 
    public virtual string Type { get; }

    [JsonPropertyName("command_id")]
    public string CommandId { get; } = Guid.NewGuid().ToString();

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; } = DateTime.Now;
};

public class Command<TPayload> : Command
{
    [JsonPropertyName("payload")] 
    public TPayload Payload { get;  }

    public override string Type => Payload.GetType().Name;

    public Command(TPayload payload)
    {
        Payload = payload;
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this);
    }
}