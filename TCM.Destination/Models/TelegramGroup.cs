namespace TCM.Destination.Models;

public record TelegramGroup(
    long Id,
    string Title,
    int MembersCount
);