using System.Security.Cryptography;

namespace BgCommon.Helpers;

/// <summary>
/// 字符串加密类.
/// </summary>
internal static class StringEncrypt
{
    /// <summary>
    /// 字符串处理.
    /// </summary>
    /// <param name="rawString">原始字符串.</param>
    /// <param name="password">密码.</param>
    /// <returns>加密后的字符串.</returns>
    public static string Parse(this string rawString, string password)
    {
        if (string.IsNullOrEmpty(rawString))
        {
            return string.Empty;
        }

        if (string.IsNullOrEmpty(password))
        {
            return rawString;
        }

        byte[] sourceBytes = Encoding.UTF8.GetBytes(rawString);
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
        byte[] resultBytes = new byte[sourceBytes.Length];

        // 循环按位与，password 长度不足时循环使用
        for (int i = 0; i < sourceBytes.Length; i++)
        {
            int passwordIndex = i % passwordBytes.Length;
            resultBytes[i] = (byte)(sourceBytes[i] ^ passwordBytes[passwordIndex]);
        }

        // 转换为 Base64 避免二进制数据乱码
        return Convert.ToBase64String(resultBytes);
    }

    /// <summary>
    /// 加密.
    /// </summary>
    /// <param name="rawString">待加密内容.</param>
    /// <param name="key">加密密钥（自动处理为 32 字节）.</param>
    /// <returns>Base64 编码的加密字符串.</returns>
    public static string Encrypt(this string rawString, string key)
    {
        // AES 要求密钥长度为 16/24/32 字节（对应 128/192/256 位），这里处理为 32 字节
        byte[] keyBytes = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
        byte[] contentBytes = Encoding.UTF8.GetBytes(rawString);

        // 使用 AES.Create() 替代过时的实现类（.NET 推荐写法）
        using var aes = Aes.Create();
        aes.Key = keyBytes;

        // 生成随机 IV（初始化向量），提升安全性，加密后一起返回
        aes.GenerateIV();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var ms = new MemoryStream();

        // 先写入 IV（解密时需要用）
        ms.Write(aes.IV, 0, aes.IV.Length);

        using var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);
        cs.Write(contentBytes, 0, contentBytes.Length);
        cs.FlushFinalBlock();

        // 合并 IV + 加密内容，转换为 Base64
        return Convert.ToBase64String(ms.ToArray());
    }

    /// <summary>
    /// 解密.
    /// </summary>
    /// <param name="encryptedContent">加密后的 Base64 字符串.</param>
    /// <param name="key">解密密钥（需与加密密钥一致）.</param>
    /// <returns>原始字符串.</returns>
    public static string Decrypt(this string encryptedContent, string key)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
        byte[] encryptedBytes = Convert.FromBase64String(encryptedContent);

        using var aes = Aes.Create();
        aes.Key = keyBytes;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var ms = new MemoryStream(encryptedBytes);

        // 先读取 IV（AES 的 IV 长度固定为 16 字节）
        byte[] iv = new byte[16];
        ms.Read(iv, 0, iv.Length);
        aes.IV = iv;

        using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using var sr = new StreamReader(cs, Encoding.UTF8);

        // 读取解密后的内容
        return sr.ReadToEnd();
    }
}