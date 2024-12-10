using System.Collections.Specialized;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;
using ArkaneSystems.Arkane.Zeroconf;
using SpotifyAuthentication;


// HTTP Server
string deviceId = Sha1.Random();
DiffieHellman diffieHellman = new();

short port = HttpServer.GetOpenPort();
string hostUrl = $"http://{HttpServer.GetLocalIPAddress()}:{port}/";
Execution.Run("netsh", $"http add urlacl url={hostUrl} user=Everyone", true);

HttpServer httpServer = new(hostUrl);
httpServer.OnRequest += (HttpListenerRequest request, HttpListenerResponse response) =>
{
    if (request.Url is null || request.Url.AbsolutePath != "/handle")
    {
        response.HandleBadRequest(102, "ERROR-BAD-REQUEST");
        return;
    }

    using StreamReader reader = new(request.InputStream, request.ContentEncoding);
    string appendedQuery = reader.ReadToEnd();

    NameValueCollection parameters = HttpUtility.ParseQueryString($"{request.Url.Query}&{appendedQuery}".Trim('&', '?'));
    if (parameters["action"] is not string action || action.Length < 1)
    {
        response.HandleBadRequest(303, "ERROR-INVALID-ARGUMENTS");
        return;
    }

    switch (action)
    {
        case "getInfo":
            Console.WriteLine("Returning info about client...");
            response.HandleRequest(200, new
            {
                status = 101,
                statusString = "OK",
                spotifyError = 0,
                responseSource = "eSDK",
                version = "2.7.1",
                deviceID = deviceId,
                publicKey = Convert.ToBase64String(diffieHellman.PublicKey),
                remoteName = "AuthenticaionSample",
                brandDisplayName = "Brand",
                modelDisplayName = "AuthenticaionSample",
                libraryVersion = "1.0.0",
                resolverVersion = "1",
                groupStatus = "NONE",
                tokenType = "default",
                clientID = "1d2cf25b1643474983fae381b00cea3c",
                productID = 30002,
                scope = "streaming,client-authorization-universal",
                availability = ""
            });
            break;
        case "addUser":
            if (request.HttpMethod != "POST")
            {
                response.HandleBadRequest(102, "ERROR-BAD-REQUEST");
                return;
            }

            if (parameters["userName"] is not string userName || userName.Length < 1 ||
                parameters["blob"] is not string blob || blob.Length < 1 ||
                parameters["clientKey"] is not string clientKey || clientKey.Length < 1)
            {
                response.HandleBadRequest(303, "ERROR-INVALID-ARGUMENTS");
                return;
            }

            Console.WriteLine("Decryping blob...");
            byte[] blobBytes = Convert.FromBase64String(blob);
            byte[] sharedKey = diffieHellman.ComputeSharedKey(Convert.FromBase64String(clientKey));

            byte[] iv = blobBytes.Slice(0, 16);
            byte[] encrypted = blobBytes.Slice(16, blobBytes.Length - 36);
            byte[] checksum = blobBytes.Slice(blobBytes.Length - 20, 20);

            using (SHA1 sha1 = SHA1.Create())
            {
                byte[] baseKey = sha1.ComputeHash(sharedKey)[..16];

                using (HMACSHA1 checkSumHmac = new(baseKey))
                {
                    byte[] checksumKey = checkSumHmac.ComputeHash(Encoding.UTF8.GetBytes("checksum"));
                    byte[] encryptionKey = checkSumHmac.ComputeHash(Encoding.UTF8.GetBytes("encryption"));

                    using (HMACSHA1 macHmac = new(checksumKey))
                    {
                        byte[] mac = macHmac.ComputeHash(encrypted);
                        if (!mac.SequenceEqual(checksum))
                        {
                            response.HandleBadRequest(303, "ERROR-INVALID-ARGUMENTS");
                            return;
                        }

                        byte[] decrypted = Decryptor.ZeroConf(encryptionKey.Slice(0, 16), iv, encrypted);
                        (byte[] authData, AuthenticationType authType) = Decryptor.Blob(deviceId, userName, decrypted);

                        File.WriteAllText("credentials.json", JsonSerializer.Serialize(new
                        {
                            username = userName,
                            credentials = authData,
                            type = authType.ToString()
                        }));

                        Console.WriteLine("Blob was decrypted.");
                        httpServer.Stop();

                        response.HandleOKRequest();
                    }
                }
            }
            break;
        default:
            response.HandleBadRequest(302, "ERROR-INVALID-ACTION");
            break;
    }
};
httpServer.Start();


// mDNS Service
RegisterService service = new()
{
    Name = "Melora",
    RegType = "_spotify-connect._tcp",
    ReplyDomain = "local.",
    Port = port,
    TxtRecord = new TxtRecord()
    {
        { "VERSION", "1.0" },
        { "CPath", "/handle" }
    }
};
service.Response += (sender, args) =>
    Console.WriteLine(args.IsRegistered ? "mDNS Service registered successfully!" : "mDNS Service registration failed!");

service.Register();


// Timeout
Console.ReadKey();