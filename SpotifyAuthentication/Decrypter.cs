using System.Security.Cryptography;
using System.Text;

namespace SpotifyAuthentication;

public class Decryptor
{
    public static byte[] ZeroConf(
        byte[] encryptionKey,
        byte[] iv,
        byte[] encrypted)
    {
        using Aes aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        aes.Key = encryptionKey;

        byte[] counter = new byte[16];
        Array.Copy(iv, counter, iv.Length);

        int blockSize = aes.BlockSize / 8;
        byte[] decrypted = new byte[encrypted.Length];

        using ICryptoTransform decryptor = aes.CreateEncryptor();
        for (int i = 0; i < encrypted.Length; i += blockSize)
        {
            byte[] counterBlock = new byte[blockSize];
            EncryptCounter(decryptor, counter, counterBlock);

            int blockEnd = Math.Min(i + blockSize, encrypted.Length);
            for (int j = i; j < blockEnd; j++)
                decrypted[j] = (byte)(encrypted[j] ^ counterBlock[j - i]);

            IncrementCounter(counter);
        }

        return Convert.FromBase64String(Encoding.UTF8.GetString(decrypted));
    }

    static void EncryptCounter(
        ICryptoTransform decryptor,
        byte[] counter,
        byte[] counterBlock) =>
        decryptor.TransformBlock(counter, 0, counter.Length, counterBlock, 0);

    static void IncrementCounter(
        byte[] counter)
    {
        for (int i = counter.Length - 1; i >= 0; i--)
        {
            counter[i]++;

            if (counter[i] != 0)
                break;
        }
    }


    public static (byte[] Data, AuthenticationType Type) Blob(
        string deviceId,
        string username,
        byte[] blob)
    {
        byte[] secret = SHA1.HashData(Encoding.UTF8.GetBytes(deviceId));
        byte[] baseKey = Rfc2898DeriveBytes.Pbkdf2(secret, Encoding.UTF8.GetBytes(username), 0x100, HashAlgorithmName.SHA1, 20);

        byte[] keyShort = SHA1.HashData(baseKey);
        byte[] key = new byte[keyShort.Length + 4];
        Array.Copy(keyShort, key, keyShort.Length);
        key[keyShort.Length] = 0x00;
        key[keyShort.Length + 1] = 0x00;
        key[keyShort.Length + 2] = 0x00;
        key[keyShort.Length + 3] = 0x14;

        using Aes aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;

        using ICryptoTransform decryptor = aes.CreateDecryptor();
        using MemoryStream ms = new(blob);
        using CryptoStream cs = new(ms, decryptor, CryptoStreamMode.Read);

        byte[] decryptedBlob = new byte[blob.Length];
        int bytesRead = cs.Read(decryptedBlob, 0, decryptedBlob.Length);
        for (int i = 0; i < bytesRead - 0x10; i++)
            decryptedBlob[bytesRead - i - 1] ^= decryptedBlob[bytesRead - i - 0x11];

        using MemoryStream blobStream = new(decryptedBlob);

        blobStream.Seek(1, SeekOrigin.Begin);
        int le = ReadBlobInt(blobStream);

        blobStream.Seek(le + 1, SeekOrigin.Current);
        AuthenticationType type = (AuthenticationType)ReadBlobInt(blobStream);

        blobStream.Seek(1, SeekOrigin.Current);
        int l = ReadBlobInt(blobStream);

        byte[] data = new byte[l];
        blobStream.Read(data, 0, data.Length);

        return (data, type);
    }

    static int ReadBlobInt(
        MemoryStream buffer)
    {
        byte lo = (byte)buffer.ReadByte();
        if ((lo & 0x80) == 0)
            return lo;

        byte hi = (byte)buffer.ReadByte();
        return (lo & 0x7F) | (hi << 7);
    }
}
