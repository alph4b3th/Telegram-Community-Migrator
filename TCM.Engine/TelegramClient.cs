
using Microsoft.Extensions.Configuration;
using WTelegram;
using TL;
using TCM.Destination.Models;

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


public async Task<List<TelegramGroup>> GetGroupsAsync()
{
    var result = await _client.Messages_GetAllChats();

    var groups = new List<TelegramGroup>();

    foreach (var (id, chat) in result.chats)
    {
        if (!chat.IsActive)
            continue;

        switch (chat)
        {
            case Chat smallGroup:
                groups.Add(new TelegramGroup(
                    id,
                    smallGroup.title,
                    smallGroup.participants_count
                ));
                break;

            case Channel channel when !channel.IsChannel:
                groups.Add(new TelegramGroup(
                    id,
                    channel.title,
                    channel.participants_count
                ));
                break;
        }
    }

    return groups;
}

public async Task<List<TelegramMember>> GetMembersAsync(long groupId)
{
    var chats = await _client.Messages_GetAllChats();

    if (!chats.chats.TryGetValue(groupId, out var chat))
        throw new InvalidOperationException("Grupo não encontrado.");

    var members = new Dictionary<long, TelegramMember>();

    void AddUser(User user)
    {
        members[user.id] = new TelegramMember(
            user.id,
            user.first_name ?? "",
            user.last_name,
            user.username
        );
    }

    // Grupo básico
    if (chat is Chat)
    {
        var fullChat = await _client.Messages_GetFullChat(groupId);

        foreach (var user in fullChat.users.Values)
            AddUser(user);
    }
    // Supergrupo
    else if (chat is Channel channel)
    {
        for (int offset = 0; ; )
        {
            var result = await _client.Channels_GetParticipants(
                channel,
                null,
                offset
            );

            foreach (var user in result.users.Values)
                AddUser(user);

            offset += result.participants.Length;

            if (offset >= result.count ||
                result.participants.Length == 0)
                break;
        }
    }

    return members.Values.ToList();
}

}
