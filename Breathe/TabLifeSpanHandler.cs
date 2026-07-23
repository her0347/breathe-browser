using Breathe;
using CefSharp;
using CefSharp.WinForms;
using System;
using System.Windows.Forms;

public class TabLifeSpanHandler : ILifeSpanHandler
{
    private readonly Form1 mainForm;

    public TabLifeSpanHandler(Form1 form)
    {
        mainForm = form;
    }

    public bool OnBeforePopup(
        IWebBrowser chromiumWebBrowser,
        IBrowser browser,
        IFrame frame,
        string targetUrl,
        string targetFrameName,
        WindowOpenDisposition targetDisposition,
        bool userGesture,
        IPopupFeatures popupFeatures,
        IWindowInfo windowInfo,
        IBrowserSettings browserSettings,
        ref bool noJavascriptAccess,
        out IWebBrowser newBrowser)
    {
        newBrowser = null;

        mainForm.BeginInvoke(new Action(() =>
        {
            chromiumWebBrowser.LoadUrl(targetUrl);
            // Or call a method like:
            // mainForm.CreateNewTab(targetUrl);
        }));

        return true; // Cancel popup
    }

    public void OnAfterCreated(IWebBrowser chromiumWebBrowser, IBrowser browser) { }

    public bool DoClose(IWebBrowser chromiumWebBrowser, IBrowser browser) => false;

    public void OnBeforeClose(IWebBrowser chromiumWebBrowser, IBrowser browser) { }
}