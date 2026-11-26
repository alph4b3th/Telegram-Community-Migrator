using TCM.Engine.Telegram;
using TCM.Engine.Serialization;

await using var telegram = new TelegramClientService();

var exporter = new JsonExporter();
var importer = new JsonImporter();

Console.WriteLine("TCM - Telegram Client Manager");
Console.WriteLine("============================");

await telegram.ConnectAsync();

Console.WriteLine("\nGrupos disponíveis:\n");

var groups = await telegram.GetGroupsAsync();

if (groups.Count == 0)
{
    Console.WriteLine("Nenhum grupo encontrado.");
    return;
}

for (int i = 0; i < groups.Count; i++)
{
    Console.WriteLine(
        $"{i + 1}. {groups[i].Title} " +
        $"({groups[i].MembersCount} membros) | ID: {groups[i].Id}"
    );
}

Console.Write("\nSelecione o grupo de origem: ");

if (!int.TryParse(Console.ReadLine(), out int sourceIndex) ||
    sourceIndex < 1 ||
    sourceIndex > groups.Count)
{
    Console.WriteLine("Seleção inválida.");
    return;
}

var sourceGroup = groups[sourceIndex - 1];

Console.WriteLine($"\nExtraindo membros de {sourceGroup.Title}...");

var members = await telegram.GetMembersAsync(sourceGroup.Id);

string exportDirectory = Path.Combine(
    AppContext.BaseDirectory,
    "exports"
);

string filePath = Path.Combine(
    exportDirectory,
    $"members_{sourceGroup.Id}.json"
);

await exporter.ExportAsync(members, filePath);

Console.WriteLine($"JSON exportado: {filePath}");

var importedMembers =
    await importer.ImportAsync<TCM.Destination.Models.TelegramMember>(
        filePath
    );

Console.WriteLine(
    $"\nJSON carregado: {importedMembers.Count} participantes."
);

Console.WriteLine("\nSelecione o grupo de destino:\n");

var destinations = groups
    .Where(g => g.Id != sourceGroup.Id)
    .ToList();

for (int i = 0; i < destinations.Count; i++)
{
    Console.WriteLine(
        $"{i + 1}. {destinations[i].Title} " +
        $"({destinations[i].MembersCount} membros) | ID: {destinations[i].Id}"
    );

    
    
}

Console.Write("\nDestino: ");

if (!int.TryParse(Console.ReadLine(), out int destinationIndex) ||
    destinationIndex < 1 ||
    destinationIndex > destinations.Count)
{
    Console.WriteLine("Seleção inválida.");
    return;
}

var destination = destinations[destinationIndex - 1];

Console.WriteLine(
    $"\nAdicionando membros em: {destination.Title}"
);

Console.WriteLine(
    $"Total de membros: {importedMembers.Count}"
);

Console.Write("\nPressione ENTER para iniciar...");

Console.ReadLine();

var results = await telegram.AddMembersAsync(
    destination.Id,
    importedMembers
);

foreach (var result in results)
{
    Console.WriteLine(result);
}

int added = results.Count(
    r => r.StartsWith("ADICIONADO")
);

int failed = results.Count - added;

Console.WriteLine("\n========== RESULTADO ==========");
Console.WriteLine($"Adicionados: {added}");
Console.WriteLine($"Não adicionados: {failed}");
Console.WriteLine($"Total processado: {results.Count}");

Console.WriteLine(
    $"\nParticipantes carregados do JSON: {importedMembers.Count}"
);