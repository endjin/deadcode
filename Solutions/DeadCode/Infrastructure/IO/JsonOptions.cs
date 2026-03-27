using System.Text.Json;
using System.Text.Json.Serialization;

namespace DeadCode.Infrastructure.IO;

public static class JsonOptions
{
    public static readonly JsonSerializerOptions ReadWrite = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };
}
