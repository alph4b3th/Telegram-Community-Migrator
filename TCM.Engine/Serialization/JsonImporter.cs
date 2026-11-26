using System.Text.Json;

namespace TCM.Engine.Serialization;

public sealed class JsonImporter
{
    public async Task<List<T>> ImportAsync<T>(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException(
                "Arquivo JSON não encontrado.",
                filePath
            );

        await using FileStream stream = File.OpenRead(filePath);

        return await JsonSerializer.DeserializeAsync<List<T>>(stream)
            ?? new List<T>();
    }
}