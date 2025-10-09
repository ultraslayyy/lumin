using System.Diagnostics;
using Microsoft.Web.WebView2.Core;
using System.Reflection;
using System.Text.Json;

namespace _;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        if (CoreWebView2Environment.GetAvailableBrowserVersionString() == null)
        {
            string boostrapperUrl = "https://go.microsoft.com/fwlink/p/?LinkId=2124703";
            string tempPath = Path.Combine(Path.GetTempPath(), "MicrosoftEdgeWebVier2Setup.exe");

            try
            {
                using (HttpClient client = new())
                {
                    var data = client.GetByteArrayAsync(boostrapperUrl).GetAwaiter().GetResult();
                    File.WriteAllBytes(tempPath, data);
                }

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = tempPath,
                        Arguments = "/silent /install",
                        UseShellExecute = false
                    }
                };
                process.Start();
                process.WaitForExit();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to install WebView2 Runtime: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        CheckForUpdatesAsync().GetAwaiter().GetResult();

        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        Application.Run(new Form1());
    }

    private static async Task CheckForUpdatesAsync()
    {
        string currentVersion = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";

        using var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("LuminBrowser");

        try
        {
            var json = await client.GetStringAsync("https://api.github.com/repos/ultraslayyy/Lumin/releases/latest");
            var release = JsonSerializer.Deserialize<GitHubRelease>(json);

            if (release != null && release.Tag_name.TrimStart('v') != currentVersion)
            {
                var result = MessageBox.Show($"A new version ({release.Tag_name}) is available. Download now?", "Update Available", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                if (result == DialogResult.Yes)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = release.Html_url,
                        UseShellExecute = true
                    });
                }
            }
        }
        catch
        {

        }
    }

    private record GitHubRelease(string Tag_name, string Html_url);
}