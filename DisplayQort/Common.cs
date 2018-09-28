using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;


namespace DisplayQort
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public int Area() => (Right - Left) * (Top - Bottom);

        public RECT(int left, int top, int right, int bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }
        public override String ToString() => $"[({Left}, {Top}), ({Right}, {Bottom})]";
    }

    [Serializable]
    public struct RECTSIZE
    {
        public int Width;
        public int Height;
        public int Area() => Width * Height;

        public RECTSIZE(int w, int h)
        {
            Width = w;
            Height = h;
        }

        public static explicit operator RECTSIZE(RECT rect) => new RECTSIZE(rect.Right - rect.Left, rect.Bottom - rect.Top);
        public override String ToString() => $"w={Width}, h={Height}";
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;

        public POINT(int x, int y)
        {
            X = x;
            Y = y;
        }
        public override String ToString() => $"({X}, {Y})";
    }

    [Serializable]
    public enum SW
    {
        HIDE = 0,
        SHOWNORMAL = 1,
        SHOWMINIMIZED = 2,
        SHOWMAXIMIZED = 3,
        SHOWNOACTIVATE = 4,
        SHOW = 5,
        MINIMIZE = 6,
        SHOWMINNOACTIVE = 7,
        SHOWNA = 8,
        RESTORE = 9,
        SHOWDEFAULT = 10,
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct WINDOWPLACEMENT
    {
        public int length;
        public int flags;
        public SW showCmd;
        public POINT minPosition;
        public POINT maxPosition;
        public RECT normalPosition;
        public override String ToString() =>
            $"SW{showCmd}Min{minPosition}Max{maxPosition}N{normalPosition}";
    }

    /// <summary>
    /// 包含関係を表す
    /// 含まれる、左上が含まれる、その他一部が含まれる、含まれない
    /// </summary>
    [Serializable]
    public enum Relation
    {
        Include, UpperLeftInclude, OtherInclude, NotInclude
    }

    public class Common
    {
        public const double UpperLeftConst = 0.2;
        public static bool IsInclude(RECT baseObj, POINT targetObj) =>
            baseObj.Left < targetObj.X && targetObj.X < baseObj.Right && baseObj.Top < targetObj.Y && targetObj.Y < baseObj.Bottom;

        public static Relation ExtraInclude(RECT baseObj, RECT targetObj)
        {
            POINT upperLeft = new POINT(targetObj.Left, targetObj.Top);
            POINT lowerRight = new POINT(targetObj.Right, targetObj.Bottom);

            if (IsInclude(baseObj, upperLeft) && IsInclude(baseObj, lowerRight))
            {
                return Relation.Include;
            }

            RECTSIZE upperLeftSize = (RECTSIZE)targetObj;
            POINT upperLeftSpecial = new POINT(
                targetObj.Left + (int)(upperLeftSize.Width * UpperLeftConst),
                targetObj.Top + (int)(upperLeftSize.Height * UpperLeftConst)
                );

            if (IsInclude(baseObj, upperLeft) && IsInclude(baseObj, upperLeftSpecial))
            {
                return Relation.UpperLeftInclude;
            }

            POINT upperRight = new POINT(targetObj.Right, targetObj.Top);
            POINT lowerLeft = new POINT(targetObj.Left, targetObj.Bottom);

            if (IsInclude(baseObj, upperLeft) || IsInclude(baseObj, lowerRight)
                || IsInclude(baseObj, upperRight) || IsInclude(baseObj, lowerLeft))
            {
                return Relation.OtherInclude;
            }

            return Relation.NotInclude;
        }

        public static bool IsInclude(RECT baseObj, RECT targetObj)
        {
            var relation = ExtraInclude(baseObj, targetObj);
            return relation == Relation.Include || relation == Relation.UpperLeftInclude;
        }

        // ウィンドウの列挙
        public delegate bool EnumWindowsDelegate(IntPtr hWnd, IntPtr lparam);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public extern static bool EnumWindows(EnumWindowsDelegate lpEnumFunc, IntPtr lparam);
        // ウィンドウテキストを取得
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd,
            StringBuilder lpString, int nMaxCount);
        // ウィンドウテキストの長さを取得
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowTextLength(IntPtr hWnd);
        // ウィンドウのクラスネームを取得
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
        // ウィンドウ座標を取得
        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr HWND, out RECT rect);
        public bool Getwindowrect(IntPtr hwnd, out RECT rc)
        {
            bool tf = GetWindowRect(hwnd, out rc);
            return tf;
        }
        // ウインドウ情報を取得する
        [DllImport("user32.dll")]
        public static extern bool GetWindowPlacement(IntPtr hWnd, out WINDOWPLACEMENT lpwndpl);
        // ウインドウ情報をセットする
        [DllImport("user32.dll")]
        public static extern bool SetWindowPlacement(IntPtr hWnd, [In] ref WINDOWPLACEMENT lpwndpl);

        // ウインドウ情報を取得する
        [DllImport("user32.dll")]
        public static extern long GetWindowLong(IntPtr hWnd, int nIndex);
        public static long GetWindowLongStyle(IntPtr hWnd) => GetWindowLong(hWnd, GWL_STYLE);
        public static long GetWindowLongExStyle(IntPtr hWnd) => GetWindowLong(hWnd, GWL_EXSTYLE);

        // ウィンドウと指定された関係（ またはオーナー）にあるウィンドウのハンドルを返します
        [DllImport("user32.dll")]
        public static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        public const int GWL_STYLE = -16; // ウインドウスタイルを取得
        public const int GWL_EXSTYLE = -20; // 拡張ウインドウスタイルを取得
        public const long WS_VISIBLE = 0x10000000L;
        public const long WS_EX_NOREDIRECTIONBITMAP = 0x00200000L;
        public const long WS_EX_TOOLWINDOW = 0x00000080L;
        public const uint GW_OWNER = 4;
    }
}
