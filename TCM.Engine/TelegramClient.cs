
using Microsoft.Extensions.Configuration;
using WTelegram;
using TL;
using TCM.Destination.Models;
using System.Formats.Asn1;

namespace TCM.Engine.Telegram;

public sealed class TelegramClientService : IAsyncDisposable
{
    private readonly Client _client;
    private readonly Dictionary<long, User> _sourceUsers = new();

    private readonly OperationController _controller = new OperationController();
    public TelegramClientService()
    {

        IConfiguration configuration = new ConfigurationBuilder()
            .AddUserSecrets<TelegramClientService>(optional: true)
            .AddEnvironmentVariables()
            .Build();



        string apiId = GetRequiredConfiguration(configuration, "Telegram:ApiId");
        string apiHash = GetRequiredConfiguration(configuration, "Telegram:ApiHash");
        string phoneNumber = GetRequiredConfiguration(configuration, "Telegram:PhoneNumber");

        _client = new Client(key => key switch
        {
            "api_id" => apiId,
            "api_hash" => apiHash,
            "phone_number" => phoneNumber,
            _ => null
        });

    }

    private static string GetRequiredConfiguration(
        IConfiguration configuration,
        string key)
    {
        return configuration[key] ?? throw new InvalidOperationException($"Configuração não encontrada: {key}");
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
        var chats = await _client.Messages_GetAllChats();

        return chats.chats
            .Where(pair => pair.Value.IsActive)
            .Select(pair => pair.Value switch
            {
                Chat chat => new TelegramGroup(
                    pair.Key,
                    chat.title,
                    chat.participants_count),

                Channel channel when !channel.IsChannel =>
                new TelegramGroup(
                    pair.Key,
                    channel.title,
                    channel.participants_count),

                _ => null

            })
            .OfType<TelegramGroup>()
            .ToList();
    }

    public async Task<List<TelegramMember>> GetMembersAsync(long groupId)
    {
        var chats = await _client.Messages_GetAllChats();

        if (!chats.chats.TryGetValue(groupId, out var chat))
            throw new InvalidOperationException("Grupo não encontrado.");

        _sourceUsers.Clear();


        if (chat is Chat)
        {
            var fullChat = await _client.Messages_GetFullChat(groupId);

            foreach (var user in fullChat.users.Values)
                AddsourceUser(user);
        }
        else if (chat is Channel channel)
        {
            await LoadChannelMembersAsync(channel);
        }

        return _sourceUsers.Values
            .Select(ToTelegramMember)
             .ToList();
    }

    private void AddsourceUser(User user)
    {
        _sourceUsers[user.id] = user;
    }

    private async Task LoadChannelMembersAsync(Channel channel)
    {
        for (int offset = 0; ;)
        {
            var result = await _client.Channels_GetParticipants(
                channel,
                null,
                offset
            );

            foreach (var user in result.users.Values)
                AddsourceUser(user);

            offset += result.participants.Length;

            if (offset >= result.count || result.participants.Length == 0)
                break;


        }
    }


    private static TelegramMember ToTelegramMember(User user)
    {
        return new TelegramMember(
            user.id,
            user.first_name ?? "",
            user.last_name,
            user.username
        );
    }

    public async Task<List<string>> AddMembersAsync(
        long destinationGroupId,
        IEnumerable<TelegramMember> members)
    {
        var destination = await GetActiveGroupAsync(destinationGroupId);
        var results = new List<string>();

        foreach (var member in members)
        {
            if (_controller.State == OperationState.Stopped)
                break;



            if (!_sourceUsers.TryGetValue(member.Id, out var user))
            {
                RecordResult(
                    results,
                    "NÃO ENCONTRADO",
                    member);

                continue;
            }

            await _controller.WaitForResumeAsync(async () =>
                       {
                           try
                           {
                               await Task.Delay( TimeSpan.FromSeconds(Random.Shared.Next(30, 60)));

                               var response = await _client.AddChatUser(
                                   destination,
                                   user);

                               return response.missing_invitees is not { Length: > 0 };
                           }
                           catch (RpcException)
                           {
                               return false;
                           }
                           catch (Exception)
                           {
                               return false;
                           }
                       });

            try
            {
              await Task.Delay( TimeSpan.FromSeconds(Random.Shared.Next(30, 60)));

                var response = await _client.AddChatUser(
                    destination,
                    user);

                if (response.missing_invitees is { Length: > 0 })
                {
                    RecordResult(
                        results,
                        "RECUSADO",
                        member);

                }
                else
                {
                    RecordResult(
                        results,
                        "ADICIONADO",
                        member);

                    if (_controller.State == OperationState.Suspended)
                        _controller.Resume();
                }
            }

            catch (RpcException ex)
            {
                if (IsRateLimitError(ex))
                {
                    RecordResult(
                        results,
                        "RESTRIÇÃO TELEGRAM",
                        member,
                        ex.Message
                    );

                    _controller.Suspend();

                    Console.WriteLine(
                       "Operação suspensa devido a uma restrição do Telegram.");


                }
                RecordResult(
                   results,
                   "RPC_ERROR",
                   member,
                   ex.Message);
            }

            catch (Exception ex)
            {
                RecordResult(
                    results,
                    "FALHA",
                    member,
                    ex.Message);
            }
        }
        return results;
    }

    private async Task<InputPeer> GetActiveGroupAsync(long groupId)
    {
        var chats = await _client.Messages_GetAllChats();

        if (!chats.chats.TryGetValue(groupId, out var chat))

            throw new InvalidOperationException(
                "Grupo de destino não encontrado.");

        if (!chat.IsActive)
            throw new InvalidOperationException(
                "Grupo de destino inativo.");

        return chat;

    }

    private static bool IsRateLimitError(RpcException exception)
    {
        return exception.Message.Contains(
                   "PEER_FLOOD",
                   StringComparison.OrdinalIgnoreCase)
               || exception.Message.Contains(
                   "FLOOD_WAIT",
                   StringComparison.OrdinalIgnoreCase);
    }

    private static void RecordResult(
        List<string> results,
        string status,
        TelegramMember member,
        string? details = null)
    {
        string message = $"{status} | {member.Id} | {member.FirstName}";

        if (!string.IsNullOrWhiteSpace(details))
            message += $" | {details}";

        results.Add(message);

        Console.WriteLine(message);
    }
}
