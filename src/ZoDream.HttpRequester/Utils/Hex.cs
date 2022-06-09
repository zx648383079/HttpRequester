using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZoDream.HttpRequester.Utils
{
    public static class Hex
    {
        public static byte[] ToByte(string text)
        {
            var prefix = "0x";
            var length = 2;
            var buffer = new List<byte>();
            var items = text.Split(new char[] { ' ', '\n', '\r', '\t' });
            if (text.Contains(prefix))
            {
                // 根据前缀来拆
                var temp = new List<string>();
                foreach (var item in items)
                {
                    if (string.IsNullOrEmpty(item))
                    {
                        continue;
                    }
                    foreach (var it in item.Split(new string[] { prefix }, StringSplitOptions.None))
                    {
                        if (string.IsNullOrEmpty(it))
                        {
                            continue;
                        }
                        temp.Add(it);
                    }
                }
                items = temp.ToArray();
            }
            else if (!text.Contains(' '))
            {
                // 根据长度来拆
                var temp = new List<string>();
                foreach (var item in items)
                {
                    if (string.IsNullOrEmpty(item))
                    {
                        continue;
                    }
                    for (int i = 0; i < item.Length; i += length)
                    {
                        var j = Math.Min(i + length, length);
                        temp.Add(item.Substring(i, j - i));
                    }
                }
                items = temp.ToArray();
            }
            foreach (var item in items)
            {
                if (string.IsNullOrEmpty(item))
                {
                    continue;
                }
                buffer.Add(Convert.ToByte(item, 16));
            }
            return buffer.ToArray();
        }
    }
}
