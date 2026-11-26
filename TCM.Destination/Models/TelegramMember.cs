namespace TCM.Destination.Models;

public record TelegramMember(
    long Id,
    string FirstName,
    string? LastName,
    string? Username
);