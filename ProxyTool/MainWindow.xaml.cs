using HandyControl.Controls;
using HandyControl.Tools.Extension;
using IWshRuntimeLibrary;
using Microsoft.Win32;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ProxyTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private readonly string appDir = AppDomain.CurrentDomain.BaseDirectory;
        private static Dictionary<string, bool> proxyStatusDict = new Dictionary<string, bool>()
        {
            ["IE"] = false,
            ["FireFox"] = false
        };

        public MainWindow()
        {
            InitializeComponent();

            var a = @"d:\prefs.js";

            var txt = System.IO.File.ReadAllLines(a).ToList();
            if(txt.Any(s => s.Contains("network.proxy.http")))
                txt[txt.FindIndex(s => s.Contains("network.proxy.http"))] = "user_pref(\"network.proxy.http\", \"127.0.0.2\");";
            else
                txt.Add("user_pref(\"network.proxy.http\", \"127.0.0.1\");");
            if (txt.Any(s => s.Contains("network.proxy.http_port")))
                txt[txt.FindIndex(s => s.Contains("network.proxy.http_port"))] = "user_pref(\"network.proxy.http_port\", 666);";
            else
                txt.Add("user_pref(\"network.proxy.http_port\", 666);");

            FirefoxProfile profile = new FirefoxProfile();

            System.IO.File.WriteAllLines(a, txt);

        }

        /// <summary>
        /// 关闭Firefox
        /// </summary>
        private void CloseFirefox()
        {
            foreach (var process in Process.GetProcessesByName("firefox"))
            {
                try
                {
                    process.Kill();
                    process.WaitForExit();
                }
                catch 
                {

                }
            } 
        }


        protected override void OnClosed(EventArgs e)
        {
            StopIEProxy();
            base.OnClosed(e);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.Hide();
            notifyIcon.Show();
            CreateShortcut();
            CheckIEProxyStatus("load", null);
        }

        /// <summary>
        /// 检测IE代理状态
        /// </summary>
        protected void CheckIEProxyStatus(object sender, RoutedEventArgs e)
        {
            notifyIcon.Icon = ByteArrayToBitmapImage(Properties.Resources._switch);
            var isEnable = GetRegValue<bool>(new RegistryDto());
            if (isEnable)
            {
                if (sender.ToString() == "load")
                    proxyStatusDict["IE"] = true;
                else
                    StopIEProxy();
            }
            else
            {
                if (sender.ToString() == "load")
                    proxyStatusDict["IE"] = false;
                else
                    StartIEProxy();
            }
            SetNotifyIcon();
        }

        /// <summary>
        /// 打开IE代理
        /// </summary>
        private void StartIEProxy()
        {
            SetRegValue(new RegistryDto() { Value = "1", Type = RegistryValueKind.DWord });
            proxyStatusDict["IE"] = true;
            var proxyServer = GetRegValue<string>(new RegistryDto() { Name = "ProxyServer" });
            notifyIcon.ShowBalloonTip(null, $"IE Proxy is running.{Environment.NewLine}ProxyServer is {proxyServer}", HandyControl.Data.NotifyIconInfoType.Info);
        }

        /// <summary>
        /// 关闭IE代理
        /// </summary>
        private void StopIEProxy()
        {
            SetRegValue(new RegistryDto() { Value = "0", Type = RegistryValueKind.DWord });
            proxyStatusDict["IE"] = false;
            notifyIcon.ShowBalloonTip(null, $"IE Proxy is stoped.", HandyControl.Data.NotifyIconInfoType.Info);
        }

        /// <summary>
        /// 设置托盘图标
        /// </summary>
        private void SetNotifyIcon()
        {
            notifyIcon.Text = string.Join(Environment.NewLine, proxyStatusDict.Select(s => $"{s.Key} Proxy Status：{(s.Value ? "ON" : "OFF")}").ToList());
            if (proxyStatusDict.Any(s => s.Value))
            {
                notifyIcon.Icon = ByteArrayToBitmapImage(Properties.Resources.on);
                //menuItem_ie.IsChecked = true;
                menuItem_ie.Icon = new Image() { Source = ByteArrayToBitmapImage(Properties.Resources.ok) };
            }
            else
            {
                notifyIcon.Icon = ByteArrayToBitmapImage(Properties.Resources.off);
                menuItem_ie.Icon = null;
                //menuItem_ie.IsChecked = false;
            }
        }

        /// <summary>
        /// 修改托盘图标
        /// </summary>
        /// <param name="byteArray"></param>
        public BitmapImage ByteArrayToBitmapImage(byte[] byteArray)
        {
            using (Stream stream = new MemoryStream(byteArray))
            {
                BitmapImage image = new BitmapImage();
                stream.Position = 0;
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = stream;
                image.EndInit();
                image.Freeze();
                return image;
            }
        }



        #region 注册表
        /// <summary>
        /// 获取注册表值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dto"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        private T GetRegValue<T>(RegistryDto dto, T defaultValue = default(T))
        {
            T result = defaultValue;
            using (var reg = dto.Key.OpenSubKey(dto.SubKey))
            {
                var value = reg.GetValue(dto.Name);
                if (value != null)
                    result = (T)Convert.ChangeType(value, typeof(T));
            }
            return result;
        }

        /// <summary>
        /// 设置注册表
        /// </summary>
        /// <param name="dtoArr"></param>
        private void SetRegValue(params RegistryDto[] dtoArr)
        {
            foreach (var dto in dtoArr)
            {
                using (var reg = dto.Key.OpenSubKey(dto.SubKey, true))
                {
                    reg.SetValue(dto.Name, dto.Value, dto.Type);
                }
            }
        }

        /// <summary>
        /// 注册表实体
        /// </summary>
        private class RegistryDto
        {
            /// <summary>
            /// 顶级节点
            /// </summary>
            public RegistryKey Key { get; set; } = Registry.CurrentUser;
            /// <summary>
            /// 子节点
            /// </summary>
            public string SubKey { get; set; } = @"Software\Microsoft\Windows\CurrentVersion\Internet Settings";
            /// <summary>
            /// 键值名
            /// </summary>
            public string Name { get; set; } = "ProxyEnable";
            /// <summary>
            /// 值
            /// </summary>
            public object Value { get; set; }
            /// <summary>
            /// 类型
            /// </summary>
            public RegistryValueKind Type { get; set; } = RegistryValueKind.String;
        }




        #endregion


        /// <summary>
        /// 创建快捷方式
        /// </summary>
        private void CreateShortcut()
        {
            var appName = Process.GetCurrentProcess().ProcessName;
            var appPath = System.IO.Path.Combine(appDir, appName);

            var lnkDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var lnkName = "ProxyTool.lnk";
            var lnkPath = System.IO.Path.Combine(lnkDir, lnkName);

            var shell = new WshShell();
            var shortcut = (IWshShortcut)shell.CreateShortcut(lnkPath);
            if (shortcut.TargetPath != appPath)
            {
                shortcut.TargetPath = appPath;
                shortcut.WorkingDirectory = appDir;
                shortcut.IconLocation = System.IO.Path.Combine(appDir, "icon.ico");
                shortcut.WindowStyle = 1;
                shortcut.Save();
            }
        }
    }
}