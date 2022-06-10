using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZoDream.HttpRequester.Utils
{
    public static class Disk
    {
        public static string FormatSize(long? size)
        {
            if (size == null)
            {
                return "0 B";
            }
            var len = size.ToString()!.Length;
            if (len < 4)
            {
                return $"{size} B";
            }
            if (len < 7)
            {
                return Math.Round(Convert.ToDouble(size / 1024d), 2) + " KB";
            }
            if (len < 10)
            {
                return Math.Round(Convert.ToDouble(size / 1024d / 1024), 2) + " MB";
            }
            if (len < 13)
            {
                return Math.Round(Convert.ToDouble(size / 1024d / 1024 / 1024), 2) + " GB";
            }
            if (len < 16)
            {
                return Math.Round(Convert.ToDouble(size / 1024d / 1024 / 1024 / 1024), 2) + " TB";
            }
            return Math.Round(Convert.ToDouble(size / 1024d / 1024 / 1024 / 1024 / 1024), 2) + " PB";
        }
    }
}
