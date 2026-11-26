
using TCM.Engine.Telegram;
using TCM.Engine.Serialization;

await using var telegram = new TelegramClientService();

Console.WriteLine("TCM - Telegram Client Manager");
Console.WriteLine("============================");

await telegram.ConnectAsync();

Console.WriteLine("\nBuscando grupos...\n");

var groups = await telegram.GetGroupsAsync();

if (groups.Count == 0)
{
    Console.WriteLine("Nenhum grupo encontrado.");
    return;
}

foreach (var group in groups)
{
    Console.WriteLine(
        $"[{group.Id}] {group.Title} ({group.MembersCount} membros)"
    );
}

Console.Write("\nDigite o ID do grupo: ");

if (!long.TryParse(Console.ReadLine(), out var groupId))
{
    Console.WriteLine("ID inválido.");
    return;
}

Console.WriteLine("\nBuscando membros...\n");

var members = await telegram.GetMembersAsync(groupId);

foreach (var member in members)
{
    Console.WriteLine(
        $"{member.Id} | {member.FirstName} {member.LastName} | @{member.Username}"
    );
}

Console.WriteLine($"\nTotal encontrado: {members.Count}");

var exporter = new JsonExporter();

string filePath = Path.Combine(
    AppContext.BaseDirectory,
    "exports",
    $"members_{groupId}.json"
);

await exporter.ExportAsync(members, filePath);

Console.WriteLine($"\nArquivo JSON exportado: {filePath}");
