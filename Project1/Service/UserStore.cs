using Project1.Auth;

namespace Project1.Services;
public static class UserStore
{
    
    public static readonly List<AppUser> Users = new()
    {
        new AppUser { Username = "admin@example.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin#123"), Role = "Admin" },
        new AppUser { Username = "user@example.com",  PasswordHash = BCrypt.Net.BCrypt.HashPassword("User#123"),  Role = "User"  }
    };

    public static AppUser? Find(string username) =>
        Users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

    public static bool CheckPassword(AppUser user, string password) =>
        BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
}
