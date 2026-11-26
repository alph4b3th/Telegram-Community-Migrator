
using Microsoft.Extensions.Configuration;
using WTelegram;
using TL;
using TCM.Destination.Models;

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
        _sourceUsers.Clear();

        // void AddUser(User user)
        // {
        //     members[user.id] = new TelegramMember(
        //         user.id,
        //         user.first_name ?? "",
        //         user.last_name,
        //         user.username
        //     );
        // }


        void AddUser(User user)
        {
            _sourceUsers[user.id] = user;

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
            for (int offset = 0; ;)
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


    public async Task<string> CreateInviteLinkAsync(long destinationGroupId)
    {
        var chats = await _client.Messages_GetAllChats();

        if (!chats.chats.TryGetValue(destinationGroupId, out var chat))
            throw new InvalidOperationException(
                "Grupo de destino não encontrado."
            );

        if (!chat.IsActive)
            throw new InvalidOperationException(
                "O grupo de destino está inativo."
            );

        var invite = (ChatInviteExported)
            await _client.Messages_ExportChatInvite(
                chat,
                title: "TCM - Convite"
            );

        return invite.link;
    }

    public async Task<List<string>> AddMembersAsync(
    long destinationGroupId,
    IEnumerable<TelegramMember> members)
    {
        var chats = await _client.Messages_GetAllChats();

        if (!chats.chats.TryGetValue(destinationGroupId, out var destination))
            throw new InvalidOperationException(
                "Grupo de destino não encontrado."
            );

        if (!destination.IsActive)
            throw new InvalidOperationException(
                "Grupo de destino inativo."
            );

        var results = new List<string>();

        foreach (var member in members)
        {
            if (_controller.State == OperationState.Stopped)
            {
                Console.WriteLine("O processamento foi encerrado.");
                break;
            }

            await _controller.WaitForResumeAsync();

            if (!_sourceUsers.TryGetValue(member.Id, out var user))
            {
                results.Add(
                    $"NÃO ENCONTRADO | {member.Id} | {member.FirstName}"
                );

                Console.WriteLine(
                   $"NÃO ENCONTRADO | {member.Id} | {member.FirstName}"
               );


                continue;
            }

            try
            {
                int delay = Random.Shared.Next(3 * 1000, 5 * 1000);
                await Task.Delay(delay);

                var response = await _client.AddChatUser(
                    destination,
                    user
                );

                if (response.missing_invitees is { Length: > 0 })
                {
                    results.Add(
                        $"RECUSADO | {member.Id} | {member.FirstName}"
                    );


                    Console.WriteLine(
                 $"RECUSADO | {member.Id} | {member.FirstName}"
             );
                }
                else
                {
                    results.Add(
                        $"ADICIONADO | {member.Id} | {member.FirstName}"
                    );


                    Console.WriteLine(
                       $"ADICIONADO | {member.Id} | {member.FirstName}"
                   );

                    _controller.Resume();
                }
            }
            catch (RpcException ex) when (
                ex.Message.Contains("PEER_FLOOD", StringComparison.OrdinalIgnoreCase)
                )
            {
                results.Add(
                    $"PEER_FLOOD | {member.Id} | {member.FirstName} | {ex.Message}"
                );

                Console.WriteLine("PEER_FLOOD: Telegram restringiu as operações.");
                _controller.Suspend();
                Console.WriteLine("Processamento suspenso.");

            }

            catch (RpcException ex) when (
             ex.Message.Contains("FLOOD_WAIT_X", StringComparison.OrdinalIgnoreCase)
             )
            {
                results.Add(
                    $"FLOOD_WAIT_X | {member.Id} | {member.FirstName} | {ex.Message}"
                );

                Console.WriteLine("FLOOD_WAIT_X: Telegram restringiu as operações.");
                _controller.Suspend();
                Console.WriteLine("Processamento suspenso.");

            }
            catch (RpcException ex)
            {
                results.Add(
                    $"RPC_ERROR | {member.Id} | {member.FirstName} | {ex.Message}"
                );

                Console.WriteLine($"RPC_ERROR | {ex.Message}");
            }
            catch (Exception ex)
            {
                results.Add(
                    $"FALHA | {member.Id} | {member.FirstName} | {ex.Message}"
                );

                Console.WriteLine($"FALHA | {ex.Message}");
            }
        }

        return results;
    }
}
