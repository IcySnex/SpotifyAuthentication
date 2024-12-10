
using System.Diagnostics;

namespace SpotifyAuthentication;

public abstract class Execution
{
    public static bool Run(
        string fileName,
        string arguments = "",
        bool asAdmin = false)
    {
        try
        {
            using Process? process = Process.Start(new ProcessStartInfo()
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = true,
                Verb = asAdmin ? "runas" : null,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });
            if (process is null)
                return false;

            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}