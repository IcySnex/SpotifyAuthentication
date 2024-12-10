using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SpotifyAuthentication;

public class HttpServer
{
    public static string GetLocalIPAddress()
    {
        IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (IPAddress ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
                return ip.ToString();
        }

        throw new InvalidOperationException("No network adapters with an IPv4 address in the system!");
    }

    public static short GetOpenPort()
    {
        short port = 2000;
        using Socket socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);


        while (true)
        {
            try
            {
                socket.Connect("127.0.0.1", port);
                port++;
            }
            catch (SocketException)
            {
                return port;
            }
        }

        throw new InvalidOperationException("No available port found.");
    }


    readonly string hostUrl;
    readonly HttpListener listener = new();
    readonly Thread thread;

    public HttpServer(
        string hostUrl)
    {
        this.hostUrl = hostUrl;
        listener.Prefixes.Add(hostUrl);

        thread = new(() =>
        {
            while (true)
            {
                HttpListenerContext context = listener.GetContext();

                Console.WriteLine($"HTTP Server recieved request: {context.Request.Url}");
                OnRequest?.Invoke(context.Request, context.Response);
            }
        });
    }


    public event Action<HttpListenerRequest, HttpListenerResponse>? OnRequest;

    public bool IsRunning { get; private set; } = false;

    public void Start()
    {
        if (IsRunning)
            return;

        Console.WriteLine($"Starting HTTP Server on hostUrl: {hostUrl}...");
        listener.Start();
        thread.Start();
        IsRunning = true;
    }

    public void Stop()
    {
        if (!IsRunning)
            return;

        Console.WriteLine($"Stopping HTTP Server...");
        thread.Join();
        listener.Stop();
        IsRunning = false;
    }
}
