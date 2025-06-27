using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

namespace ApiGateway.Authentication;

public sealed class HardcodedAuthenticationHandler : AuthenticationHandler<HardcodedAuthenticationSchemeOptions>
{
    private readonly ILogger<HardcodedAuthenticationHandler> _logger;
    private readonly IUserStore _userStore;

    public HardcodedAuthenticationHandler(
        IOptionsMonitor<HardcodedAuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IUserStore userStore) : base(options, logger, encoder)
    {
        _logger = logger.CreateLogger<HardcodedAuthenticationHandler>();
        _userStore = userStore ?? throw new ArgumentNullException(nameof(userStore));
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check if Authorization header exists
        if (!Request.Headers.ContainsKey("Authorization"))
        {
            _logger.LogDebug("No Authorization header found");
            return AuthenticateResult.NoResult();
        }

        var authHeader = Request.Headers.Authorization.ToString();
        
        // Check for Basic authentication
        if (!authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("Authorization header is not Basic authentication");
            return AuthenticateResult.Fail("Invalid authorization header");
        }

        try
        {
            // Decode Basic auth credentials
            var token = authHeader["Basic ".Length..].Trim();
            var credentialBytes = Convert.FromBase64String(token);
            var credentials = Encoding.UTF8.GetString(credentialBytes);
            var parts = credentials.Split(':', 2);

            if (parts.Length != 2)
            {
                _logger.LogWarning("Invalid Basic auth format");
                return AuthenticateResult.Fail("Invalid credentials format");
            }

            var username = parts[0];
            var password = parts[1];

            // Validate credentials against hardcoded store
            var user = await _userStore.ValidateCredentialsAsync(username, password);
            if (user == null)
            {
                _logger.LogWarning("Authentication failed for user: {Username}", username);
                return AuthenticateResult.Fail("Invalid username or password");
            }

            // Create claims for the authenticated user
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Email, user.Email),
                new("FullName", user.FullName)
            };

            // Add role claims
            foreach (var role in user.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            _logger.LogInformation("User {Username} authenticated successfully with roles: [{Roles}]", 
                user.Username, string.Join(", ", user.Roles));

            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during authentication");
            return AuthenticateResult.Fail("Authentication error");
        }
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers.Append("WWW-Authenticate", "Basic realm=\"API Gateway\"");
        Response.StatusCode = 401;
        return Task.CompletedTask;
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 403;
        return Task.CompletedTask;
    }
}

public sealed class HardcodedAuthenticationSchemeOptions : AuthenticationSchemeOptions
{
    public const string SchemeName = "HardcodedBasic";
} 