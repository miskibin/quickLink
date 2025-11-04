using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace quickLink
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private MainWindow? _window;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
            
            // Add unhandled exception handler
            this.UnhandledException += (sender, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"UNHANDLED EXCEPTION: {e.Message}");
                System.Diagnostics.Debug.WriteLine($"Exception type: {e.Exception?.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {e.Exception?.StackTrace}");
                
                // Write to a log file
                try
                {
                    var logPath = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "QuickLink",
                        "crash.log"
                    );
                    var logDirectory = System.IO.Path.GetDirectoryName(logPath);
                    if (!string.IsNullOrEmpty(logDirectory))
                    {
                        Directory.CreateDirectory(logDirectory);
                        File.AppendAllText(logPath, $"\n[{DateTime.Now}] CRASH:\n{e.Message}\n{e.Exception}\n");
                    }
                }
                catch { }
                
                e.Handled = true;
            };
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();
            // Window will be hidden initially and shown via Ctrl+Space
        }

        public void HideMainWindow()
        {
            _window?.HideWindow();
        }
    }
}
