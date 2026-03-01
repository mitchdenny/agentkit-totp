using System.Text.Json.Serialization;

namespace AgentKit.Totp.Storage;

[JsonSerializable(typeof(StoreSchema))]
[JsonSerializable(typeof(Dictionary<string, TotpEntry>))]
[JsonSerializable(typeof(TotpEntry))]
internal partial class StoreJsonContext : JsonSerializerContext
{
}

internal class StoreSchema
{
    public int Version { get; set; }
    public Dictionary<string, TotpEntry> Entries { get; set; } = new();
}
