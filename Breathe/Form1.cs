using CefSharp;
using CefSharp.WinForms;
using com.sun.security.auth;
using EasyTabs;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing;
using System.IO;
using System.IO;
using System.Linq;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Breathe
{
    public partial class Form1 : Form
    {

        protected TitleBarTabs ParentTabs
        {
            get
            {
                return (ParentForm as TitleBarTabs);
            }
        }

        // When a TitleBarTab is created to host this Form, the creator may set this.
        public TitleBarTab ParentTab { get; set; }

        // Favicon cache and fetch tasks to make icons appear faster and avoid duplicate downloads
        private static readonly HttpClient _favHttpClient = new HttpClient() { Timeout = TimeSpan.FromSeconds(2) };
        private static readonly ConcurrentDictionary<string, Task<Image>> _faviconTasks = new ConcurrentDictionary<string, Task<Image>>();
        private static readonly ConcurrentDictionary<string, Image> _faviconCache = new ConcurrentDictionary<string, Image>();

        private string GetFaviconKey(Uri uri)
        {
            if (uri.Scheme == Uri.UriSchemeFile)
            {
                var folder = Path.GetDirectoryName(uri.LocalPath) ?? Application.StartupPath;
                return "file:" + Path.GetFullPath(folder).ToLowerInvariant();
            }
            return (uri.Scheme + "://" + uri.Host).ToLowerInvariant();
        }

        private Task<Image> StartFaviconFetch(Uri uri)
        {
            return Task.Run(async () =>
            {
                try
                {
                    // Local file: look for favicon.ico next to file or in startup
                    if (uri.Scheme == Uri.UriSchemeFile)
                    {
                        string folder = Path.GetDirectoryName(uri.LocalPath) ?? Application.StartupPath;
                        string[] localCandidates = { Path.Combine(folder, "favicon.ico"), Path.Combine(Application.StartupPath, "favicon.ico"), Path.Combine(folder, "favicon.png"), Path.Combine(Application.StartupPath, "favicon.png") };
                        foreach (var p in localCandidates)
                        {
                            if (!File.Exists(p)) continue;
                            try
                            {
                                if (p.EndsWith(".ico", StringComparison.OrdinalIgnoreCase))
                                {
                                    using (var ic = new Icon(p))
                                    {
                                        var bmp = ic.ToBitmap();
                                        return (Image)new Bitmap(bmp);
                                    }
                                }
                                else
                                {
                                    using (var img = Image.FromFile(p))
                                    {
                                        return (Image)new Bitmap(img);
                                    }
                                }
                            }
                            catch { }
                        }
                        return null;
                    }

                    // HTTP(S): try host root /favicon.ico then /favicon.png, also try www.
                    var hosts = new List<string> { uri.Host };
                    if (!uri.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase)) hosts.Add("www." + uri.Host);
                    foreach (var h in hosts)
                    {
                        var baseUrl = uri.Scheme + "://" + h;
                        string[] candidates = { baseUrl + "/favicon.ico", baseUrl + "/favicon.png" };
                        foreach (var cand in candidates)
                        {
                            try
                            {
                                using (var resp = await _favHttpClient.GetAsync(
                                    cand,
                                    HttpCompletionOption.ResponseHeadersRead))
                                {
                                    if (!resp.IsSuccessStatusCode) continue;
                                    var data = await resp.Content.ReadAsByteArrayAsync();
                                    if (data == null || data.Length == 0) continue;
                                    using (var ms = new MemoryStream(data))
                                    {
                                        // try ICO first
                                        try
                                        {
                                            var icon = new Icon(ms);
                                            var bmp = icon.ToBitmap();
                                            return (Image)new Bitmap(bmp);
                                        }
                                        catch
                                        {
                                            try
                                            {
                                                ms.Position = 0;
                                                using (var img = Image.FromStream(ms))
                                                {
                                                    return (Image)new Bitmap(img);
                                                }
                                            }
                                            catch { }
                                        }
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                }
                catch { }
                return null;
            });
        }


        // Search for a file in the current directory and parent directories up to maxLevels
        private string FindFileInParents(string fileName, int maxLevels = 5)
        {
            try
            {
                string dir = Application.StartupPath;
                for (int i = 0; i < maxLevels && !string.IsNullOrEmpty(dir); i++)
                {
                    string candidate = Path.Combine(dir, fileName);
                    if (File.Exists(candidate)) return candidate;

                    var parent = Directory.GetParent(dir);
                    if (parent == null) break;
                    dir = parent.FullName;
                }
            }
            catch { }

            return null;
        }
        

        // Update the EasyTabs tab image (Bitmap/Image) for the tab that hosts this form
        private void SetTabImage(Image image)
        {
            try
            {
                if (ParentTabs == null) return;

                foreach (var tab in ParentTabs.Tabs)
                {
                    var contentProp = tab.GetType().GetProperty("Content");
                    if (contentProp == null) continue;

                    var content = contentProp.GetValue(tab);
                    if (content != this) continue;

                    var iconProp = tab.GetType().GetProperty("Image") ?? tab.GetType().GetProperty("Icon");
                    if (iconProp != null && iconProp.CanWrite)
                    {
                        if (iconProp.PropertyType == typeof(System.Drawing.Image))
                        {
                            iconProp.SetValue(tab, image);
                        }
                        else if (iconProp.PropertyType == typeof(Icon))
                        {
                            try
                            {
                                var bmp = new Bitmap(image);
                                var hIcon = bmp.GetHicon();
                                var icon = Icon.FromHandle(hIcon);
                                iconProp.SetValue(tab, icon);
                            }
                            catch { }
                        }
                    }

                    var setIconMethod = tab.GetType().GetMethod("SetImage") ?? tab.GetType().GetMethod("SetIcon");
                    if (setIconMethod != null)
                    {
                        var p = setIconMethod.GetParameters();
                        if (p.Length == 1)
                        {
                            if (p[0].ParameterType == typeof(System.Drawing.Image))
                                setIconMethod.Invoke(tab, new object[] { image });
                            else if (p[0].ParameterType == typeof(Icon))
                                setIconMethod.Invoke(tab, new object[] { Icon.FromHandle(((Bitmap)image).GetHicon()) });
                        }
                    }

                    try { ParentTabs.Invalidate(); ParentTabs.Refresh(); ParentTabs.Update(); } catch { }
                    break;
                }
            }
            catch { }
        }

        private bool IsHomepageUri(Uri uri)
        {
            try
            {
                string homepage = FindFileInParents("homepage.html") ?? Path.Combine(Application.StartupPath, "homepage.html");
                if (!File.Exists(homepage)) return false;
                if (uri.Scheme != Uri.UriSchemeFile) return false;
                return string.Equals(Path.GetFullPath(uri.LocalPath), Path.GetFullPath(homepage), StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }
        

        public Form1()
        {
            InitializeComponent();
            // Wire CefSharp browser events to update tab title and favicon
            browser.TitleChanged += Browser_TitleChanged;
            browser.AddressChanged += Browser_AddressChanged;
            browser.LoadingStateChanged += Browser_LoadingStateChanged;

        }

        private void Browser_LoadingStateChanged(object sender, CefSharp.LoadingStateChangedEventArgs e)
        {
            // When loading finished, try to fetch favicon for the current address
            if (!e.IsLoading)
            {
                try
                {
                    var addr = browser.Address;
                    // Fire-and-forget async fetch on UI thread
                    BeginInvoke(new Action(() => { var _ = FetchFaviconAsync(addr); }));

                    // If this is the homepage file, ensure tab title remains "New Tab"
                    try
                    {
                        Uri u = null;
                        try { u = new Uri(addr); } catch { }
                        if (u != null && IsHomepageUri(u))
                        {
                            BeginInvoke(new Action(() =>
                            {
                                this.Text = "New Tab";
                                UpdateTabTitle("New Tab");
                            }));
                        }
                    }
                    catch { }
                }
                catch { }
            }
        }

        private async System.Threading.Tasks.Task FetchFaviconAsync(string address)
        {
            if (string.IsNullOrEmpty(address)) return;

            Uri uri;
            try { uri = new Uri(address); } catch { return; }

            string key = GetFaviconKey(uri);

            // Fast path: cached image
            if (_faviconCache.TryGetValue(key, out var cached))
            {
                try { BeginInvoke(new Action(() => SetTabImage(cached))); } catch { }
                return;
            }

            // Ensure only one fetch per host/folder
            var task = _faviconTasks.GetOrAdd(key, k => StartFaviconFetch(uri));
            Image result = null;
            try
            {
                result = await task;
            }
            catch { result = null; }
            finally
            {
                // remove completed task to allow refresh later
                _faviconTasks.TryRemove(key, out _);
            }

            if (result != null)
            {
                _faviconCache[key] = result;
                try
                {
                    BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            SetTabImage(result);

                            if (ParentTabs != null)
                            {
                                ParentTabs.Invalidate();
                                ParentTabs.Refresh();
                                ParentTabs.Update();
                            }
                        }
                        catch
                        {
                        }
                    }));
                }
                catch
                {
                }
            }
        }

        private void Browser_TitleChanged(object sender, CefSharp.TitleChangedEventArgs e)
        {
            BeginInvoke(new Action(() =>
            {
                // Only update tab title for real pages, keep "New Tab" for the homepage until user navigates
                bool isHome = false;
                try { isHome = IsHomepageUri(new Uri(browser.Address)); } catch { }

                this.Text = isHome ? "New Tab" : e.Title;
                UpdateTabTitle(this.Text);
            }));
        }

        private async void Browser_AddressChanged(object sender, CefSharp.AddressChangedEventArgs e)
        {
            
            try 
            {
                // Update address textbox on UI thread (clear when loading homepage)
                Uri uri = new Uri(e.Address);
                string homepagePath = FindFileInParents("homepage.html") ?? Path.Combine(Application.StartupPath, "homepage.html");
                bool isHomepage = false;
                try { isHomepage = (uri.Scheme == Uri.UriSchemeFile && string.Equals(Path.GetFullPath(uri.LocalPath), Path.GetFullPath(homepagePath), StringComparison.OrdinalIgnoreCase)); } catch { }

                BeginInvoke(new Action(() =>
                {
                    try { txtUrl.Text = isHomepage ? string.Empty : e.Address; } catch { }
                }));



                // Update tab title immediately on address change (use current form title or host/file name)
                try
                {
                    string newTitle = this.Text;
                    if (string.IsNullOrWhiteSpace(newTitle) || newTitle == "New Tab")
                    {
                        newTitle = uri.Scheme == Uri.UriSchemeFile ? Path.GetFileNameWithoutExtension(uri.LocalPath) : uri.Host;
                    }
                    BeginInvoke(new Action(() => UpdateTabTitle(newTitle)));
                }
                catch { }

                // If loading a local file, prefer a local favicon.png (next to the html or in startup path)
                if (uri.Scheme == Uri.UriSchemeFile)
                {
                    string localPath = uri.LocalPath;
                    string folder = Path.GetDirectoryName(localPath) ?? Application.StartupPath;
                    string localPng1 = Path.Combine(folder, "favicon.png");
                    string localIco1 = Path.Combine(folder, "favicon.ico");
                    string localPng2 = Path.Combine(Application.StartupPath, "favicon.png");
                    string localIco2 = Path.Combine(Application.StartupPath, "favicon.ico");
                    string favPath = null;

                    // Prefer PNG then ICO
                    if (File.Exists(localPng1)) favPath = localPng1;
                    else if (File.Exists(localIco1)) favPath = localIco1;
                    else if (File.Exists(localPng2)) favPath = localPng2;
                    else if (File.Exists(localIco2)) favPath = localIco2;

                    if (favPath != null)
                    {
                        try
                        {
                            if (favPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                            {
                                using (var img = Image.FromFile(favPath))
                                {
                                    var bmp = new Bitmap(img);
                                    BeginInvoke(new Action(() => SetTabImage(bmp)));
                                }
                            }
                            else
                            {
                                var icon = new Icon(favPath);
                                BeginInvoke(new Action(() => SetTabIcon(icon)));
                            }
                        }
                        catch { }

                        return;
                    }
                }

                // favicon fetch handled by FetchFaviconAsync (cached + deduped)
            }
            catch
            {
                // ignore failures fetching favicon
            }

        }



        private void Form1_Load(object sender, EventArgs e)
        {
            // Try to find homepage.html in startup path or parent folders (use workspace root if running from bin)
            string homepage = FindFileInParents("homepage.html") ?? Path.Combine(Application.StartupPath, "homepage.html");
            if (File.Exists(homepage))
            {
                browser.Load(new Uri(homepage).AbsoluteUri);
            }


        }


        // Update the EasyTabs tab title for the tab that hosts this form
        private void UpdateTabTitle(string title)
        {
            try
            {
                if (ParentTabs == null) return;

                foreach (var tab in ParentTabs.Tabs)
                {
                    var contentProp = tab.GetType().GetProperty("Content");
                    if (contentProp == null) continue;

                    var content = contentProp.GetValue(tab);
                    if (content != this) continue;

                    // Try common property names for title/text
                    var titleProp = tab.GetType().GetProperty("Title") ?? tab.GetType().GetProperty("Text");
                    if (titleProp != null && titleProp.CanWrite)
                    {
                        titleProp.SetValue(tab, title);
                    }

                    // Force parent to repaint
                    try { ParentTabs.Invalidate(); } catch { }
                    break;
                }
            }
            catch { }
        }

        // Update the EasyTabs tab icon for the tab that hosts this form (uses reflection to be resilient)
        private void SetTabIcon(Icon icon)
        {
            try
            {
                if (ParentTabs == null) return;

                foreach (var tab in ParentTabs.Tabs)
                {
                    var contentProp = tab.GetType().GetProperty("Content");
                    if (contentProp == null) continue;

                    var content = contentProp.GetValue(tab);
                    if (content != this) continue;

                    // Try common property names
                    var iconProp = tab.GetType().GetProperty("Icon") ?? tab.GetType().GetProperty("Image");
                    if (iconProp != null && iconProp.CanWrite)
                    {
                        if (iconProp.PropertyType == typeof(Icon))
                        {
                            iconProp.SetValue(tab, icon);
                        }
                        else if (iconProp.PropertyType == typeof(System.Drawing.Image))
                        {
                            iconProp.SetValue(tab, icon.ToBitmap());
                        }
                    }

                    // Try methods like SetIcon or SetImage
                    var setIconMethod = tab.GetType().GetMethod("SetIcon") ?? tab.GetType().GetMethod("SetImage");
                    if (setIconMethod != null)
                    {
                        var p = setIconMethod.GetParameters();
                        if (p.Length == 1)
                        {
                            if (p[0].ParameterType == typeof(Icon))
                                setIconMethod.Invoke(tab, new object[] { icon });
                            else if (p[0].ParameterType == typeof(System.Drawing.Image))
                                setIconMethod.Invoke(tab, new object[] { icon.ToBitmap() });
                        }
                    }

                    try { ParentTabs.Invalidate(); } catch { }
                    break;
                }
            }
            catch { }
        }




        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;

                string input = txtUrl.Text.Trim();
                string url;

                // If it's already a complete URL
                if (Uri.IsWellFormedUriString(input, UriKind.Absolute))
                {
                    url = input;
                }
                // Looks like a domain name
                else if (input.Contains("."))
                {
                    url = "https://" + input;
                }
                // Otherwise search Google
                else
                {
                    url = "https://www.duckduckgo.com/search?q=" +
                          Uri.EscapeDataString(input);
                }

                if (input == "about:breathe")
                {
                    string about = FindFileInParents("index.html") ?? Path.Combine(Application.StartupPath, "index.html");

                    url = about;
                }

                if (input == "about:credits")
                {
                    string about = FindFileInParents("credits.html") ?? Path.Combine(Application.StartupPath, "credits.html");

                    url = about;
                }


                browser.Load(url);
            }
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            if (browser.CanGoBack)
            {
                browser.Back();
            }
        }

        private void btnForward_Click(object sender, EventArgs e)
        {
            if (browser.CanGoForward)
            {
                browser.Forward();
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            if (browser.IsLoading)
            {
                browser.Stop();
            }
            else
            {
                browser.Reload();
            }
        }

        private void btnDownloads_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads");


        }
    }
}
