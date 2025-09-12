using Microsoft.Web.WebView2.WinForms;

namespace _;

public partial class Form1 : Form
{
    readonly WebView2 webView;
    readonly TextBox addressBar;
    readonly Button backBtn, forwardBtn, reloadBtn;

    public Form1()
    {
        InitializeComponent();
        Text = "Lumin";
        Width = 1200; Height = 800;

        backBtn = new Button { Text = "←", Left = 10, Top = 10, Width = 30 };
        forwardBtn = new Button { Text = "→", Left = 50, Top = 10, Width = 30 };
        reloadBtn = new Button { Text = "⟳", Left = 90, Top = 10, Width = 30 };
        addressBar = new TextBox { Left = 130, Top = 10, Width = 940 };

        webView = new WebView2 { Left = 10, Top = 50, Width = 1160, Height = 700 };

        Controls.Add(backBtn);
        Controls.Add(forwardBtn);
        Controls.Add(reloadBtn);
        Controls.Add(addressBar);
        Controls.Add(webView);

        Load += async (s, e) =>
        {
            await webView.EnsureCoreWebView2Async();
            webView.Source = new Uri("https://github.com/ultraslayyy/lumin/tree/dotnet");

            webView.CoreWebView2.HistoryChanged += (s, e) =>
            {
                addressBar.Text = webView.Source.ToString();
            };
            webView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
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

        addressBar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        backBtn.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        forwardBtn.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        reloadBtn.Anchor = AnchorStyles.Top | AnchorStyles.Left;
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
}
