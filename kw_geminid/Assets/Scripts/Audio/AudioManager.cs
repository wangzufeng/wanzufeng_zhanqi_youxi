using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace KwGeminid
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager I { get; private set; }

        AudioSource bgmSource;
        AudioSource sfxSource;
        readonly Dictionary<string, AudioClip> sfxCache = new Dictionary<string, AudioClip>();

        void Awake()
        {
            if (I == null) { I = this; DontDestroyOnLoad(gameObject); }
            else { Destroy(gameObject); return; }

            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.volume = 0.5f;

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.volume = 0.7f;
        }

        public void PlayBgm(AudioClip clip)
        {
            if (clip == null || bgmSource == null) return;
            if (bgmSource.clip == clip && bgmSource.isPlaying) return;
            bgmSource.clip = clip;
            bgmSource.Play();
        }

        public void PlayChapterBgm(int chapterIndex)
        {
            // 尝试读取 StreamingAssets/kw/SoundTrk/ 目录下的 mp3
            string bgmDir = OriginalAssets.StreamingPath("kw/SoundTrk");
            if (Directory.Exists(bgmDir))
            {
                string[] files = Directory.GetFiles(bgmDir, "*.mp3");
                if (files.Length > 0)
                {
                    int idx = chapterIndex % files.Length;
                    StartCoroutine(LoadAndPlayBgm(files[idx]));
                }
            }
        }

        System.Collections.IEnumerator LoadAndPlayBgm(string path)
        {
            using (var uwr = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip("file://" + path, AudioType.MPEG))
            {
                yield return uwr.SendWebRequest();
                if (uwr.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    PlayBgm(UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(uwr));
                }
            }
        }

        public void PlaySfx(string sfxName)
        {
            if (sfxSource == null || string.IsNullOrEmpty(sfxName)) return;
            string sfxPath = OriginalAssets.StreamingPath("kw/" + sfxName);
            if (!File.Exists(sfxPath)) return;

            AudioClip clip;
            if (!sfxCache.TryGetValue(sfxName, out clip))
            {
                StartCoroutine(LoadAndPlaySfx(sfxPath, sfxName));
            }
            else
            {
                sfxSource.PlayOneShot(clip);
            }
        }

        System.Collections.IEnumerator LoadAndPlaySfx(string path, string name)
        {
            using (var uwr = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip("file://" + path, AudioType.WAV))
            {
                yield return uwr.SendWebRequest();
                if (uwr.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    var clip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(uwr);
                    sfxCache[name] = clip;
                    sfxSource.PlayOneShot(clip);
                }
            }
        }
    }
}
