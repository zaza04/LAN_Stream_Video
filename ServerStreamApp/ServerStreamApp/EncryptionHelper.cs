using System;
using System.Security.Cryptography;
using System.Text;

namespace ServerStreamApp
{
    public static class EncryptionHelper
    {
        // RSA key size
        private const int RSA_KEY_SIZE = 2048;

        // Event để log các hoạt động mã hóa
        public static event Action<string>? OnLogMessage;

        private static void LogMessage(string message)
        {
            var logEntry = $"[ENCRYPTION] {DateTime.Now:HH:mm:ss.fff} - {message}";
            Console.WriteLine(logEntry);
            OnLogMessage?.Invoke(logEntry);
        }

        /// <summary>
        /// Tạo cặp khóa RSA (công khai/riêng tư)
        /// </summary>
        /// <returns>Tuple chứa (PublicKey, PrivateKey)</returns>
        public static (string PublicKey, string PrivateKey) GenerateRSAKeyPair()
        {
            LogMessage("Bắt đầu tạo cặp khóa RSA...");
            
            try
            {
                using (var rsa = RSA.Create(RSA_KEY_SIZE))
                {
                    var publicKey = Convert.ToBase64String(rsa.ExportRSAPublicKey());
                    var privateKey = Convert.ToBase64String(rsa.ExportRSAPrivateKey());
                    
                    LogMessage($"Tạo cặp khóa RSA thành công - Public key length: {publicKey.Length}, Private key length: {privateKey.Length}");
                    return (publicKey, privateKey);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Lỗi khi tạo cặp khóa RSA: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Mã hóa dữ liệu bằng khóa công khai RSA
        /// </summary>
        /// <param name="data">Dữ liệu cần mã hóa</param>
        /// <param name="publicKey">Khóa công khai RSA (Base64)</param>
        /// <returns>Dữ liệu đã mã hóa (Base64)</returns>
        public static string RSAEncrypt(string data, string publicKey)
        {
            LogMessage($"Bắt đầu mã hóa RSA - Data length: {data?.Length ?? 0}");
            
            try
            {
                using (var rsa = RSA.Create())
                {
                    rsa.ImportRSAPublicKey(Convert.FromBase64String(publicKey), out _);
                    var dataBytes = Encoding.UTF8.GetBytes(data ?? "");
                    var encryptedBytes = rsa.Encrypt(dataBytes, RSAEncryptionPadding.OaepSHA256);
                    var result = Convert.ToBase64String(encryptedBytes);
                    
                    LogMessage($"Mã hóa RSA thành công - Input: {(data?.Length ?? 0)} bytes, Output: {result.Length} chars");
                    return result;
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Lỗi khi mã hóa RSA: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Giải mã dữ liệu bằng khóa riêng tư RSA
        /// </summary>
        /// <param name="encryptedData">Dữ liệu đã mã hóa (Base64)</param>
        /// <param name="privateKey">Khóa riêng tư RSA (Base64)</param>
        /// <returns>Dữ liệu gốc</returns>
        public static string RSADecrypt(string encryptedData, string privateKey)
        {
            LogMessage($"Bắt đầu giải mã RSA - Encrypted data length: {encryptedData?.Length ?? 0}");
            
            try
            {
                using (var rsa = RSA.Create())
                {
                    rsa.ImportRSAPrivateKey(Convert.FromBase64String(privateKey ?? ""), out _);
                    var encryptedBytes = Convert.FromBase64String(encryptedData ?? "");
                    var decryptedBytes = rsa.Decrypt(encryptedBytes, RSAEncryptionPadding.OaepSHA256);
                    var result = Encoding.UTF8.GetString(decryptedBytes);
                    
                    LogMessage($"Giải mã RSA thành công - Input: {(encryptedData?.Length ?? 0)} chars, Output: {result.Length} bytes");
                    return result;
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Lỗi khi giải mã RSA: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Tạo khóa AES ngẫu nhiên (256-bit)
        /// </summary>
        /// <returns>Khóa AES (Base64)</returns>
        public static string GenerateAESKey()
        {
            LogMessage("Bắt đầu tạo khóa AES 256-bit...");
            
            try
            {
                using (var aes = Aes.Create())
                {
                    aes.KeySize = 256;
                    aes.GenerateKey();
                    var result = Convert.ToBase64String(aes.Key);
                    
                    LogMessage($"Tạo khóa AES thành công - Key length: {result.Length} chars");
                    return result;
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Lỗi khi tạo khóa AES: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Mã hóa dữ liệu bằng AES
        /// </summary>
        /// <param name="plainText">Dữ liệu gốc</param>
        /// <param name="key">Khóa AES (Base64)</param>
        /// <returns>Dữ liệu đã mã hóa (Base64) bao gồm IV</returns>
        public static string AESEncrypt(string plainText, string key)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                LogMessage("AES Encrypt: Dữ liệu đầu vào rỗng, trả về chuỗi rỗng");
                return string.Empty;
            }

            LogMessage($"Bắt đầu mã hóa AES - Plain text length: {plainText.Length}");

            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = Convert.FromBase64String(key);
                    aes.GenerateIV();

                    LogMessage($"Đã tạo IV mới - IV length: {aes.IV.Length} bytes");

                    using (var encryptor = aes.CreateEncryptor())
                    using (var msEncrypt = new System.IO.MemoryStream())
                    {
                        // Ghi IV vào đầu stream
                        msEncrypt.Write(aes.IV, 0, aes.IV.Length);

                        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        using (var swEncrypt = new System.IO.StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }

                        var result = Convert.ToBase64String(msEncrypt.ToArray());
                        LogMessage($"Mã hóa AES thành công - Input: {plainText.Length} chars, Output: {result.Length} chars");
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Lỗi khi mã hóa AES: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Giải mã dữ liệu AES
        /// </summary>
        /// <param name="cipherText">Dữ liệu đã mã hóa (Base64)</param>
        /// <param name="key">Khóa AES (Base64)</param>
        /// <returns>Dữ liệu gốc</returns>
        public static string AESDecrypt(string cipherText, string key)
        {
            if (string.IsNullOrEmpty(cipherText))
            {
                LogMessage("AES Decrypt: Dữ liệu đầu vào rỗng, trả về chuỗi rỗng");
                return string.Empty;
            }

            LogMessage($"Bắt đầu giải mã AES - Cipher text length: {cipherText.Length}");

            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = Convert.FromBase64String(key);

                    var fullCipher = Convert.FromBase64String(cipherText);
                    
                    // Đọc IV từ đầu dữ liệu
                    var iv = new byte[aes.BlockSize / 8];
                    var cipher = new byte[fullCipher.Length - iv.Length];

                    Array.Copy(fullCipher, 0, iv, 0, iv.Length);
                    Array.Copy(fullCipher, iv.Length, cipher, 0, cipher.Length);

                    aes.IV = iv;

                    LogMessage($"Đã trích xuất IV - IV length: {iv.Length} bytes, Cipher length: {cipher.Length} bytes");

                    using (var decryptor = aes.CreateDecryptor())
                    using (var msDecrypt = new System.IO.MemoryStream(cipher))
                    using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    using (var srDecrypt = new System.IO.StreamReader(csDecrypt))
                    {
                        var result = srDecrypt.ReadToEnd();
                        LogMessage($"Giải mã AES thành công - Input: {cipherText.Length} chars, Output: {result.Length} chars");
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Lỗi khi giải mã AES: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Mã hóa mảng byte bằng AES (cho video streaming)
        /// </summary>
        /// <param name="data">Dữ liệu gốc</param>
        /// <param name="key">Khóa AES (Base64)</param>
        /// <returns>Dữ liệu đã mã hóa bao gồm IV</returns>
        public static byte[] AESEncryptBytes(byte[] data, string key)
        {
            if (data == null || data.Length == 0)
            {
                LogMessage("AES Encrypt Bytes: Dữ liệu đầu vào rỗng, trả về mảng rỗng");
                return new byte[0];
            }

            LogMessage($"Bắt đầu mã hóa AES bytes - Data length: {data.Length} bytes");

            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = Convert.FromBase64String(key);
                    aes.GenerateIV();

                    using (var encryptor = aes.CreateEncryptor())
                    using (var msEncrypt = new System.IO.MemoryStream())
                    {
                        // Ghi IV vào đầu stream
                        msEncrypt.Write(aes.IV, 0, aes.IV.Length);

                        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        {
                            csEncrypt.Write(data, 0, data.Length);
                        }

                        var result = msEncrypt.ToArray();
                        LogMessage($"Mã hóa AES bytes thành công - Input: {data.Length} bytes, Output: {result.Length} bytes");
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Lỗi khi mã hóa AES bytes: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Giải mã mảng byte AES (cho video streaming)
        /// </summary>
        /// <param name="encryptedData">Dữ liệu đã mã hóa</param>
        /// <param name="key">Khóa AES (Base64)</param>
        /// <returns>Dữ liệu gốc</returns>
        public static byte[] AESDecryptBytes(byte[] encryptedData, string key)
        {
            if (encryptedData == null || encryptedData.Length == 0)
            {
                LogMessage("AES Decrypt Bytes: Dữ liệu đầu vào rỗng, trả về mảng rỗng");
                return new byte[0];
            }

            LogMessage($"Bắt đầu giải mã AES bytes - Encrypted data length: {encryptedData.Length} bytes");

            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = Convert.FromBase64String(key);

                    // Đọc IV từ đầu dữ liệu
                    var iv = new byte[aes.BlockSize / 8];
                    var cipher = new byte[encryptedData.Length - iv.Length];

                    Array.Copy(encryptedData, 0, iv, 0, iv.Length);
                    Array.Copy(encryptedData, iv.Length, cipher, 0, cipher.Length);

                    aes.IV = iv;

                    using (var decryptor = aes.CreateDecryptor())
                    using (var msDecrypt = new System.IO.MemoryStream(cipher))
                    using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    using (var output = new System.IO.MemoryStream())
                    {
                        csDecrypt.CopyTo(output);
                        var result = output.ToArray();
                        LogMessage($"Giải mã AES bytes thành công - Input: {encryptedData.Length} bytes, Output: {result.Length} bytes");
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Lỗi khi giải mã AES bytes: {ex.Message}");
                throw;
            }
        }
    }
}