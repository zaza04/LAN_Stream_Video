using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ServerStreamApp.Models
{
    public class AuthMessage
    {
        // Existing fields - Authentication
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("userId")]
        public int UserId { get; set; }

        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;

        // New fields - Key Exchange for Encryption
        [JsonPropertyName("publicKey")]
        public string PublicKey { get; set; } = string.Empty;

        [JsonPropertyName("encryptedAESKey")]
        public string EncryptedAESKey { get; set; } = string.Empty;

        [JsonPropertyName("keyExchangeStep")]
        public string KeyExchangeStep { get; set; } = string.Empty;

        [JsonPropertyName("isEncrypted")]
        public bool IsEncrypted { get; set; } = false;

        // Helper method to create key exchange request
        public static AuthMessage CreateKeyExchangeRequest(string clientPublicKey)
        {
            return new AuthMessage
            {
                Type = "KEY_EXCHANGE_REQUEST",
                PublicKey = clientPublicKey,
                KeyExchangeStep = "CLIENT_PUBLIC_KEY",
                Message = "Client requesting secure key exchange"
            };
        }

        // Helper method to create key exchange response
        public static AuthMessage CreateKeyExchangeResponse(string encryptedAESKey)
        {
            return new AuthMessage
            {
                Type = "KEY_EXCHANGE_RESPONSE",
                EncryptedAESKey = encryptedAESKey,
                KeyExchangeStep = "SERVER_ENCRYPTED_AES_KEY",
                Message = "Server sending encrypted AES key"
            };
        }

        // Helper method to create encrypted auth request
        public static AuthMessage CreateEncryptedAuthRequest(string username, string password, string clientPublicKey)
        {
            return new AuthMessage
            {
                Type = "AUTH_REQUEST_ENCRYPTED",
                Username = username,
                Password = password,
                PublicKey = clientPublicKey,
                KeyExchangeStep = "AUTH_WITH_KEY_EXCHANGE",
                IsEncrypted = true,
                Message = "Authentication request with key exchange"
            };
        }
    }
}
