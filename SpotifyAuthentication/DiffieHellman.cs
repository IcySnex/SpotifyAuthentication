using System.Numerics;

namespace SpotifyAuthentication;

public class DiffieHellman
{
    public static readonly BigInteger Prime = BigInteger.Parse("1552518092300708935130918131258481755631334049434514313202351194902966239949102107258669453876591642442910007680288864229150803718918046342632727613031282983744380820890196288509170691316593175367469551763119843371637221007210577919");

    static byte[] ToTrimmedByteArray(
        BigInteger input)
    {
        byte[] bytes = input.ToByteArray(true, true);
        int nonZeroIndex = Array.FindIndex(bytes, b => b != 0);

        return bytes[nonZeroIndex..];
    }


    readonly BigInteger privateKey;
    readonly BigInteger publicKey;

    public DiffieHellman()
    {
        byte[] random = new byte[95];
        Random.Shared.NextBytes(random);

        privateKey = new BigInteger(random, true, true);
        publicKey = BigInteger.ModPow(2, privateKey, Prime);
    }


    public byte[] PrivateKey => ToTrimmedByteArray(privateKey);

    public byte[] PublicKey => ToTrimmedByteArray(publicKey);


    public byte[] ComputeSharedKey(
        byte[] remoteKeyBytes)
    {
        BigInteger remoteKey = new(remoteKeyBytes, true, true);
        BigInteger sharedKey = BigInteger.ModPow(remoteKey, privateKey, Prime);

        return ToTrimmedByteArray(sharedKey);
    }
}