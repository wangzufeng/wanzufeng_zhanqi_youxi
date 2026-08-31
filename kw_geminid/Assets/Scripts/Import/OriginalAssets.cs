using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace KwGeminid
{
    public static class OriginalAssets
    {
        public static string StreamingPath(string relativePath)
        {
            return Path.Combine(Application.streamingAssetsPath, relativePath);
        }

        public static IEnumerator LoadBytes(string relativePath, Action<byte[]> onDone)
        {
            string p = StreamingPath(relativePath);
            if (!File.Exists(p))
            {
                // 尝试同级 kw 目录
                string fallback = Path.Combine(Application.dataPath, "../../kw", Path.GetFileName(relativePath));
                if (File.Exists(fallback)) p = fallback;
            }

            if (p.Contains("://") || p.Contains(":///"))
            {
                using (var uwr = UnityWebRequest.Get(p))
                {
                    yield return uwr.SendWebRequest();
                    if (uwr.result == UnityWebRequest.Result.Success)
                    {
                        onDone(uwr.downloadHandler.data);
                    }
                    else
                    {
                        onDone(null);
                    }
                }
            }
            else
            {
                if (File.Exists(p))
                {
                    byte[] data = null;
                    try { data = File.ReadAllBytes(p); } catch { }
                    onDone(data);
                }
                else
                {
                    onDone(null);
                }
                yield break;
            }
        }
    }
}
