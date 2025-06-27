using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace Shared.Contracts.Security;

/// <summary>
/// Security service implementation for encryption and data protection
/// </summary>
public class SecurityService : ISecurityService
{
    private readonly ILogger<SecurityService> _logger;
    private readonly byte[] _encryptionKey;

    public SecurityService(ILogger<SecurityService> logger, string encryptionKey = "MySecretKey12345") // In production, use proper key management
    {
        _logger = logger;
        _encryptionKey = Encoding.UTF8.GetBytes(encryptionKey.PadRight(32).Substring(0, 32)); // Ensure 32 bytes for AES-256
    }

    public async Task<string> EncryptSensitiveDataAsync(string data, string? keyId = null)
    {
        try
        {
            using var aes = Aes.Create();
            aes.Key = _encryptionKey;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            var dataBytes = Encoding.UTF8.GetBytes(data);
            var encryptedBytes = encryptor.TransformFinalBlock(dataBytes, 0, dataBytes.Length);

            // Combine IV and encrypted data
            var result = new byte[aes.IV.Length + encryptedBytes.Length];
            Array.Copy(aes.IV, 0, result, 0, aes.IV.Length);
            Array.Copy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);

            var base64Result = Convert.ToBase64String(result);
            _logger.LogDebug("Encrypted sensitive data (KeyId: {KeyId})", keyId ?? "default");
            
            return await Task.FromResult(base64Result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt sensitive data");
            throw;
        }
    }

    public async Task<string> DecryptSensitiveDataAsync(string encryptedData, string? keyId = null)
    {
        try
        {
            var encryptedBytes = Convert.FromBase64String(encryptedData);

            using var aes = Aes.Create();
            aes.Key = _encryptionKey;

            // Extract IV
            var iv = new byte[16]; // AES block size
            Array.Copy(encryptedBytes, 0, iv, 0, iv.Length);
            aes.IV = iv;

            // Extract encrypted data
            var cipherBytes = new byte[encryptedBytes.Length - iv.Length];
            Array.Copy(encryptedBytes, iv.Length, cipherBytes, 0, cipherBytes.Length);

            using var decryptor = aes.CreateDecryptor();
            var decryptedBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

            var result = Encoding.UTF8.GetString(decryptedBytes);
            _logger.LogDebug("Decrypted sensitive data (KeyId: {KeyId})", keyId ?? "default");
            
            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt sensitive data");
            throw;
        }
    }

    public async Task<string> HashPasswordAsync(string password)
    {
        try
        {
            var salt = RandomNumberGenerator.GetBytes(32);
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
            var hash = pbkdf2.GetBytes(32);

            var hashBytes = new byte[64]; // 32 for salt + 32 for hash
            Array.Copy(salt, 0, hashBytes, 0, 32);
            Array.Copy(hash, 0, hashBytes, 32, 32);

            var result = Convert.ToBase64String(hashBytes);
            _logger.LogDebug("Hashed password");
            
            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to hash password");
            throw;
        }
    }

    public async Task<bool> VerifyPasswordAsync(string password, string hash)
    {
        try
        {
            var hashBytes = Convert.FromBase64String(hash);
            var salt = new byte[32];
            Array.Copy(hashBytes, 0, salt, 0, 32);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
            var testHash = pbkdf2.GetBytes(32);

            var storedHash = new byte[32];
            Array.Copy(hashBytes, 32, storedHash, 0, 32);

            var result = CryptographicOperations.FixedTimeEquals(testHash, storedHash);
            _logger.LogDebug("Password verification result: {Result}", result);
            
            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify password");
            return false;
        }
    }

    public async Task<string> GenerateSecureTokenAsync(int length = 32)
    {
        try
        {
            var bytes = RandomNumberGenerator.GetBytes(length);
            var result = Convert.ToBase64String(bytes);
            _logger.LogDebug("Generated secure token of length {Length}", length);
            
            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate secure token");
            throw;
        }
    }

    public async Task<bool> ValidateTokenAsync(string token, string expectedHash)
    {
        try
        {
            var tokenHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
            var result = CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(tokenHash),
                Encoding.UTF8.GetBytes(expectedHash));
            
            _logger.LogDebug("Token validation result: {Result}", result);
            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate token");
            return false;
        }
    }
} 