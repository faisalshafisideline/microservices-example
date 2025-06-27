namespace Shared.Contracts.UserContext;

/// <summary>
/// Provides access to the current user context
/// </summary>
public interface IUserContextAccessor
{
    /// <summary>
    /// Gets the current user context. Returns null if no context is set.
    /// </summary>
    UserContext? Current { get; }

    /// <summary>
    /// Sets the current user context for the current execution flow
    /// </summary>
    void SetContext(UserContext context);

    /// <summary>
    /// Clears the current user context
    /// </summary>
    void ClearContext();

    /// <summary>
    /// Gets the current context or returns Empty context if none is set
    /// </summary>
    UserContext GetCurrentOrEmpty();

    /// <summary>
    /// Gets the current context or throws if none is set
    /// </summary>
    UserContext GetRequiredContext();
} 