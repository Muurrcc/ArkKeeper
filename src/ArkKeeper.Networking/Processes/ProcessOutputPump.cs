using System.Text;

namespace ArkKeeper.Networking.Processes;

/// <summary>Reads a text stream and invokes <paramref name="onOutput"/> per line, treating a bare
/// carriage return as a line terminator too — not just '\n'. CLI tools like SteamCMD report
/// progress with '\r'-only updates meant to overwrite one line in a real terminal;
/// <c>Process.OutputDataReceived</c>/<c>BeginOutputReadLine</c> only split on '\n', so those
/// updates sit buffered until the next actual newline, making a live progress log look frozen for
/// long stretches and then dump everything at once.</summary>
public static class ProcessOutputPump
{
    public static async Task PumpAsync(TextReader reader, Action<string>? onOutput, CancellationToken cancellationToken = default)
    {
        var buffer = new char[256];
        var line = new StringBuilder();
        int read;
        while ((read = await reader.ReadAsync(buffer, cancellationToken)) > 0)
        {
            for (var i = 0; i < read; i++)
            {
                var c = buffer[i];
                if (c is '\r' or '\n')
                {
                    if (line.Length > 0)
                    {
                        onOutput?.Invoke(line.ToString());
                        line.Clear();
                    }
                }
                else
                {
                    line.Append(c);
                }
            }
        }

        if (line.Length > 0)
        {
            onOutput?.Invoke(line.ToString());
        }
    }
}
