using System.Net;
using System.Diagnostics;
using Microsoft.Web.WebView2.Core;

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
                using WebClient client = new WebClient();
                client.DownloadFile(boostrapperUrl, tempPath);

                var process = new Process();
                process.StartInfo.FileName = tempPath;
                process.StartInfo.Arguments = "/silent /install";
                process.StartInfo.UseShellExecute = false;
                process.Start();
                process.WaitForExit();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to install WebView2 Runtime: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        Application.Run(new Form1());
    }
}