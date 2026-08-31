using System;
using System.Collections.Generic;

namespace KwCursor
{
    /// <summary>
    /// Ls11/Ls12 封包读取器（曹操传/英杰传系列通用容器）。
    /// 格式为社区公开逆向成果（轩辕春秋 van/Maxwell 等），此实现为本项目原创代码。
    ///
    /// 结构：
    ///   0x000  "Ls11"/"Ls12" + 12 字节填充
    ///   0x010  256 字节字典
    ///   0x110  条目表：每条 12 字节（大端 u32：压缩长度、原始长度、偏移），压缩长度 0 结束；
    ///          压缩长度 == 原始长度 表示未压缩存储
    /// 解压：MSB 位流。变长码 = 前缀（读 1 直到 0）+ 等长补码；
    ///   码值 &lt; 0x100 输出 dictionary[码值]；
    ///   码值 &gt;= 0x100 为回溯复制：偏移 = 码值-0x100，长度 = 下一码值+3。
    /// </summary>
    public class E5Archive
    {
        public struct Entry
        {
            public int Compressed;
            public int Original;
            public int Offset;
        }

        byte[] data;
        readonly byte[] dict = new byte[256];
        readonly List<Entry> entries = new List<Entry>();

        public int Count { get { return entries.Count; } }
        public Entry GetEntry(int i) { return entries[i]; }

        public static E5Archive Parse(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 0x110 + 12) return null;
            if (bytes[0] != (byte)'L' || bytes[1] != (byte)'s' || bytes[2] != (byte)'1') return null;
            if (bytes[3] != (byte)'1' && bytes[3] != (byte)'2') return null;

            var a = new E5Archive();
            a.data = bytes;
            Array.Copy(bytes, 0x10, a.dict, 0, 256);

            int off = 0x110;
            while (off + 12 <= bytes.Length)
            {
                int comp = ReadBE(bytes, off);
                int orig = ReadBE(bytes, off + 4);
                int pos = ReadBE(bytes, off + 8);
                off += 12;
                if (comp == 0) break;
                if (orig < 0 || pos < 0 || pos + Math.Max(comp, 0) > bytes.Length) return null;
                a.entries.Add(new Entry { Compressed = comp, Original = orig, Offset = pos });
            }
            return a;
        }

        static int ReadBE(byte[] b, int i)
        {
            return (b[i] << 24) | (b[i + 1] << 16) | (b[i + 2] << 8) | b[i + 3];
        }

        public byte[] Extract(int index)
        {
            Entry e = entries[index];
            var outBuf = new byte[e.Original];
            if (e.Compressed == e.Original)
            {
                Array.Copy(data, e.Offset, outBuf, 0, e.Original);
                return outBuf;
            }

            int pos = e.Offset;
            int curByte = 0;
            int nbits = 0;
            int produced = 0;

            int Bit()
            {
                if (nbits == 0)
                {
                    curByte = data[pos++];
                    nbits = 8;
                }
                int b = (curByte >> 7) & 1;
                curByte = (curByte << 1) & 0xFF;
                nbits--;
                return b;
            }

            int Bits(int n)
            {
                int v = 0;
                for (int i = 0; i < n; i++) v = (v << 1) | Bit();
                return v;
            }

            int Code()
            {
                int c1 = 0, n = 0, b;
                do
                {
                    b = Bit();
                    c1 = (c1 << 1) | b;
                    n++;
                } while (b != 0);
                return c1 + Bits(n);
            }

            while (produced < e.Original)
            {
                int code = Code();
                if (code < 0x100)
                {
                    outBuf[produced++] = dict[code];
                    continue;
                }
                int back = code - 0x100;
                if (back > produced) throw new InvalidOperationException("Ls12 数据损坏：回溯越界");
                int count = Code() + 3;
                for (int i = 0; i < count && produced < e.Original; i++)
                {
                    outBuf[produced] = outBuf[produced - back];
                    produced++;
                }
            }
            return outBuf;
        }
    }
}
