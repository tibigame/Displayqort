using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Forms;
using System.IO;
using Microsoft.Win32;


namespace DisplayQort
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
#if DEBUG
        const int TIME_SPAN = 5; // 更新間隔 (秒)
#else
        const int TIME_SPAN = 30; // 更新間隔 (秒)
#endif

        WindowManager wm = new WindowManager();
        object lockObject = new object();

        DispatcherTimer dispatcherTimer;

        private NotifyIconEx _notify;
        private RECT ThisWindowRext;

        public MainWindow()
        {
            InitializeComponent();

            ThisWindowRext = new RECT((int)Left, (int)Top, (int)(Left + Width), (int)(Top + Height));
            ThisWindowRext = new RECT(250, 100, 640, 480);

            // 通知領域の設定
            var iconPath = new Uri("pack://application:,,,/DisplayQort;component/displayqort.ico", UriKind.Absolute);
            var menu = (System.Windows.Controls.ContextMenu)this.FindResource("TaskMenu");
            this._notify = new NotifyIconEx(iconPath, "Notify Title", menu);

            this.Title = "Displayqort"; // ウィンドウタイトル設定
            this._notify.Text = this.Title;
            this.ShowInTaskbar = false; // タスクバー非表示
            this.Visibility = Visibility.Hidden; // 初期状態で通知領域のみに表示

            wm.GetWindowList();
            wm.SetBelongMonitor(new MultiMonitor());
            SaveWindow();

            SystemEvents.DisplaySettingsChanged += new EventHandler(MonitorChanged); // モニター変更のイベント登録


            void callback(object sender, EventArgs e)
            {
                SaveWindow();
            }

            // ウィンドウ保存関数を一定間隔で呼び出す
            dispatcherTimer = new DispatcherTimer(DispatcherPriority.Background);
            dispatcherTimer.Interval = new TimeSpan(0, 0, TIME_SPAN);
            dispatcherTimer.Tick += new EventHandler(callback);
            dispatcherTimer.Start();
        }

        private void MonitorChanged(object sender, EventArgs e)
        {
            WindowManager.MonitorChanged();
            DebugBlock.Text = $"{ new MultiMonitor().ToString()}";
            System.Windows.Forms.MessageBox.Show(Properties.Resources.MonitorChanged, Properties.Resources.Warning, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }

        public void SaveWindow()
        {
            lock (lockObject)
            {
                var dateStr = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                if (WindowManager.IsSave)
                {
                    InfoText.Text = $"{Properties.Resources.SaveTrue} {dateStr}";
                }
                else
                {
                    InfoText.Text = $"{Properties.Resources.SaveFalse} {dateStr}";
                }

                if (!WindowManager.IsSave)
                {
                    return;
                }
                wm.GetWindowList();
                var m = new MultiMonitor();
                wm.SetBelongMonitor(m);
                wm.Save();
                DebugBlock.Text = $"{m.ToString()}";
            }
        }

        private void Button_Minimize(object sender, RoutedEventArgs e)
        {
            Visibility = Visibility.Hidden;
        }

        private void Button_Load(object sender, RoutedEventArgs e)
        {
            // ここにロードコードを書く
            lock (lockObject)
            {
                wm.Reload();
                wm.Renew();
            }
        }

        /// <summary>
        /// メインウィンドウを表示させます
        /// </summary>
        private void ShowMainWindow(object sender, RoutedEventArgs e)
        {
            Visibility = Visibility.Visible;
        }

        /// <summary>
        /// アプリケーションを終了させます
        /// </summary>
        private void Exit(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // ウィンドウを閉じる際に、タスクトレイのアイコンを削除する。
            this._notify.Dispose();

            SystemEvents.DisplaySettingsChanged -= new EventHandler(MonitorChanged); // モニター変更のイベントを解除しておく
        }
    }
}
