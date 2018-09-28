using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DisplayQort
{
    /// <summary>
    /// 各モニタの情報を表す構造体
    /// </summary>
    [Serializable]
    public class ScreenObj
    {
        public String DeviceName;
        public int BitsPerPixel;
        public bool Primary;
        public RECT Bounds; // Include TaskBar
        public RECT WorkingArea; // Exclude TaskBar

        public int ScreenNum;

        public ScreenObj(String deviceName, int bitsPerPixel, bool primary, RECT bounds, RECT workingArea, int screenNum)
        {
            DeviceName = deviceName;
            BitsPerPixel = bitsPerPixel;
            Primary = primary;
            Bounds = bounds;
            WorkingArea = workingArea;
            ScreenNum = screenNum;
        }
        public override String ToString() =>
            $"DeviceName: {DeviceName}\nBounds: {Bounds} WorkingArea: {WorkingArea}\nIsPrimary: {Primary} BitsPerPixel: {BitsPerPixel}\n";

    }

    /// <summary>
    /// マルチモニターを管理するクラス
    /// </summary>
    [Serializable]
    public class MultiMonitor
    {
        public int MonitorNum;
        public List<ScreenObj>ScreenList;

        public MultiMonitor()
        {
            MonitorNum = Screen.AllScreens.Length;
            ScreenList = new List<ScreenObj> { };

            int i = 0;
            foreach (var s in Screen.AllScreens)
            {
                ScreenList.Add(new ScreenObj(
                    s.DeviceName, s.BitsPerPixel, s.Primary,
                    new RECT(s.Bounds.Left, s.Bounds.Top, s.Bounds.Right, s.Bounds.Bottom),
                    new RECT(s.WorkingArea.Left, s.WorkingArea.Top, s.WorkingArea.Right, s.WorkingArea.Bottom),
                    i));
                ++i;
            }

        }

        public override String ToString()
        {
            String tmp = $"MonitorNum: {MonitorNum}";
            for (int i = 0; i < MonitorNum; ++i)
            {
                tmp += $"\n{ScreenList[i]}";
            }
            return tmp;
        }

        public override bool Equals(object obj)
        {
            return false;
        }

        public static bool operator ==(MultiMonitor c1, MultiMonitor c2)
        {
            //nullの確認（構造体のようにNULLにならない型では不要）
            //両方nullか（参照元が同じか）
            //(c1 == c2)とすると、無限ループ
            if (object.ReferenceEquals(c1, c2))
            {
                return true;
            }
            //どちらかがnullか
            //(c1 == null)とすると、無限ループ
            if (((object)c1 == null) || ((object)c2 == null))
            {
                return false;
            }
            // モニタの数が異なる
            if (c1.MonitorNum != c2.MonitorNum)
            {
                return false;
            }
            // モニタの座標が異なる
            for (int i = 0; i < c1.MonitorNum; i++)
            {
                var s1 = c1.ScreenList[i];
                var s2 = c2.ScreenList[i];
                if (
                    s1.Bounds.Bottom != s2.Bounds.Bottom
                    || s1.Bounds.Top != s2.Bounds.Top
                    || s1.Bounds.Left != s2.Bounds.Left
                    || s1.Bounds.Right != s2.Bounds.Right
                    )
                {
                    return false;
                }
            }

            return true;
        }

        public static bool operator !=(MultiMonitor c1, MultiMonitor c2)
        {
            return !(c1 == c2);
        }

        public override int GetHashCode()
        {
            var hash = 0;
            for (int i = 0; i < MonitorNum; i++)
            {
                hash += (16 * i + 1) * ((RECTSIZE)ScreenList[i].Bounds).Area();
            }
            return hash;
        }

    }
}
