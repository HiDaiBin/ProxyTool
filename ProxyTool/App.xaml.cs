using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Windows;

namespace ProxyTool
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        Mutex mutex;
        public App()
        {
            this.Startup += App_Startup;
        }

        private void App_Startup(object sender, StartupEventArgs e)
        {
            mutex = new Mutex(true, Process.GetCurrentProcess().ProcessName, out bool res);
            if (!res)
                Environment.Exit(0);
        }

    }

}
