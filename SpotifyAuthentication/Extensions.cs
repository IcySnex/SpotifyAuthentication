using System.Net;
using System.Text;
using System.Text.Json;

namespace SpotifyAuthentication;

public static class Extensions
{
    public static T[] Slice<T>(
        this T[] array,
        int start,
        int length)
    {
        T[] result = new T[length];
        Array.Copy(array, start, result, 0, length);

        return result;
    }


    public static void HandleRequest(
        this HttpListenerResponse response,
        int statusCode,
        dynamic result)
    {
        byte[] body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(result));

        response.ContentLength64 = body.Length;
        response.ContentType = "application/json";
        response.OutputStream.Write(body, 0, body.Length);

        response.StatusCode = statusCode;
        response.Close();
    }


    public static void HandleBadRequest(
        this HttpListenerResponse response,
        int status,
        string statusString) =>
        response.HandleRequest(400, new
        {
            status = status,
            statusString = statusString,
            spotifyError = 0,
            responseSource = "eSDK",
        });

    public static void HandleOKRequest(
        this HttpListenerResponse response) =>
        response.HandleRequest(200, new
        {
            status = 101,
            statusString = "OK",
            spotifyError = 0,
            responseSource = "eSDK"
        });
}
