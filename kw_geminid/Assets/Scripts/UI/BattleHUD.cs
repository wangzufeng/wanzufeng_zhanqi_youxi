using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KwGeminid
{
    public class BattleHUD : MonoBehaviour
    {
        Canvas canvas;
        Text turnLabel;
        GameObject menuPanel;
        GameObject infoPanel;
        Text infoText;
        Image faceImage;
        Image itemIcon;
        Text itemText;
        GameObject dialogPanel;
        Text dialogSpeaker;
        Text dialogContent;
        Image minimapImage;

        BattleManager battle;
        List<string[]> activeDialog;
        int dialogIndex;
        Action onDialogFinish;

        public void Build(BattleManager bm)
        {
            battle = bm;
            var cgo = new GameObject("BattleCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = cgo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = cgo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            BuildTopBar(canvas.transform);
            BuildMenuPanel(canvas.transform);
            BuildInfoPanel(canvas.transform);
            BuildMinimap(canvas.transform);
            BuildDialog(canvas.transform);
        }

        void BuildTopBar(Transform root)
        {
            var p = UIFactory.CreatePanel(root, "TopBar", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 70), new Vector2(0, -35), new Color(0.08f, 0.1f, 0.15f, 0.9f));

            var tgo = new GameObject("TurnLabel", typeof(RectTransform), typeof(Text));
            tgo.transform.SetParent(p.transform, false);
            var rt = tgo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0.5f, 1);
            rt.offsetMin = new Vector2(30, 0);
            rt.offsetMax = Vector2.zero;

            turnLabel = tgo.GetComponent<Text>();
            turnLabel.font = UIFactory.DefaultFont();
            turnLabel.fontSize = 26;
            turnLabel.color = new Color(0.9f, 0.8f, 0.4f);
            turnLabel.alignment = TextAnchor.MiddleLeft;

            var endBtn = UIFactory.CreateButton(p.transform, "结束回合", new Vector2(160, 50), () => battle.OnPlayerEndTurnClicked());
            var brt = endBtn.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(1, 0.5f);
            brt.anchorMax = new Vector2(1, 0.5f);
            brt.anchoredPosition = new Vector2(-110, 0);
        }

        void BuildMenuPanel(Transform root)
        {
            menuPanel = UIFactory.CreatePanel(root, "ActionMenu", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(580, 80), new Vector2(0, 60), new Color(0.08f, 0.1f, 0.15f, 0.95f));

            var hlg = menuPanel.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 15;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            UIFactory.CreateButton(menuPanel.transform, "移动", new Vector2(100, 56), () => battle.OnMenuMove());
            UIFactory.CreateButton(menuPanel.transform, "攻击", new Vector2(100, 56), () => battle.OnMenuAttack());
            UIFactory.CreateButton(menuPanel.transform, "策略", new Vector2(100, 56), () => battle.OnMenuSkill());
            UIFactory.CreateButton(menuPanel.transform, "待机", new Vector2(100, 56), () => battle.OnMenuWait());
            UIFactory.CreateButton(menuPanel.transform, "取消", new Vector2(100, 56), () => battle.OnMenuCancel());

            menuPanel.SetActive(false);
        }

        void BuildInfoPanel(Transform root)
        {
            infoPanel = UIFactory.CreatePanel(root, "UnitInfo", new Vector2(0, 0), new Vector2(0, 0), new Vector2(420, 180), new Vector2(230, 110), new Color(0.08f, 0.1f, 0.15f, 0.95f));

            var faceGo = new GameObject("Face", typeof(RectTransform), typeof(Image));
            faceGo.transform.SetParent(infoPanel.transform, false);
            var frt = faceGo.GetComponent<RectTransform>();
            frt.anchorMin = new Vector2(0, 0.5f);
            frt.anchorMax = new Vector2(0, 0.5f);
            frt.anchoredPosition = new Vector2(65, 0);
            frt.sizeDelta = new Vector2(90, 115);
            faceImage = faceGo.GetComponent<Image>();

            var tgo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            tgo.transform.SetParent(infoPanel.transform, false);
            var trt = tgo.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0, 0);
            trt.anchorMax = new Vector2(1, 1);
            trt.offsetMin = new Vector2(130, 10);
            trt.offsetMax = new Vector2(-10, -10);

            infoText = tgo.GetComponent<Text>();
            infoText.font = UIFactory.DefaultFont();
            infoText.fontSize = 20;
            infoText.color = Color.white;
            infoText.alignment = TextAnchor.MiddleLeft;

            infoPanel.SetActive(false);
        }

        void BuildMinimap(Transform root)
        {
            var p = UIFactory.CreatePanel(root, "Minimap", new Vector2(1, 1), new Vector2(1, 1), new Vector2(180, 180), new Vector2(-100, -140), new Color(0, 0, 0, 0.8f));
            minimapImage = p.GetComponent<Image>();
            p.SetActive(false);
        }

        void BuildDialog(Transform root)
        {
            dialogPanel = UIFactory.CreatePanel(root, "Dialog", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(900, 180), new Vector2(0, 110), new Color(0.05f, 0.07f, 0.12f, 0.95f));

            var spkGo = new GameObject("Speaker", typeof(RectTransform), typeof(Text));
            spkGo.transform.SetParent(dialogPanel.transform, false);
            var srt = spkGo.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0, 1);
            srt.anchorMax = new Vector2(0, 1);
            srt.anchoredPosition = new Vector2(120, -25);
            srt.sizeDelta = new Vector2(200, 35);
            dialogSpeaker = spkGo.GetComponent<Text>();
            dialogSpeaker.font = UIFactory.DefaultFont();
            dialogSpeaker.fontSize = 24;
            dialogSpeaker.color = new Color(0.95f, 0.8f, 0.35f);

            var cntGo = new GameObject("Content", typeof(RectTransform), typeof(Text));
            cntGo.transform.SetParent(dialogPanel.transform, false);
            var crt = cntGo.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0, 0);
            crt.anchorMax = new Vector2(1, 1);
            crt.offsetMin = new Vector2(30, 20);
            crt.offsetMax = new Vector2(-30, -55);
            dialogContent = cntGo.GetComponent<Text>();
            dialogContent.font = UIFactory.DefaultFont();
            dialogContent.fontSize = 22;
            dialogContent.color = Color.white;

            var nextBtn = UIFactory.CreateButton(dialogPanel.transform, "继续 ▼", new Vector2(120, 40), () => AdvanceDialog());
            var nrt = nextBtn.GetComponent<RectTransform>();
            nrt.anchorMin = new Vector2(1, 0);
            nrt.anchorMax = new Vector2(1, 0);
            nrt.anchoredPosition = new Vector2(-80, 30);

            dialogPanel.SetActive(false);
        }

        public void SetTurnLabel(int turn, bool isPlayer, string weather = "晴天")
        {
            if (turnLabel != null)
            {
                turnLabel.text = string.Format("第 {0} 回合 · {1} · 🌤️ {2}", turn, isPlayer ? "我军行动" : "敌军行动", weather);
            }
        }

        public void ShowMenu(bool show)
        {
            if (menuPanel != null) menuPanel.SetActive(show);
        }

        public void ShowUnitInfo(UnitView u, TerrainType terrain)
        {
            if (u == null) { HideUnitInfo(); return; }
            infoPanel.SetActive(true);

            Sprite face = (u.Data.faceId >= 0) ? FaceLoader.Get(u.Data.faceId) : null;
            if (face != null)
            {
                faceImage.gameObject.SetActive(true);
                faceImage.sprite = face;
            }
            else
            {
                faceImage.gameObject.SetActive(false);
            }

            infoText.text = string.Format(
                "<b>{0}</b> (Lv.{1} {2})\nHP: {3}/{4}  MP: {5}/{6}\n攻: {7}  防: {8}  神: {9}\n地形: {10} (+{11}%)\n装备: Lv.{12} 武器 / Lv.{13} 防具",
                u.Data.name, u.Data.level, u.Data.ClassName,
                u.Data.hp, u.Data.maxHp, u.Data.mp, u.Data.maxMp,
                u.Data.atk, u.Data.def, u.Data.mind,
                TerrainTable.DisplayName(terrain), (int)(TerrainTable.DefenseBonus(terrain) * 100),
                u.Data.weaponLevel, u.Data.armorLevel
            );
        }

        public void HideUnitInfo()
        {
            if (infoPanel != null) infoPanel.SetActive(false);
        }

        public void ShowMinimap(Sprite s)
        {
            if (minimapImage == null) return;
            if (s != null)
            {
                minimapImage.sprite = s;
                minimapImage.gameObject.SetActive(true);
            }
            else
            {
                minimapImage.gameObject.SetActive(false);
            }
        }

        public void ShowDialog(List<string[]> lines, Action onFinish)
        {
            if (lines == null || lines.Count == 0)
            {
                if (onFinish != null) onFinish();
                return;
            }
            activeDialog = lines;
            dialogIndex = 0;
            onDialogFinish = onFinish;
            dialogPanel.SetActive(true);
            RenderDialogLine();
        }

        void AdvanceDialog()
        {
            dialogIndex++;
            if (dialogIndex >= activeDialog.Count)
            {
                dialogPanel.SetActive(false);
                if (onDialogFinish != null) onDialogFinish();
            }
            else
            {
                RenderDialogLine();
            }
        }

        void RenderDialogLine()
        {
            var line = activeDialog[dialogIndex];
            dialogSpeaker.text = line.Length > 0 ? line[0] : "";
            dialogContent.text = line.Length > 1 ? line[1] : "";
        }
    }
}
