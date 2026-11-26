
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TCM.Engine.Serialization;

public sealed class JsonExporter
{
    private readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task ExportAsync<T>(
        IEnumerable<T> data,
        string filePath)
    {
        string? directory = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        await using FileStream stream = File.Create(filePath);

        await JsonSerializer.SerializeAsync(
            stream,
            data,
            _options
        );
    }
}
