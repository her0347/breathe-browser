using CefSharp;
using System;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Breathe
{
    public class CustomMenuHandler : IContextMenuHandler
    {
        private readonly IWebBrowser browserControlInstance;

        private const int ShowDevToolsId = 26501;
        private const int CloseDevToolsId = 26502;
        private const int SaveImageId = 26503;
        private const int CopyImageId = 26504;
        private const int PasteImageId = 26505;
        private const int PasteTextId = 26506;


        public CustomMenuHandler(IWebBrowser browserControl)
        {
            browserControlInstance = browserControl;
        }


        public void OnBeforeContextMenu(
            IWebBrowser browserControl,
            IBrowser browser,
            IFrame frame,
            IContextMenuParams parameters,
            IMenuModel model)
        {
            model.Clear();

            model.AddItem((CefMenuCommand)ShowDevToolsId, "Show DevTools");
            model.AddItem((CefMenuCommand)CloseDevToolsId, "Close DevTools");


            if (parameters.MediaType == ContextMenuMediaType.Image &&
                !string.IsNullOrEmpty(parameters.SourceUrl))
            {
                model.AddSeparator();

                model.AddItem((CefMenuCommand)SaveImageId, "Save Image");
                model.AddItem((CefMenuCommand)CopyImageId, "Copy Image");
            }


            if (Clipboard.ContainsImage() || Clipboard.ContainsText())
            {
                model.AddSeparator();

                if (Clipboard.ContainsImage())
                {
                    model.AddItem(
                        (CefMenuCommand)PasteImageId,
                        "Paste Image"
                    );
                }

                if (Clipboard.ContainsText())
                {
                    model.AddItem(
                        (CefMenuCommand)PasteTextId,
                        "Paste Text"
                    );
                }
            }
        }



        public bool OnContextMenuCommand(
            IWebBrowser browserControl,
            IBrowser browser,
            IFrame frame,
            IContextMenuParams parameters,
            CefMenuCommand commandId,
            CefEventFlags eventFlags)
        {

            if (commandId == (CefMenuCommand)ShowDevToolsId)
            {
                browser.GetHost().ShowDevTools();
                return true;
            }


            if (commandId == (CefMenuCommand)CloseDevToolsId)
            {
                browser.GetHost().CloseDevTools();
                return true;
            }


            if (commandId == (CefMenuCommand)SaveImageId)
            {
                browser.GetHost().StartDownload(parameters.SourceUrl);
                return true;
            }


            if (commandId == (CefMenuCommand)CopyImageId)
            {
                CopyImage(parameters.SourceUrl);
                return true;
            }


            if (commandId == (CefMenuCommand)PasteImageId)
            {
                PasteImage();
                return true;
            }


            if (commandId == (CefMenuCommand)PasteTextId)
            {
                PasteText();
                return true;
            }


            return false;
        }



        private async void CopyImage(string url)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    byte[] data = await client.GetByteArrayAsync(url);

                    using (MemoryStream ms = new MemoryStream(data))
                    using (Image image = Image.FromStream(ms))
                    {
                        Bitmap bitmap = new Bitmap(image);

                        if (browserControlInstance is Control control)
                        {
                            control.Invoke(new Action(() =>
                            {
                                Clipboard.Clear();
                                Clipboard.SetImage(bitmap);
                            }));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Copy image failed:\n" + ex.Message
                );
            }
        }



        private void PasteImage()
        {
            try
            {
                if (!Clipboard.ContainsImage())
                    return;


                Image image = Clipboard.GetImage();

                if (image == null)
                    return;


                string base64 = ImageToBase64(image);


                string script = $@"
                document.execCommand(
                    'insertImage',
                    false,
                    '{base64}'
                );
                ";


                browserControlInstance.ExecuteScriptAsync(script);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Paste image failed:\n" + ex.Message
                );
            }
        }



        private void PasteText()
        {
            try
            {
                if (!Clipboard.ContainsText())
                    return;


                string text = Clipboard.GetText();

                text = text
                    .Replace("\\", "\\\\")
                    .Replace("'", "\\'");


                string script = $@"
                document.execCommand(
                    'insertText',
                    false,
                    '{text}'
                );
                ";


                browserControlInstance.ExecuteScriptAsync(script);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Paste text failed:\n" + ex.Message
                );
            }
        }



        private string ImageToBase64(Image image)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                image.Save(
                    ms,
                    System.Drawing.Imaging.ImageFormat.Png
                );

                return
                    "data:image/png;base64," +
                    Convert.ToBase64String(ms.ToArray());
            }
        }



        public void OnContextMenuDismissed(
            IWebBrowser browserControl,
            IBrowser browser,
            IFrame frame)
        {
        }



        public bool RunContextMenu(
            IWebBrowser browserControl,
            IBrowser browser,
            IFrame frame,
            IContextMenuParams parameters,
            IMenuModel model,
            IRunContextMenuCallback callback)
        {
            return false;
        }
    }
}