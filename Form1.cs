using System.Text.Json;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using System.ComponentModel.DataAnnotations;

namespace _;

public partial class Form1 : Form
{
    readonly WebView2 webView;
    readonly TextBox addressBar;
    readonly Button backBtn, forwardBtn, reloadBtn, bookmarkBtn, bookmarkDropdownBtn, historyDropdownBtn;
    readonly ContextMenuStrip bookmarksDropdown, historyDropdown;
    readonly List<string> history = [];

    public Form1()
    {
        InitializeComponent();
        Text = "Lumin";
        Width = 1200; Height = 800;

        backBtn = new Button
        {
            Text = "←",
            Width = 30,
            Anchor = AnchorStyles.Top | AnchorStyles.Left
        };
        forwardBtn = new Button
        {
            Text = "→",
            Width = 30,
            Anchor = AnchorStyles.Top | AnchorStyles.Left
        };
        reloadBtn = new Button
        {
            Text = "⟳",
            Width = 30,
            Anchor = AnchorStyles.Top | AnchorStyles.Left
        };
        bookmarkBtn = new Button
        {
            Text = "★",
            Width = 40,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        bookmarkDropdownBtn = new Button
        {
            Text = "▼",
            Width = 30,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        historyDropdownBtn = new Button
        {
            Text = "⏰",
            Width = 40,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        addressBar = new TextBox
        {
            Width = 900,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        Controls.Add(backBtn);
        Controls.Add(forwardBtn);
        Controls.Add(reloadBtn);
        Controls.Add(bookmarkBtn);
        Controls.Add(bookmarkDropdownBtn);
        Controls.Add(historyDropdownBtn);
        Controls.Add(addressBar);

        int toolbarTop = 5;
        backBtn.Top = forwardBtn.Top = reloadBtn.Top = bookmarkBtn.Top = bookmarkDropdownBtn.Top = historyDropdownBtn.Top = addressBar.Top = toolbarTop;

        backBtn.Left = 10;
        forwardBtn.Left = backBtn.Right + 5;
        reloadBtn.Left = forwardBtn.Right + 5;
        addressBar.Left = reloadBtn.Right + 5;
        bookmarkBtn.Left = addressBar.Right + 5;
        bookmarkDropdownBtn.Left = bookmarkBtn.Right + 2;
        historyDropdownBtn.Left = bookmarkDropdownBtn.Right + 5;

        bookmarksDropdown = new ContextMenuStrip
        {
            ShowImageMargin = false,
            RightToLeft = RightToLeft.No
        };
        historyDropdown = new ContextMenuStrip
        {
            ShowImageMargin = false,
            RightToLeft = RightToLeft.No
        };

        bookmarkDropdownBtn.Click += (s, e) =>
        {
            ShowDropdown(bookmarkBtn, bookmarksDropdown);
        };
        historyDropdownBtn.Click += (s, e) =>
        {
            ShowDropdown(historyDropdownBtn, historyDropdown);
        };

        webView = new WebView2
        {
            Left = 10,
            Top = backBtn.Bottom + 5,
            Width = ClientSize.Width - 20,
            Height = ClientSize.Height,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };
        webView.Height = ClientSize.Height - webView.Top - 10;
        Controls.Add(webView);

        Load += async (s, e) =>
        {
            await webView.EnsureCoreWebView2Async();
            webView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

            webView.Source = new Uri("https://google.com");

            webView.CoreWebView2.HistoryChanged += (s, e) =>
            {
                var currentUrl = webView.Source.ToString();
                if (!string.IsNullOrWhiteSpace(currentUrl))
                {
                    addressBar.Text = currentUrl;
                    backBtn.Enabled = webView.CanGoBack;
                    forwardBtn.Enabled = webView.CanGoForward;

                    if (history.Count == 0 || history[^1] != currentUrl)
                    {
                        history.Add(currentUrl);

                        var item = new ToolStripMenuItem(currentUrl);
                        item.Click += (_, _) => Navigate(currentUrl);
                        historyDropdown.Items.Insert(0, item);

                        const int MaxHistory = 100;
                        if (history.Count > MaxHistory)
                        {
                            history.RemoveAt(0);
                            historyDropdown.Items.RemoveAt(historyDropdown.Items.Count - 1);
                        }
                    }
                }
            };

            webView.CoreWebView2.DocumentTitleChanged += (s, e) =>
            {
                if (history.Count > 0 && history[^1] == webView.Source.ToString())
                {
                    string newTitle = webView.CoreWebView2.DocumentTitle;
                    if (!string.IsNullOrWhiteSpace(newTitle))
                    {
                        historyDropdown.Items[0].Text = newTitle;
                    }
                }
            };

            RefreshBookmarksDropdown();
        };

        addressBar.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                Navigate(addressBar.Text);
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        };

        reloadBtn.Click += (s, e) => webView.Reload();
        backBtn.Click += (s, e) => { if (webView.CanGoBack) webView.GoBack(); };
        forwardBtn.Click += (s, e) => { if (webView.CanGoForward) webView.GoForward(); };
        bookmarkBtn.Click += (s, e) =>
        {
            var bookmarks = BookmarkManager.Load();
            bookmarks.Add(new Bookmark
            {
                Title = webView.CoreWebView2.DocumentTitle,
                Url = webView.Source.ToString()
            });
            BookmarkManager.Save(bookmarks);
            MessageBox.Show("Bookmark saved!", "Bookmarks", MessageBoxButtons.OK, MessageBoxIcon.Information);
            RefreshBookmarksDropdown();
            ShowDropdown(bookmarkBtn, bookmarksDropdown);
        };
    }

    private async void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            var msg = e.TryGetWebMessageAsString();
            using var doc = JsonDocument.Parse(msg);
            var root = doc.RootElement;
            var action = root.GetProperty("action").GetString() ?? "";

            switch (action)
            {
                case "getBookmarks":
                    var bookmarks = BookmarkManager.Load();
                    webView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(bookmarks));
                    break;
                case "open":
                    if (root.TryGetProperty("url", out var urlEl))
                        Navigate(urlEl.GetString() ?? "");
                    break;
                case "getHistory":
                    webView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(history));
                    break;
            }
        }
        catch (Exception ex)
        {
            webView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(new { error = ex.Message }));
        }
    }

    private void ShowDropdown(Control btn, ContextMenuStrip menu)
    {
        Size dropdownSize = menu.GetPreferredSize(new Size(0, 0));
        Point buttonBottomLeft = new(btn.Left, btn.Bottom);
        Point screenPoint = btn.Parent!.PointToScreen(buttonBottomLeft);
        Rectangle workingArea = Screen.FromControl(this).WorkingArea;

        if (screenPoint.X + dropdownSize.Width > workingArea.Right) screenPoint.X = workingArea.Right - dropdownSize.Width;
        if (screenPoint.Y + dropdownSize.Height > workingArea.Bottom) screenPoint.Y = screenPoint.Y - dropdownSize.Height - btn.Height;

        menu.Show(screenPoint);
    }

    private void Navigate(string url)
    {
        if (url.StartsWith("lumin://", StringComparison.OrdinalIgnoreCase))
        {
            NavigateInternal(url);
            return;
        }

        if (url.StartsWith("file:///", StringComparison.OrdinalIgnoreCase))
        {
            NavigateFile(url);
            return;
        }

        if (File.Exists(url))
        {
            string fileUrl = new Uri(url).AbsoluteUri;
            NavigateFile(fileUrl);
            return;
        }

        if (url.StartsWith("about:", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("blob:", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                webView.Source = new Uri(url);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Invalid URL format: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        if (url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            HandleDataUrl(url);
            return;
        }

        if (!url.Contains("://"))
            url = "https://" + url;
        try
        {
            webView.Source = new Uri(url);
        }
        catch
        {
            MessageBox.Show("Invalid URL", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RefreshBookmarksDropdown()
    {
        bookmarksDropdown.Items.Clear();
        var bookmarks = BookmarkManager.Load();
        foreach (var bm in bookmarks)
        {
            var item = new ToolStripMenuItem($"{bm.Title} ({bm.Url})")
            {
                Padding = new Padding(8, 2, 8, 2)
            };
            item.Click += (s, e) => webView.Source = new Uri(bm.Url);
            bookmarksDropdown.Items.Add(item);
        }
    }

    private void NavigateInternal(string url)
    {
        string page = url.Replace("lumin://", "").ToLowerInvariant();

        switch (page)
        {
            case "settings":
                ShowInternalPage("Settings");
                break;
            case "bookmarks":
                ShowInternalPage("Bookmarks");
                break;
            default:
                ShowInternalPage($"lumin://{page}", "404 - Page Not Found");
                break;
        }
    }

    private void ShowInternalPage(string title, string fallback = "")
    {
        string html = File.ReadAllText($"internal/{title}.html");
        webView.NavigateToString(html);
        addressBar.Text = $"lumin://{title.ToLower()}";
    }

    private void NavigateFile(string fileUrl)
    {
        try
        {
            Uri uri = new(fileUrl);
            if (!File.Exists(uri.LocalPath))
            {
                MessageBox.Show("File not found: " + uri.LocalPath, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            webView.Source = uri;
            addressBar.Text = fileUrl;
        }
        catch (Exception ex)
        {
            MessageBox.Show("Failed to open file:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void HandleDataUrl(string url)
    {
        try
        {
            int commaIndex = url.IndexOf(',');
            if (commaIndex == -1)
            {
                MessageBox.Show("Invalid data URL format", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string header = url[5..commaIndex];
            string dataPart = url[(commaIndex + 1)..];

            bool isBase64 = header.Contains(";base64", StringComparison.OrdinalIgnoreCase);
            string mimeType = "text/plain";

            if (!string.IsNullOrWhiteSpace(header))
            {
                string[] parts = header.Split(';', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0 && parts[0].Contains('/'))
                    mimeType = parts[0];
            }

            string content;
            if (isBase64)
            {
                byte[] bytes = Convert.FromBase64String(dataPart);
                content = System.Text.Encoding.UTF8.GetString(bytes);
            }
            else
            {
                content = Uri.UnescapeDataString(dataPart);
            }

            if (mimeType.Equals("text/html", StringComparison.OrdinalIgnoreCase))
            {
                webView.NavigateToString(content);
            }
            else if (mimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                string html = $"<html><body style='margin:0'><img src='{url}' /></body></html>";
                webView.NavigateToString(html);
            }
            else
            {
                string html = $"<pre style='white-space: pre-wrap; font-family: monospace'>{System.Web.HttpUtility.HtmlEncode(content)}</pre>";
                webView.NavigateToString(html);
            }

            addressBar.Text = url;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to handle data URL:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

public class Bookmark
{
    public string Title { get; set; } = "";
    public string Url { get; set; } = "";
}

public static class BookmarkManager
{
    public static readonly string FilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Lumin", "bookmarks.json");

    public static List<Bookmark> Load()
    {
        if (!File.Exists(FilePath)) return [];
        try
        {
            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<List<Bookmark>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static readonly JsonSerializerOptions s_writeOptions = new()
    {
        WriteIndented = true
    };

    public static void Save(List<Bookmark> bookmarks)
    {
        string? folder = Path.GetDirectoryName(FilePath);
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder ?? "bookmarks.json");

        var json = JsonSerializer.Serialize(bookmarks, s_writeOptions);
        File.WriteAllText(FilePath, json);
    }
}
