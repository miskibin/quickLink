using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace quickLink.Services
{
    public sealed class ClipboardService
    {
        #region Public Methods

        public void CopyToClipboard(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            try
            {
                var dataPackage = new DataPackage();
                dataPackage.SetText(text);
                Clipboard.SetContent(dataPackage);
            }
            catch (Exception)
            {
                // Clipboard operations can fail if another process has the clipboard locked
                // Silently ignore to prevent app crashes
            }
        }

        public async Task<bool> OpenUrlAsync(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            try
            {
                // Validate URI before launching
                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                    return false;

                // Only allow http, https, and mailto schemes for security
                if (uri.Scheme != Uri.UriSchemeHttp && 
                    uri.Scheme != Uri.UriSchemeHttps && 
                    uri.Scheme != Uri.UriSchemeMailto)
                    return false;

                return await Windows.System.Launcher.LaunchUriAsync(uri);
            }
            catch (Exception)
            {
                // Launch can fail for various reasons (no default handler, security policy, etc.)
                return false;
            }
        }

        #endregion
    }
}
