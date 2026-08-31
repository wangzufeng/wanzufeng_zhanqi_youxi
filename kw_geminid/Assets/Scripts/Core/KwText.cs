using System;
using System.Text;

namespace KwGeminid
{
    /// <summary>
    /// GBK 文本解码工具（处理中文字符集与定长/以 0 结尾的字符串）。
    /// </summary>
    public static class KwText
    {
        static Encoding gbk;

        static Encoding GBK
        {
            get
            {
                if (gbk == null)
                {
                    try { gbk = Encoding.GetEncoding("GBK"); }
                    catch { gbk = Encoding.GetEncoding(936); }
                }
                return gbk;
            }
        }

        public static string DecodeGbkZ(byte[] data, int offset, int maxLen, string fallback = null)
        {
            if (data == null || offset < 0 || offset >= data.Length) return fallback;
            int len = 0;
            while (len < maxLen && offset + len < data.Length && data[offset + len] != 0) len++;
            if (len == 0) return fallback;
            try
            {
                string s = GBK.GetString(data, offset, len).Trim();
                return string.IsNullOrEmpty(s) ? fallback : s;
            }
            catch
            {
                return fallback;
            }
        }
    }
}
