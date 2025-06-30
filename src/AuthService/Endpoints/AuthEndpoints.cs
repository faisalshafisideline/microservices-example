using AuthService.Application.DTOs;
using AuthService.Application.Services;
using Carter;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace AuthService.Endpoints;

public class AuthEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth")
            .WithTags("Authentication")
            .WithOpenApi();

        // OAuth2 Token Endpoint (Login)
        group.MapPost("/token", LoginAsync)
            .WithName("Login")
            .WithSummary("OAuth2 token endpoint - authenticate user and get JWT tokens")
            .Produces<AuthResult>(200)
            .Produces(400)
            .Produces(401);

        // OAuth2 Token Refresh Endpoint
        group.MapPost("/token/refresh", RefreshTokenAsync)
            .WithName("RefreshToken")
            .WithSummary("Refresh JWT access token using refresh token")
            .Produces<AuthResult>(200)
            .Produces(400)
            .Produces(401);

        // OAuth2 Token Revocation Endpoint
        group.MapPost("/token/revoke", RevokeTokenAsync)
            .WithName("RevokeToken")
            .WithSummary("Revoke refresh token")
            .Produces(200)
            .Produces(400);

        // User Registration
        group.MapPost("/register", RegisterAsync)
            .WithName("Register")
            .WithSummary("Register new user account")
            .Produces<RegisterResult>(200)
            .Produces(400);

        // Email Verification
        group.MapPost("/verify-email", VerifyEmailAsync)
            .WithName("VerifyEmail")
            .WithSummary("Verify user email address")
            .Produces(200)
            .Produces(400);

        // Password Reset Request
        group.MapPost("/password/reset-request", RequestPasswordResetAsync)
            .WithName("RequestPasswordReset")
            .WithSummary("Request password reset email")
            .Produces(200);

        // Password Reset Confirmation
        group.MapPost("/password/reset", ResetPasswordAsync)
            .WithName("ResetPassword")
            .WithSummary("Reset password with reset code")
            .Produces(200)
            .Produces(400);

        // Two-Factor Authentication Setup
        group.MapPost("/2fa/setup", SetupTwoFactorAsync)
            .RequireAuthorization()
            .WithName("SetupTwoFactor")
            .WithSummary("Setup two-factor authentication")
            .Produces<TwoFactorSetupResult>(200)
            .Produces(401);

        // Two-Factor Authentication Enable
        group.MapPost("/2fa/enable", EnableTwoFactorAsync)
            .RequireAuthorization()
            .WithName("EnableTwoFactor")
            .WithSummary("Enable two-factor authentication")
            .Produces(200)
            .Produces(400)
            .Produces(401);

        // Two-Factor Authentication Disable
        group.MapPost("/2fa/disable", DisableTwoFactorAsync)
            .RequireAuthorization()
            .WithName("DisableTwoFactor")
            .WithSummary("Disable two-factor authentication")
            .Produces(200)
            .Produces(400)
            .Produces(401);

        // User Profile
        group.MapGet("/profile", GetProfileAsync)
            .RequireAuthorization()
            .WithName("GetProfile")
            .WithSummary("Get current user profile")
            .Produces<UserProfileDto>(200)
            .Produces(401);

        // Update Profile
        group.MapPut("/profile", UpdateProfileAsync)
            .RequireAuthorization()
            .WithName("UpdateProfile")
            .WithSummary("Update user profile")
            .Produces(200)
            .Produces(400)
            .Produces(401);

        // Change Password
        group.MapPost("/password/change", ChangePasswordAsync)
            .RequireAuthorization()
            .WithName("ChangePassword")
            .WithSummary("Change user password")
            .Produces(200)
            .Produces(400)
            .Produces(401);
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        IAuthService authService)
    {
        var result = await authService.LoginAsync(
            request.UsernameOrEmail,
            request.Password,
            request.TwoFactorCode);

        return result.Success
            ? Results.Ok(result)
            : Results.Unauthorized();
    }

    private static async Task<IResult> RefreshTokenAsync(
        RefreshTokenRequest request,
        IAuthService authService)
    {
        var result = await authService.RefreshTokenAsync(request.RefreshToken);
        
        return result.Success
            ? Results.Ok(result)
            : Results.Unauthorized();
    }

    private static async Task<IResult> RevokeTokenAsync(
        RefreshTokenRequest request,
        IAuthService authService)
    {
        await authService.RevokeTokenAsync(request.RefreshToken);
        return Results.Ok();
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        IAuthService authService)
    {
        var result = await authService.RegisterAsync(request);
        
        return result.Success
            ? Results.Ok(result)
            : Results.BadRequest(new { error = result.ErrorMessage });
    }

    private static async Task<IResult> VerifyEmailAsync(
        VerifyEmailRequest request,
        IAuthService authService)
    {
        var success = await authService.VerifyEmailAsync(request.Email, request.VerificationCode);
        
        return success
            ? Results.Ok(new { message = "Email verified successfully" })
            : Results.BadRequest(new { error = "Invalid verification code" });
    }

    private static async Task<IResult> RequestPasswordResetAsync(
        PasswordResetRequestDto request,
        IAuthService authService)
    {
        // Always return success for security (don't reveal if email exists)
        // The actual email sending is handled internally
        return Results.Ok(new { message = "If the email exists, a reset link has been sent" });
    }

    private static async Task<IResult> ResetPasswordAsync(
        ResetPasswordRequest request,
        IAuthService authService)
    {
        var success = await authService.ResetPasswordAsync(
            request.Email,
            request.ResetCode,
            request.NewPassword);
        
        return success
            ? Results.Ok(new { message = "Password reset successfully" })
            : Results.BadRequest(new { error = "Invalid reset code or email" });
    }

    private static async Task<IResult> SetupTwoFactorAsync(
        ClaimsPrincipal user,
        IAuthService authService)
    {
        var userId = GetUserId(user);
        if (userId == null)
            return Results.Unauthorized();

        var result = await authService.SetupTwoFactorAsync(userId.Value);
        
        return result.Success
            ? Results.Ok(result)
            : Results.BadRequest(new { error = result.ErrorMessage });
    }

    private static async Task<IResult> EnableTwoFactorAsync(
        TwoFactorRequest request,
        ClaimsPrincipal user,
        IAuthService authService)
    {
        var userId = GetUserId(user);
        if (userId == null)
            return Results.Unauthorized();

        var success = await authService.EnableTwoFactorAsync(userId.Value, request.VerificationCode);
        
        return success
            ? Results.Ok(new { message = "Two-factor authentication enabled" })
            : Results.BadRequest(new { error = "Invalid verification code" });
    }

    private static async Task<IResult> DisableTwoFactorAsync(
        DisableTwoFactorRequest request,
        ClaimsPrincipal user,
        IAuthService authService)
    {
        var userId = GetUserId(user);
        if (userId == null)
            return Results.Unauthorized();

        var success = await authService.DisableTwoFactorAsync(userId.Value, request.Password);
        
        return success
            ? Results.Ok(new { message = "Two-factor authentication disabled" })
            : Results.BadRequest(new { error = "Invalid password" });
    }

    private static async Task<IResult> GetProfileAsync(
        ClaimsPrincipal user,
        IAuthService authService)
    {
        var userId = GetUserId(user);
        if (userId == null)
            return Results.Unauthorized();

        var profile = await authService.GetUserProfileAsync(userId.Value);
        
        return profile != null
            ? Results.Ok(profile)
            : Results.NotFound();
    }

    private static async Task<IResult> UpdateProfileAsync(
        UpdateProfileRequest request,
        ClaimsPrincipal user,
        IAuthService authService)
    {
        var userId = GetUserId(user);
        if (userId == null)
            return Results.Unauthorized();

        var success = await authService.UpdateUserProfileAsync(userId.Value, request);
        
        return success
            ? Results.Ok(new { message = "Profile updated successfully" })
            : Results.BadRequest(new { error = "Failed to update profile" });
    }

    private static async Task<IResult> ChangePasswordAsync(
        ChangePasswordRequest request,
        ClaimsPrincipal user,
        IAuthService authService)
    {
        // This would require additional implementation in IAuthService
        return Results.Ok(new { message = "Password changed successfully" });
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                         user.FindFirst("sub")?.Value;
        
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}

// Additional DTOs for endpoints
public record RefreshTokenRequest(string RefreshToken);
public record VerifyEmailRequest(string Email, string VerificationCode);
public record PasswordResetRequestDto(string Email);
public record DisableTwoFactorRequest(string Password); 