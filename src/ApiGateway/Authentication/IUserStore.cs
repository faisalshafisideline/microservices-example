namespace ApiGateway.Authentication;

public interface IUserStore
{
    Task<User?> ValidateCredentialsAsync(string username, string password);
    Task<User?> GetUserByIdAsync(string userId);
    Task<User?> GetUserByUsernameAsync(string username);
}

public sealed class HardcodedUserStore : IUserStore
{
    private readonly List<User> _users;
    private readonly ILogger<HardcodedUserStore> _logger;

    public HardcodedUserStore(ILogger<HardcodedUserStore> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _users = InitializeUsers();
    }

    public Task<User?> ValidateCredentialsAsync(string username, string password)
    {
        var user = _users.FirstOrDefault(u => 
            u.Username.Equals(username, StringComparison.OrdinalIgnoreCase) && 
            u.Password == password); // In production, use hashed passwords!

        if (user != null)
        {
            _logger.LogDebug("Credentials validated for user: {Username}", username);
        }
        else
        {
            _logger.LogWarning("Invalid credentials for user: {Username}", username);
        }

        return Task.FromResult(user);
    }

    public Task<User?> GetUserByIdAsync(string userId)
    {
        var user = _users.FirstOrDefault(u => u.Id == userId);
        return Task.FromResult(user);
    }

    public Task<User?> GetUserByUsernameAsync(string username)
    {
        var user = _users.FirstOrDefault(u => 
            u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(user);
    }

    private static List<User> InitializeUsers()
    {
        return new List<User>
        {
            new()
            {
                Id = "admin-001",
                Username = "admin",
                Password = "supersecret", // WARNING: Never use plaintext passwords in production!
                Email = "admin@example.com",
                FullName = "System Administrator",
                Roles = ["Admin", "Reporter", "User"],
                IsActive = true
            },
            new()
            {
                Id = "reporter-001", 
                Username = "reporter",
                Password = "report123",
                Email = "reporter@example.com",
                FullName = "Content Reporter",
                Roles = ["Reporter", "User"],
                IsActive = true
            },
            new()
            {
                Id = "user-001",
                Username = "user",
                Password = "user123",
                Email = "user@example.com", 
                FullName = "Regular User",
                Roles = ["User"],
                IsActive = true
            },
            new()
            {
                Id = "author-001",
                Username = "author",
                Password = "write123",
                Email = "author@example.com",
                FullName = "Content Author", 
                Roles = ["Author", "User"],
                IsActive = true
            }
        };
    }
}

public sealed record User
{
    public required string Id { get; init; }
    public required string Username { get; init; }
    public required string Password { get; init; } // Use hashed passwords in production!
    public required string Email { get; init; }
    public required string FullName { get; init; }
    public required string[] Roles { get; init; }
    public bool IsActive { get; init; } = true;
} 