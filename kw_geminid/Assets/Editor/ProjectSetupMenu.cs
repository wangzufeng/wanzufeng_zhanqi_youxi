#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KwGeminid.Editor
{
    public static class ProjectSetupMenu
    {
        const string ScenePath = "Assets/Scenes/Battle.unity";

        [MenuItem("战棋复刻/1. 生成并保存战斗场景", false, 1)]
        public static void GenerateAndSaveScene()
        {
            string dir = Path.GetDirectoryName(ScenePath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0.08f, 0.11f);
            cam.transform.position = new Vector3(0, 0, -10);
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<TouchCameraControls>();

            var root = new GameObject("BattleRoot");
            root.AddComponent<AudioManager>();
            root.AddComponent<BattleManager>();

            new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));

            EditorSceneManager.SaveScene(scene, ScenePath);

            var list = new EditorBuildSettingsScene[] { new EditorBuildSettingsScene(ScenePath, true) };
            EditorBuildSettings.scenes = list;

            Debug.Log("战斗场景已生成并保存到: " + ScenePath);
        }

        [MenuItem("战棋复刻/2. 应用移动端 Player 设置", false, 2)]
        public static void ApplyMobilePlayerSettings()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateLandscapeLeft = true;
            PlayerSettings.allowedAutorotateLandscapeRight = true;
            PlayerSettings.allowedAutorotatePortrait = false;
            PlayerSettings.allowedAutorotatePortraitUpsideDown = false;

            PlayerSettings.companyName = "KwGeminid";
            PlayerSettings.productName = "三国志曹操传复刻";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.kw.caocaozhuan");
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, "com.kw.caocaozhuan");

            Debug.Log("移动端 Player 设置已应用：横屏锁定、包名已就绪。");
        }

        [MenuItem("战棋复刻/3. 导入原版数据 (从 kw 目录)", false, 3)]
        public static void ImportOriginalData()
        {
            ImportOriginalDataInternal(true);
        }

        public static void ImportOriginalDataBatch()
        {
            ImportOriginalDataInternal(false);
        }

        static void ImportOriginalDataInternal(bool showDialog)
        {
            string srcDir = Path.Combine(Application.dataPath, "../../kw");
            if (!Directory.Exists(srcDir))
            {
                if (showDialog && !Application.isBatchMode)
                    EditorUtility.DisplayDialog("提示", "未在仓库中找到原版 kw 目录: " + srcDir, "确定");
                return;
            }

            string dstDir = Path.Combine(Application.streamingAssetsPath, "kw");
            if (!Directory.Exists(dstDir)) Directory.CreateDirectory(dstDir);

            string[] copyFiles =
            {
                "hexzmap.e5", "Data.e5", "Face.e5", "Spalet.e5", "Pmpalet.e5",
                "Pmapobj.e5", "Item.e5", "Imsg.e5", "Smlmap.e5", "Gate.e5",
                "Hm00.e5", "Hm01.e5", "Hm02.e5", "Hm03.e5", "Hm04.e5"
            };

            int copied = 0;
            foreach (string file in copyFiles)
            {
                string src = Path.Combine(srcDir, file);
                string dst = Path.Combine(dstDir, file);
                if (File.Exists(src))
                {
                    File.Copy(src, dst, true);
                    copied++;
                }
            }

            // 复制音效
            string[] sfx = Directory.GetFiles(srcDir, "Se*.wav");
            foreach (string s in sfx)
            {
                string dst = Path.Combine(dstDir, Path.GetFileName(s));
                File.Copy(s, dst, true);
                copied++;
            }

            // 复制音乐
            string srcBgm = Path.Combine(srcDir, "SoundTrk");
            string dstBgm = Path.Combine(dstDir, "SoundTrk");
            if (Directory.Exists(srcBgm))
            {
                if (!Directory.Exists(dstBgm)) Directory.CreateDirectory(dstBgm);
                string[] bgmFiles = Directory.GetFiles(srcBgm, "*.mp3");
                foreach (string b in bgmFiles)
                {
                    string dst = Path.Combine(dstBgm, Path.GetFileName(b));
                    File.Copy(b, dst, true);
                    copied++;
                }
            }

            AssetDatabase.Refresh();
            if (showDialog && !Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("完成", string.Format("已将原版 {0} 个素材文件复制到 StreamingAssets/kw/！", copied), "确定");
            }
            Debug.Log(string.Format("已导入原版 {0} 个素材文件到 StreamingAssets/kw/", copied));
        }

        [MenuItem("战棋复刻/4. 自动化打包/Android (APK)", false, 11)]
        public static void BuildAndroidBatch()
        {
            GenerateAndSaveScene();
            ApplyMobilePlayerSettings();
            ImportOriginalDataBatch();

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            string outDir = Path.Combine(Application.dataPath, "../Builds/Android");
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);
            string apkPath = Path.Combine(outDir, "KwCaoCaoZhuan.apk");

            var report = BuildPipeline.BuildPlayer(new[] { ScenePath }, apkPath, BuildTarget.Android, BuildOptions.None);
            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log("Android APK 打包成功: " + apkPath);
            }
            else
            {
                Debug.LogError("Android APK 打包失败: " + report.summary.totalErrors + " 处错误");
            }
        }

        [MenuItem("战棋复刻/4. 自动化打包/Windows (EXE)", false, 12)]
        public static void BuildWindowsBatch()
        {
            GenerateAndSaveScene();
            ImportOriginalDataBatch();

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
            string outDir = Path.Combine(Application.dataPath, "../Builds/Windows");
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);
            string exePath = Path.Combine(outDir, "KwCaoCaoZhuan.exe");

            var report = BuildPipeline.BuildPlayer(new[] { ScenePath }, exePath, BuildTarget.StandaloneWindows64, BuildOptions.None);
            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log("Windows 独立程序打包成功: " + exePath);
            }
            else
            {
                Debug.LogError("Windows 独立程序打包失败: " + report.summary.totalErrors + " 处错误");
            }
        }
    }
}
#endif
