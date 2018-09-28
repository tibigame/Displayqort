using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.Win32;


namespace DisplayQort
{
    /// <summary>
    /// 個々のウィンドウの情報を表す構造体
    /// </summary>
    [Serializable]
    public class WindowObj
    {
        // ウィンドウの情報

        public String ClassName;
        public String TitleName;
        public RECTSIZE Size;
        public RECT Rect;
        public WINDOWPLACEMENT WindowPlacement;
        public IntPtr hWnd;

        // モニターとの関係

        /// <summary>
        /// MultiMonitorのインデックスに対応する
        /// ウィンドウが主に所属するモニターを表す
        /// 無所属の場合はOutOfRangeMonitorを指定すること
        /// </summary>
        public int BelongMonitor;
        /// <summary>
        /// ウィンドウが範囲外などでモニターに所属していない時に設定するモニター番号
        /// </summary>
        public const int OutOfRangeMonitor = System.Int32.MaxValue;
        /// <summary>
        /// モニターに再配置の必要があるかのフラグ
        /// </summary>
        public bool NeedRelocate;

        public WindowObj(IntPtr hWnd_)
        {
            hWnd = hWnd_;
        }
        public override String ToString() =>
            $"{TitleName}, {ClassName} {Rect}[Belong]{BelongMonitor}[Relocate]{NeedRelocate}{WindowPlacement}";
    }

    [Serializable]
    public class WindowManager
    {
        public List<WindowObj> WindowList;
        private static readonly String Filename = @"./windows.dat";
        private MultiMonitor Monitor;

        public static bool IsSave = true; // この値がfalseならwindowのsaveを止める

        public WindowManager()
        {
            WindowList = new List<WindowObj> { };
        }

        // モニターが変更された時に呼ばれる関数
        // ウィンドウ位置保存をストップさせる
        public static void MonitorChanged()
        {
            IsSave = false;
        }

        public void GetWindowList()
        {
            WindowList.Clear();
            //すべてのウィンドウを列挙する
            Common.EnumWindows(EnumWindowCallBack, (IntPtr)null);
        }

        private bool EnumWindowCallBack(IntPtr hWnd, IntPtr lparam)
        {
            // 可視状態でないものを除く
            if ((Common.GetWindowLongStyle(hWnd) & Common.WS_VISIBLE) == 0)
            {
                return true;
            }
            if ((Common.GetWindowLongExStyle(hWnd) & Common.WS_EX_NOREDIRECTIONBITMAP) != 0)
            {
                return true;
            }
            // タスクバーに表示されているものを除く
            if ((Common.GetWindowLongExStyle(hWnd) & Common.WS_EX_TOOLWINDOW) != 0)
            {
                return true;
            }

            //ウィンドウのタイトルの長さを取得する
            int textLen = Common.GetWindowTextLength(hWnd);
            if (0 < textLen)
            {
                WindowObj TempWindow = new WindowObj(hWnd);
                //ウィンドウのタイトルを取得する
                StringBuilder tsb = new StringBuilder(textLen + 1);
                Common.GetWindowText(hWnd, tsb, tsb.Capacity);
                TempWindow.TitleName = tsb.ToString();

                //ウィンドウのクラス名を取得する
                StringBuilder csb = new StringBuilder(256);
                Common.GetClassName(hWnd, csb, csb.Capacity);
                TempWindow.ClassName = csb.ToString();

                // ウィンドウ位置を取得する
                var rc = new RECT();
                Common.GetWindowRect(hWnd, out rc);
                RECTSIZE rsize = (RECTSIZE)rc;
                TempWindow.Rect = rc;
                TempWindow.Size = rsize;

                // ウィンドウ情報を取得する
                WINDOWPLACEMENT wp = new WINDOWPLACEMENT();
                Common.GetWindowPlacement(hWnd, out wp);
                TempWindow.WindowPlacement = wp;

                if (rsize.Height != 0 || rsize.Width != 0)
                {
                    WindowList.Add(TempWindow);
                }
            }
            return true;
        }
        public override String ToString()
        {
            String tmp = $"";
            foreach (var window in WindowList)
            {
                if (window.WindowPlacement.showCmd == SW.SHOWNORMAL)
                {
                    tmp += $"{window}\r\n";
                }
            }
            return tmp;
        }

        /// <summary>
        /// MultiMonitorを受け取り
        /// BelongMonitorとNeedRelocateの値を設定する
        /// </summary>
        public void SetBelongMonitor(MultiMonitor multiMonitor)
        {
            Monitor = multiMonitor;
            foreach (var window in WindowList)
            {
                var tempBelong = WindowObj.OutOfRangeMonitor;
                foreach (var screen in multiMonitor.ScreenList)
                {
                    // 完全に内包するモニタを見つけた
                    if (Common.IsInclude(screen.Bounds, window.Rect))
                    {
                        window.BelongMonitor = screen.ScreenNum;
                        window.NeedRelocate = false;
                        goto ENDLOOP;
                    }
                    // 一部内包するモニターだった (一部のみのモニターが複数存在するときはどっちの所属でもいい)
                    if (Common.ExtraInclude(screen.Bounds, window.Rect) == Relation.OtherInclude)
                    {
                        tempBelong = screen.ScreenNum;
                    }
                }
                // 範囲外モニターならノータッチ
                if (tempBelong != WindowObj.OutOfRangeMonitor)
                {
                    window.BelongMonitor = tempBelong;
                    window.NeedRelocate = true;
                }
                ENDLOOP:;
            }
        }

        /// <summary>
        /// Monitorの値が現在の値と一致しているか
        /// </summary>
        public bool IsMonitorEqual()
        {
            return Monitor == new MultiMonitor();
        }

        /// <summary>
        /// WindowListの値をファイルに保存する
        /// </summary>
        public void Save()
        {
            using (Stream stream = File.OpenWrite(Filename))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, WindowList);
            }
        }

        /// <summary>
        /// WindowListを保存された値に戻す
        /// </summary>
        public void Reload()
        {
            if (File.Exists(Filename))
            {
                using (Stream stream = File.OpenRead(Filename))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    WindowList = (List<WindowObj>)formatter.Deserialize(stream);
                }
            }
        }

        /// <summary>
        /// 全てのWindowを保存された値に戻す
        /// </summary>
        public void Renew()
        {
            // モニタの構造が保存時と異なるときは警告を出す
            if (!IsMonitorEqual())
            {
                var r = System.Windows.Forms.MessageBox.Show(
                    Properties.Resources.MonitorDiffer, Properties.Resources.Warning,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation,
                    MessageBoxDefaultButton.Button2);
                if (r == DialogResult.No) { return; }
            }
            foreach (var window in WindowList)
            {
                Common.SetWindowPlacement(window.hWnd, ref window.WindowPlacement);
                // MessageBox.Show(window.ToString());
            }
            // 復元完了したのでウィンドウ保存を再開してもいい
            IsSave = true;
        }

        /// <summary>
        /// 全てのWindowをpointだけ動かす
        /// </summary>
        public void MoveWindows(POINT point)
        {
            foreach (var window in WindowList)
            {
                window.WindowPlacement.normalPosition.Left += point.X;
                window.WindowPlacement.normalPosition.Right += point.X;
                window.WindowPlacement.normalPosition.Top += point.Y;
                window.WindowPlacement.normalPosition.Bottom += point.Y;
                Common.SetWindowPlacement(window.hWnd, ref window.WindowPlacement);
            }
        }
    }
}
