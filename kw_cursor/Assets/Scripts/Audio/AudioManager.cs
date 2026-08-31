using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace KwCursor
{
    /// <summary>
    /// 音频：BGM 使用原版 SoundTrk 的 MP3（按章节选曲），音效使用原版 Se??.wav。
    /// 文件由用户通过编辑器菜单复制到 StreamingAssets；缺失时静音，不影响游戏。
    /// 音效编号与场合的对应为待校准的默认映射。
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager I { get; private set; }

        AudioSource bgm;
        AudioSource sfx;
        readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();

        // 场合 → 音效文件（待校准）
        static readonly Dictionary<string, string> SfxMap = new Dictionary<string, string>
        {
            { "attack", "kw/Se01.wav" },
            { "hit", "kw/Se02.wav" },
            { "die", "kw/Se04.wav" },
            { "heal", "kw/Se06.wav" },
            { "fire", "kw/Se07.wav" },
            { "select", "kw/Se00.wav" },
        };

        public static AudioManager Ensure(Transform parent)
        {
            if (I != null) return I;
            var go = new GameObject("AudioManager");
            go.transform.SetParent(parent, false);
            I = go.AddComponent<AudioManager>();
            I.bgm = go.AddComponent<AudioSource>();
            I.bgm.loop = true;
            I.bgm.volume = 0.55f;
            I.sfx = go.AddComponent<AudioSource>();
            I.sfx.volume = 0.9f;
            return I;
        }

        void OnDestroy()
        {
            if (I == this) I = null;
        }

        public void PlayChapterBgm(int chapter)
        {
            int track = 2 + (chapter % 8); // SoundTrk 从 02 开始
            string name = string.Format("kw/SoundTrk/{0:D2}-AudioTrack {0:D2}.mp3", track);
            StartCoroutine(LoadAndPlayBgm(name));
        }

        IEnumerator LoadAndPlayBgm(string rel)
        {
            AudioClip clip = null;
            yield return LoadClip(rel, AudioType.MPEG, c => clip = c);
            if (clip != null)
            {
                bgm.clip = clip;
                bgm.Play();
            }
        }

        public void Play(string key)
        {
            string rel;
            if (!SfxMap.TryGetValue(key, out rel)) return;
            AudioClip cached;
            if (cache.TryGetValue(rel, out cached))
            {
                if (cached != null) sfx.PlayOneShot(cached);
                return;
            }
            StartCoroutine(LoadAndPlaySfx(rel));
        }

        IEnumerator LoadAndPlaySfx(string rel)
        {
            AudioClip clip = null;
            yield return LoadClip(rel, AudioType.WAV, c => clip = c);
            cache[rel] = clip; // 失败也记录，避免反复请求
            if (clip != null) sfx.PlayOneShot(clip);
        }

        IEnumerator LoadClip(string rel, AudioType type, System.Action<AudioClip> done)
        {
            string p = OriginalAssets.PathFor(rel);
            string url = p.Contains("://") ? p : "file://" + p;
            url = url.Replace(" ", "%20");
            using (var req = UnityWebRequestMultimedia.GetAudioClip(url, type))
            {
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                    done(DownloadHandlerAudioClip.GetContent(req));
                else
                    done(null);
            }
        }
    }
}
