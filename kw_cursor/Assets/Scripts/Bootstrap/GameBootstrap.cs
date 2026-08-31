using UnityEngine;

namespace KwCursor
{
    /// <summary>
    /// 运行时自举：进入 Play（或真机启动）后自动搭建整个战斗场景，
    /// 因此工程不依赖任何手工摆放的场景对象，空场景即可运行。
    /// </summary>
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoStart()
        {
            if (Object.FindObjectOfType<BattleManager>() != null) return;
            Build();
        }

        public static void Build()
        {
            Application.targetFrameRate = 60;
            if (!Application.isEditor)
            {
                Screen.orientation = ScreenOrientation.LandscapeLeft;
                Screen.sleepTimeout = SleepTimeout.NeverSleep;
            }

            var root = new GameObject("GameRoot");
            AudioManager.Ensure(root.transform);
            root.AddComponent<BattleManager>(); // 关卡由 Campaign + 原版数据在 Start 中构建
        }

        public static void Rebuild()
        {
            var mgr = Object.FindObjectOfType<BattleManager>();
            if (mgr != null) Object.DestroyImmediate(mgr.gameObject);
            Build();
        }
    }
}
