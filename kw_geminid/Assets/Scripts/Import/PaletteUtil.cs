using UnityEngine;

namespace KwGeminid
{
    public static class PaletteUtil
    {
        public static Color32[] Build(byte[] palBytes, int entryIndex = 0)
        {
            var pal = new Color32[256];
            if (palBytes == null || palBytes.Length < 16)
            {
                for (int i = 0; i < 256; i++) pal[i] = new Color32((byte)i, (byte)i, (byte)i, 255);
                return pal;
            }

            E5Archive arc = E5Archive.Parse(palBytes);
            byte[] raw = null;
            if (arc != null && arc.Count > 0)
            {
                int idx = Mathf.Clamp(entryIndex, 0, arc.Count - 1);
                try { raw = arc.Extract(idx); } catch { }
            }
            if (raw == null) raw = palBytes;

            for (int i = 0; i < 256; i++)
            {
                int off = i * 3;
                if (off + 2 < raw.Length)
                {
                    // 存储 (b0,b1,b2) -> 显示 (R,G,B) = (b1,b2,b0)
                    byte r = raw[off + 1];
                    byte g = raw[off + 2];
                    byte b = raw[off + 0];
                    pal[i] = new Color32(r, g, b, 255);
                }
                else
                {
                    pal[i] = new Color32((byte)i, (byte)i, (byte)i, 255);
                }
            }
            return pal;
        }
    }
}
