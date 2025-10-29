using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace quickLink.Services
{
    public class ClipboardService
    {
        public void CopyToClipboard(string text)
     {
 if (string.IsNullOrEmpty(text))
           return;

 var dataPackage = new DataPackage();
  dataPackage.SetText(text);
       Clipboard.SetContent(dataPackage);
        }

        public async Task<bool> OpenUrlAsync(string url)
     {
      try
       {
    var uri = new Uri(url);
     return await Windows.System.Launcher.LaunchUriAsync(uri);
            }
 catch
{
            return false;
  }
        }
    }
}
