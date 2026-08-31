using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KwCursor
{
    /// <summary>战斗界面：回合横幅、单位信息、指令菜单、策略列表、对话与结算面板。</summary>
    public class BattleHUD : MonoBehaviour
    {
        BattleManager mgr;
        Canvas canvas;

        Text turnLabel;
        Text banner;
        RectTransform infoPanel;
        Text infoTitle;
        Text infoBody;
        Image portrait;
        Image equipmentIcon;
        RectTransform menuPanel;
        Button btnMove;
        Button btnAttack;
        Button btnSkill;
        Button btnWait;
        Button btnCancel;
        RectTransform skillPanel;
        RectTransform dialogPanel;
        Text dialogName;
        Text dialogBody;
        Button dialogAdvance;
        RectTransform endPanel;
        Text endTitle;
        UnityEngine.UI.Button endButton;
        Text endButtonLabel;
        System.Action endAction;
        Image minimap;

        List<string[]> dialogLines;
        int dialogIndex;
        System.Action dialogDone;

        public void Build(BattleManager manager)
        {
            mgr = manager;
            UIFactory.EnsureEventSystem();
            canvas = UIFactory.CreateCanvas(transform);
            Transform c = canvas.transform;

            // 左上：回合信息
            var turnBg = UIFactory.CreatePanel(c, "TurnBg", new Color(0f, 0f, 0f, 0.55f));
            UIFactory.Place(turnBg, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -20f), new Vector2(360f, 64f));
            turnLabel = UIFactory.CreateText(turnBg, "TurnLabel", "第 1 回合 我军行动", 32, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Stretch((RectTransform)turnLabel.transform, new Vector2(10f, 5f), new Vector2(10f, 5f));

            // 右上：原版小地图（有数据时显示）
            var miniBg = UIFactory.CreatePanel(c, "MinimapBg", new Color(0f, 0f, 0f, 0.55f));
            UIFactory.Place(miniBg, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -20f), new Vector2(232f, 232f));
            var miniGo = new GameObject("Minimap");
            miniGo.transform.SetParent(miniBg, false);
            minimap = miniGo.AddComponent<Image>();
            minimap.preserveAspect = true;
            minimap.raycastTarget = false;
            UIFactory.Stretch((RectTransform)miniGo.transform, new Vector2(8f, 8f), new Vector2(8f, 8f));
            miniBg.gameObject.SetActive(false);

            // 中央横幅
            banner = UIFactory.CreateText(c, "Banner", "", 84, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Place((RectTransform)banner.transform, new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200f, 140f));
            banner.gameObject.SetActive(false);

            // 左下：单位信息（含原版头像位）
            infoPanel = UIFactory.CreatePanel(c, "InfoPanel", new Color(0.05f, 0.07f, 0.10f, 0.88f));
            UIFactory.Place(infoPanel, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(20f, 20f), new Vector2(650f, 270f));
            infoTitle = UIFactory.CreateText(infoPanel, "InfoTitle", "", 36, TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.Place((RectTransform)infoTitle.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -16f), new Vector2(600f, 48f));

            var portraitGo = new GameObject("Portrait");
            portraitGo.transform.SetParent(infoPanel, false);
            portrait = portraitGo.AddComponent<Image>();
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            UIFactory.Place((RectTransform)portraitGo.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -70f), new Vector2(128f, 160f));
            portraitGo.SetActive(false);

            infoBody = UIFactory.CreateText(infoPanel, "InfoBody", "", 28, TextAnchor.UpperLeft);
            UIFactory.Place((RectTransform)infoBody.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(168f, -70f), new Vector2(390f, 190f));

            var itemGo = new GameObject("EquipmentIcon");
            itemGo.transform.SetParent(infoPanel, false);
            equipmentIcon = itemGo.AddComponent<Image>();
            equipmentIcon.preserveAspect = true;
            equipmentIcon.raycastTarget = false;
            UIFactory.Place((RectTransform)itemGo.transform, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-22f, 24f), new Vector2(72f, 72f));
            itemGo.SetActive(false);
            infoPanel.gameObject.SetActive(false);

            // 右下：指令菜单
            menuPanel = UIFactory.CreatePanel(c, "MenuPanel", new Color(0.05f, 0.07f, 0.10f, 0.88f));
            UIFactory.Place(menuPanel, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-20f, 20f), new Vector2(280f, 420f));
            btnMove = MakeMenuButton("移动", 0, () => mgr.OnMovePressed());
            btnAttack = MakeMenuButton("攻击", 1, () => mgr.OnAttackPressed());
            btnSkill = MakeMenuButton("策略", 2, () => mgr.OnSkillPressed());
            btnWait = MakeMenuButton("待机", 3, () => mgr.OnWaitPressed());
            btnCancel = MakeMenuButton("取消", 4, () => mgr.OnCancelPressed());
            menuPanel.gameObject.SetActive(false);

            // 策略列表
            skillPanel = UIFactory.CreatePanel(c, "SkillPanel", new Color(0.05f, 0.07f, 0.10f, 0.92f));
            UIFactory.Place(skillPanel, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-320f, 20f), new Vector2(340f, 420f));
            skillPanel.gameObject.SetActive(false);

            // 对话框
            dialogPanel = UIFactory.CreatePanel(c, "DialogPanel", new Color(0f, 0f, 0f, 0.0f));
            UIFactory.Stretch(dialogPanel, Vector2.zero, Vector2.zero);
            var dlgBg = UIFactory.CreatePanel(dialogPanel, "DlgBg", new Color(0.04f, 0.05f, 0.09f, 0.92f));
            UIFactory.Place(dlgBg, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(1500f, 280f));
            dialogName = UIFactory.CreateText(dlgBg, "DlgName", "", 34, TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.Place((RectTransform)dialogName.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -20f), new Vector2(600f, 44f));
            dialogName.color = new Color(1f, 0.85f, 0.4f);
            dialogBody = UIFactory.CreateText(dlgBg, "DlgBody", "", 32, TextAnchor.UpperLeft);
            UIFactory.Place((RectTransform)dialogBody.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -76f), new Vector2(1430f, 180f));
            var hint = UIFactory.CreateText(dlgBg, "DlgHint", "点击继续 ▼", 24, TextAnchor.LowerRight);
            UIFactory.Place((RectTransform)hint.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-30f, 16f), new Vector2(240f, 32f));
            hint.color = new Color(1f, 1f, 1f, 0.6f);
            // 全屏点击推进
            var catchGo = new GameObject("DlgCatch");
            catchGo.transform.SetParent(dialogPanel, false);
            var catchImg = catchGo.AddComponent<Image>();
            catchImg.color = new Color(0f, 0f, 0f, 0.003f);
            UIFactory.Stretch((RectTransform)catchGo.transform, Vector2.zero, Vector2.zero);
            dialogAdvance = catchGo.AddComponent<Button>();
            dialogAdvance.targetGraphic = catchImg;
            dialogAdvance.transition = Selectable.Transition.None;
            dialogAdvance.onClick.AddListener(AdvanceDialog);
            dialogPanel.gameObject.SetActive(false);

            // 结算面板
            endPanel = UIFactory.CreatePanel(c, "EndPanel", new Color(0f, 0f, 0f, 0.72f));
            UIFactory.Stretch(endPanel, Vector2.zero, Vector2.zero);
            endTitle = UIFactory.CreateText(endPanel, "EndTitle", "", 110, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Place((RectTransform)endTitle.transform, new Vector2(0.5f, 0.60f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200f, 160f));
            endButton = UIFactory.CreateButton(endPanel, "EndButton", "重新开始", 40, () => { if (endAction != null) endAction(); });
            UIFactory.Place((RectTransform)endButton.transform, new Vector2(0.5f, 0.38f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(480f, 96f));
            endButtonLabel = endButton.GetComponentInChildren<Text>();
            endPanel.gameObject.SetActive(false);
        }

        Button MakeMenuButton(string label, int index, UnityEngine.Events.UnityAction onClick)
        {
            var btn = UIFactory.CreateButton(menuPanel, "Btn" + label, label, 34, onClick);
            var rt = (RectTransform)btn.transform;
            UIFactory.Place(rt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -16f - index * 80f), new Vector2(240f, 68f));
            return btn;
        }

        public void ShowMinimap(Sprite sprite)
        {
            minimap.sprite = sprite;
            minimap.transform.parent.gameObject.SetActive(sprite != null);
        }

        // ---------- 回合与横幅 ----------

        public void SetTurnLabel(int turn, bool playerPhase)
        {
            turnLabel.text = "第 " + turn + " 回合  " + (playerPhase ? "我军行动" : "敌军行动");
        }

        public IEnumerator ShowBanner(string text, Color color, float stay = 0.9f)
        {
            banner.text = text;
            banner.color = color;
            banner.gameObject.SetActive(true);
            float t = 0f;
            while (t < stay)
            {
                t += Time.deltaTime;
                yield return null;
            }
            banner.gameObject.SetActive(false);
        }

        // ---------- 单位信息 ----------

        public void ShowUnitInfo(UnitView u, TerrainType terrain)
        {
            if (u == null)
            {
                infoPanel.gameObject.SetActive(false);
                return;
            }
            infoPanel.gameObject.SetActive(true);
            UnitData d = u.Data;

            Sprite face = FaceLoader.Get(d.faceId);
            portrait.sprite = face;
            portrait.gameObject.SetActive(face != null);

            ItemDef item = ItemLoader.GetItem(d.itemId);
            Sprite itemSprite = ItemLoader.GetIcon(d.itemId);
            equipmentIcon.sprite = itemSprite;
            equipmentIcon.gameObject.SetActive(itemSprite != null);

            infoTitle.text = d.name + "  Lv." + d.level + "  [" + d.ClassName + "]" + (d.isEnemy ? "（敌）" : "");
            int defBonus = Mathf.RoundToInt(TerrainTable.DefenseBonus(terrain) * 100f);
            infoBody.text =
                "HP " + d.hp + "/" + d.maxHp + "    MP " + d.mp + "/" + d.maxMp + "\n" +
                "攻击 " + d.atk + "    防御 " + d.def + "    精神 " + d.mind + "\n" +
                "移动 " + d.Move + "    射程 " + d.MinRange + "-" + d.MaxRange + "\n" +
                "地形 " + TerrainTable.DisplayName(terrain) + (defBonus > 0 ? "（防御 +" + defBonus + "%）" : "") + "\n" +
                "装备 " + (item != null ? item.name : "无");
        }

        // ---------- 指令菜单 ----------

        public void ShowActionMenu(bool canMove, bool canAttack, bool canSkill)
        {
            menuPanel.gameObject.SetActive(true);
            skillPanel.gameObject.SetActive(false);
            btnMove.interactable = canMove;
            btnAttack.interactable = canAttack;
            btnSkill.interactable = canSkill;
        }

        public void HideActionMenu()
        {
            menuPanel.gameObject.SetActive(false);
            skillPanel.gameObject.SetActive(false);
        }

        public void ShowSkillList(List<Skill> skills, int curMp, System.Action<int> onPick)
        {
            skillPanel.gameObject.SetActive(true);
            for (int i = skillPanel.childCount - 1; i >= 0; i--)
                Destroy(skillPanel.GetChild(i).gameObject);

            var title = UIFactory.CreateText(skillPanel, "SkTitle", "策略（MP " + curMp + "）", 30, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Place((RectTransform)title.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(300f, 40f));

            for (int i = 0; i < skills.Count; i++)
            {
                int idx = i;
                Skill s = skills[i];
                string label = s.name + "  MP" + s.mpCost + " 射程" + s.range;
                var btn = UIFactory.CreateButton(skillPanel, "Sk" + i, label, 28, () => onPick(idx));
                btn.interactable = curMp >= s.mpCost;
                UIFactory.Place((RectTransform)btn.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -66f - i * 80f), new Vector2(300f, 68f));
            }
        }

        public void HideSkillList()
        {
            skillPanel.gameObject.SetActive(false);
        }

        // ---------- 对话 ----------

        public void ShowDialog(List<string[]> lines, System.Action onDone)
        {
            dialogLines = lines;
            dialogIndex = 0;
            dialogDone = onDone;
            dialogPanel.gameObject.SetActive(true);
            RenderDialogLine();
        }

        void RenderDialogLine()
        {
            string[] line = dialogLines[dialogIndex];
            dialogName.text = line[0];
            dialogBody.text = line[1];
        }

        void AdvanceDialog()
        {
            dialogIndex++;
            if (dialogLines != null && dialogIndex < dialogLines.Count)
            {
                RenderDialogLine();
                return;
            }
            dialogPanel.gameObject.SetActive(false);
            var cb = dialogDone;
            dialogDone = null;
            if (cb != null) cb();
        }

        // ---------- 结算 ----------

        public void ShowEnd(bool victory, string buttonLabel, System.Action onButton)
        {
            endPanel.gameObject.SetActive(true);
            endTitle.text = victory ? "胜  利" : "败  北";
            endTitle.color = victory ? new Color(1f, 0.85f, 0.3f) : new Color(0.9f, 0.3f, 0.25f);
            if (endButtonLabel != null) endButtonLabel.text = buttonLabel;
            endAction = onButton;
        }
    }
}
