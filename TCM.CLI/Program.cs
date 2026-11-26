using TCM.Engine.Telegram;

await using var telegram = new TelegramClientService();

Console.WriteLine("TCM - Telegram Client Mananger");
Console.WriteLine("Iniciando conexão...");

await telegram.ConnectAsync();