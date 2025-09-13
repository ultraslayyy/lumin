using System.Text.Json;
using Microsoft.Web.WebView2.WinForms;

namespace _;

public partial class Form1 : Form
{
    readonly WebView2 webView;
    readonly TextBox addressBar;
    readonly Button backBtn, forwardBtn, reloadBtn, bookmarkBtn, bookmarkDropdownBtn;
    readonly ContextMenuStrip bookmarksDropdown;

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
        Controls.Add(addressBar);

        int toolbarTop = 5;
        backBtn.Top = forwardBtn.Top = reloadBtn.Top = bookmarkBtn.Top = bookmarkDropdownBtn.Top = addressBar.Top = toolbarTop;

        backBtn.Left = 10;
        forwardBtn.Left = backBtn.Right + 5;
        reloadBtn.Left = forwardBtn.Right + 5;
        addressBar.Left = reloadBtn.Right + 5;
        bookmarkBtn.Left = addressBar.Right + 5;
        bookmarkDropdownBtn.Left = bookmarkBtn.Right + 2;

        bookmarksDropdown = new ContextMenuStrip
        {
            ShowImageMargin = false,
            RightToLeft = RightToLeft.No
        };

        bookmarkDropdownBtn.Click += (s, e) =>
        {
            ShowBookmarksDropdown();
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
            webView.Source = new Uri("https://google.com");

            webView.CoreWebView2.HistoryChanged += (s, e) =>
            {
                addressBar.Text = webView.Source.ToString();
                backBtn.Enabled = webView.CanGoBack;
                forwardBtn.Enabled = webView.CanGoForward;
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
            ShowBookmarksDropdown();
        };
    }

    private void ShowBookmarksDropdown()
    {
        Size dropdownSize = bookmarksDropdown.GetPreferredSize(new Size(0, 0));
        Point buttonBottomLeft = new(bookmarkBtn.Left, bookmarkBtn.Bottom);
        Point screenPoint = bookmarkBtn.Parent!.PointToScreen(buttonBottomLeft);
        Rectangle workingArea = Screen.FromControl(this).WorkingArea;

        if (screenPoint.X + dropdownSize.Width > workingArea.Right) screenPoint.X = workingArea.Right - dropdownSize.Width;
        if (screenPoint.Y + dropdownSize.Height > workingArea.Bottom) screenPoint.Y = screenPoint.Y - dropdownSize.Height - bookmarkBtn.Height;

        bookmarksDropdown.Show(screenPoint);
    }


    private void Navigate(string url)
    {
        if (!url.StartsWith("http://") && !url.StartsWith("https://")) url = "https://" + url;
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
