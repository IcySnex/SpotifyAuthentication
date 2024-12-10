using System.Security.Cryptography;
using System.Text;

namespace SpotifyAuthentication;

public abstract class Sha1
{
    public static string Random()
    {
        Console.WriteLine("Generating random SHA1...");

        byte[] data = new byte[128];
        System.Random.Shared.NextBytes(data);
        
        byte[] hashBytes = SHA1.HashData(data);
        StringBuilder hashStringBuilder = new();

        foreach (byte b in hashBytes)
            hashStringBuilder.Append(b.ToString("x2"));

        return hashStringBuilder.ToString();
    }
}
