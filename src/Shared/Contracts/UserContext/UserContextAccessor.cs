namespace Shared.Contracts.UserContext;

/// <summary>
/// Thread-safe implementation of IUserContextAccessor using AsyncLocal
/// </summary>
public class UserContextAccessor : IUserContextAccessor
{
    private static readonly AsyncLocal<UserContext?> _userContext = new();

    public UserContext? Current => _userContext.Value;

    public void SetContext(UserContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _userContext.Value = context;
    }

    public void ClearContext()
    {
        _userContext.Value = null;
    }

    public UserContext GetCurrentOrEmpty()
    {
        return Current ?? UserContext.Empty;
    }

    public UserContext GetRequiredContext()
    {
        return Current ?? throw new InvalidOperationException(
            "No user context is available. Ensure that context is properly set via middleware or interceptors.");
    }
} 