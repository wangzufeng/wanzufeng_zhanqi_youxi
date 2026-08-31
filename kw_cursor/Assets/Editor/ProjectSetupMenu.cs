using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KwCursor.EditorTools
{
    /// <summary>编辑器辅助菜单：生成场景、应用移动端 Player 设置。</summary>
    public static class ProjectSetupMenu
    {
        [MenuItem("战棋复刻/1. 生成并保存战斗场景")]
        public static void CreateBattleScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            System.IO.Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Battle.unity");
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/Battle.unity", true)
            };
            EditorUtility.DisplayDialog("完成",
                "已生成 Assets/Scenes/Battle.unity 并加入 Build Settings。\n直接点击 Play 即可开始战斗。", "好的");
        }

        [MenuItem("战棋复刻/3. 导入原版数据 (仓库 kw → StreamingAssets)")]
        public static void ImportOriginalData()
        {
            string projRoot = System.IO.Path.GetDirectoryName(Application.dataPath); // kw_cursor/
            string repoRoot = System.IO.Path.GetDirectoryName(projRoot);             // 仓库根目录
            string srcDir = System.IO.Path.Combine(repoRoot, "kw");
            string dstDir = System.IO.Path.Combine(Application.dataPath, "StreamingAssets/kw");

            var wanted = new System.Collections.Generic.List<string>
            {
                "hexzmap.e5", "Data.e5", "Spalet.e5", "Face.e5", "Imsg.e5",
                "Pmpalet.e5", "Smlmap.e5", "Pmapobj.e5", "Item.e5"
            };
            // 战役各章的战场画面
            for (int i = 0; i <= 4; i++) wanted.Add("Hm" + i.ToString("D2") + ".e5");
            // 常用音效
            for (int i = 0; i <= 9; i++) wanted.Add("Se" + i.ToString("D2") + ".wav");
            // 各章 BGM（文件名含空格，按原名复制）
            for (int t = 2; t <= 6; t++)
                wanted.Add("SoundTrk/" + t.ToString("D2") + "-AudioTrack " + t.ToString("D2") + ".mp3");

            int copied = 0;
            long bytes = 0;
            System.IO.Directory.CreateDirectory(dstDir);
            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(dstDir, "SoundTrk"));
            foreach (string name in wanted)
            {
                string src = System.IO.Path.Combine(srcDir, name);
                if (!System.IO.File.Exists(src)) continue;
                string dst = System.IO.Path.Combine(dstDir, name);
                System.IO.File.Copy(src, dst, true);
                copied++;
                bytes += new System.IO.FileInfo(src).Length;
            }
            AssetDatabase.Refresh();

            if (copied > 0)
                EditorUtility.DisplayDialog("完成",
                    "已复制 " + copied + " 个原版数据文件（约 " + (bytes / 1024 / 1024) + " MB）到 StreamingAssets/kw/。\n" +
                    "运行时将自动加载：真实战场地形 + 原版画面与调色板 + 武将表 + 音乐音效。\n" +
                    "该目录已被 .gitignore 排除，原版数据不会提交进仓库。", "好的");
            else
                EditorUtility.DisplayDialog("未找到数据",
                    "在 " + srcDir + " 下没有找到 hexzmap.e5 等文件。\n" +
                    "请确认本工程位于游戏仓库内（kw 与 kw_cursor 为同级目录）。", "好的");
        }

        [MenuItem("战棋复刻/2. 应用移动端 Player 设置")]
        public static void ApplyMobileSettings()
        {
            PlayerSettings.productName = "战棋复刻Demo";
            PlayerSettings.companyName = "kw_cursor";

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.kwcursor.zhanqi");
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, "com.kwcursor.zhanqi");

            EditorUtility.DisplayDialog("完成",
                "已设置横屏、产品名与 Android/iOS 包名。\n构建前请在 Build Settings 中切换目标平台。", "好的");
        }
    }
}
