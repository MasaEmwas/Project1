namespace BookCatalog.Models; // was Auth

public class AppUser
{
    public required string Username { get; init; }
    public required string PasswordHash { get; init; }

    public required string Role { get; init; }


}