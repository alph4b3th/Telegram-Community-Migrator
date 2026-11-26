
using Microsoft.Extensions.Configuration;
using WTelegram;

namespace TCM.Engine.Telegram;

public sealed class TelegramClientService : IAsyncDisposable
{
    private readonly Client _client;

    public TelegramClientService()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddUserSecrets<TelegramClientService>()
            .Build();

        string apiId = configuration["Telegram:ApiId"]
            ?? throw new InvalidOperationException("ApiId não configurado.");

        string apiHash = configuration["Telegram:ApiHash"]
            ?? throw new InvalidOperationException("ApiHash não configurado.");

        string phoneNumber = configuration["Telegram:PhoneNumber"]
            ?? throw new InvalidOperationException("PhoneNumber não configurado.");

        _client = new Client(key => key switch
        {
            "api_id" => apiId,
            "api_hash" => apiHash,
            "phone_number" => phoneNumber,
            _ => null
        });
    }

    public async Task ConnectAsync()
    {
        var user = await _client.LoginUserIfNeeded();

        Console.WriteLine($"Autenticado: {user.first_name}");
    }

    public ValueTask DisposeAsync()
    {
        return _client.DisposeAsync();
    }
}
