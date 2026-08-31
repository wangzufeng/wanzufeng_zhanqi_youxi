using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace KwCursor
{
    /// <summary>GBK 文本解码（IL2CPP 缺码表时回退占位名）。</summary>
    public static class KwText
    {
        static System.Text.Encoding gbk;
        static bool tried;

        public static string DecodeGbkZ(byte[] b, int off, int maxLen, string fallback = null)
        {
            int end = off;
            int limit = Mathf.Min(off + maxLen, b.Length);
            while (end < limit && b[end] != 0) end++;
            if (!tried)
            {
                tried = true;
                try { gbk = System.Text.Encoding.GetEncoding("gbk"); }
                catch (System.Exception) { gbk = null; }
            }
            if (gbk != null && end > off)
            {
                try { return gbk.GetString(b, off, end - off); }
                catch (System.Exception) { }
            }
            return fallback;
        }
    }

    /// <summary>
    /// 调色板工具。实测字节序：存储 (b0,b1,b2) → 显示 (R,G,B) = (b1,b2,b0)。
    /// </summary>
    public static class PaletteUtil
    {
        public static Color32[] Build(byte[] lsArchiveBytes, int entryIndex)
        {
            byte[] raw = null;
            if (lsArchiveBytes != null)
            {
                E5Archive arc = E5Archive.Parse(lsArchiveBytes);
                if (arc != null && arc.Count > 0)
                {
                    try { raw = arc.Extract(Mathf.Clamp(entryIndex, 0, arc.Count - 1)); }
                    catch (System.Exception ex) { Debug.LogWarning("调色板解压失败: " + ex.Message); }
                }
            }
            var pal = new Color32[256];
            for (int i = 0; i < 256; i++)
            {
                if (raw != null && i * 3 + 2 < raw.Length)
                    pal[i] = new Color32(raw[i * 3 + 1], raw[i * 3 + 2], raw[i * 3], 255);
                else
                    pal[i] = new Color32((byte)i, (byte)i, (byte)i, 255);
            }
            return pal;
        }
    }

    /// <summary>
    /// 从 StreamingAssets/kw/ 读取用户自备的原版数据文件。
    /// 桌面端直接读文件；Android 的 StreamingAssets 在 apk 内，需走 UnityWebRequest。
    /// </summary>
    public static class OriginalAssets
    {
        public static string PathFor(string rel)
        {
            return System.IO.Path.Combine(Application.streamingAssetsPath, rel);
        }

        public static IEnumerator LoadBytes(string rel, System.Action<byte[]> done)
        {
            string p = PathFor(rel);
            if (p.Contains("://") || p.StartsWith("jar:"))
            {
                using (var req = UnityWebRequest.Get(p))
                {
                    yield return req.SendWebRequest();
                    done(req.result == UnityWebRequest.Result.Success ? req.downloadHandler.data : null);
                }
            }
            else
            {
                byte[] bytes = null;
                try
                {
                    if (System.IO.File.Exists(p)) bytes = System.IO.File.ReadAllBytes(p);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning("读取原版数据失败: " + p + " — " + ex.Message);
                }
                done(bytes);
            }
        }
    }

    /// <summary>hexzmap.e5 条目解析：1 字节宽×3、1 字节高×3、然后 w*h 个地形 ID。</summary>
    public static class HexzMap
    {
        public static bool TryParse(byte[] blob, out int w, out int h, out byte[] cells)
        {
            w = 0; h = 0; cells = null;
            if (blob == null || blob.Length < 3) return false;
            w = blob[0] / 3;
            h = blob[1] / 3;
            if (w <= 0 || h <= 0 || 2 + w * h > blob.Length) return false;
            cells = new byte[w * h];
            System.Array.Copy(blob, 2, cells, 0, w * h);
            return true;
        }
    }

    /// <summary>
    /// 原版地形 ID → 本工程地形类型的映射。
    /// 依据：对 hexzmap 与 Hm 战场图的对照观察（0=平原类、4=山体环带、
    /// 20/22/24=营帐建筑、14/23=栅栏等）。多数 ID 语义仍待校准，未知值一律按平地处理。
    /// </summary>
    public static class TerrainIdMap
    {
        public static TerrainType ToTerrain(byte id)
        {
            switch (id)
            {
                case 4: return TerrainType.Hill;      // 山体（观察确认）
                case 3:
                case 5:
                case 7: return TerrainType.Forest;    // 推测：树木/植被
                case 13: return TerrainType.Water;    // 推测：水面
                case 14:
                case 23: return TerrainType.Forest;   // 栅栏/工事 → 以障碍地形近似
                case 20:
                case 22:
                case 24: return TerrainType.Village;  // 营帐/建筑
                default: return TerrainType.Plain;
            }
        }
    }
}
