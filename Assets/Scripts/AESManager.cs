using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

// AES加密和解密工具类
public static class AESManager
{
    // 固定盐值
    private static readonly string FixedSalt = "PalmDuelbyGICF";

    // 生成动态密钥
    private static byte[] GetKey()
    {
        // 获取设备唯一标识符
        string deviceId = SystemInfo.deviceUniqueIdentifier;

        // 使用SHA256算法生成哈希值
        using (var sha256 = SHA256.Create())
        {
            // 将设备标识符与盐值组合
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(deviceId + FixedSalt));

            // 取前16字节作为AES密钥
            byte[] key = new byte[16];
            Array.Copy(hashBytes, key, key.Length);
            return key;
        }
    }

    // 生成随机的16字节初始向量IV
    private static byte[] GenerateRandomIV()
    {
        // 定义16字节数组
        byte[] iv = new byte[16];

        // 使用加密安全随机数生成器填充IV
        using (var rng = new RNGCryptoServiceProvider())
        {
            rng.GetBytes(iv);
        }
        return iv;
    }

    // 使用AES算法加密明文
    public static string Encrypt(string plainText)
    {
        using (Aes aes = Aes.Create())
        {
            // 设置AES密钥
            aes.Key = GetKey();

            // 生成随机的初始向量IV
            aes.IV = GenerateRandomIV();

            // 创建加密器
            ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            // 将明文转换为字节数组
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

            // 执行加密操作
            byte[] encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            // 合并IV和加密后的密文
            byte[] combinedData = new byte[aes.IV.Length + encryptedBytes.Length];
            Buffer.BlockCopy(aes.IV, 0, combinedData, 0, aes.IV.Length);
            Buffer.BlockCopy(encryptedBytes, 0, combinedData, aes.IV.Length, encryptedBytes.Length);

            // 将结果转换为Base64字符串返回
            return Convert.ToBase64String(combinedData);
        }
    }

    // 使用AES算法解密密文
    public static string Decrypt(string encryptedText)
    {
        // 将Base64字符串转换为字节数组
        byte[] combinedData = Convert.FromBase64String(encryptedText);

        using (Aes aes = Aes.Create())
        {
            // 分离初始向量IV和密文
            byte[] iv = new byte[16];
            byte[] encryptedBytes = new byte[combinedData.Length - iv.Length];
            // 复制前16字节作为IV
            Buffer.BlockCopy(combinedData, 0, iv, 0, iv.Length);
            // 剩余部分为密文
            Buffer.BlockCopy(combinedData, iv.Length, encryptedBytes, 0, encryptedBytes.Length);

            // 设置AES密钥和IV
            aes.Key = GetKey();
            aes.IV = iv;

            // 创建解密器
            ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            // 执行解密操作
            byte[] plainBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);

            // 将解密后的字节数组转换为字符串返回
            return Encoding.UTF8.GetString(plainBytes);
        }
    }
}