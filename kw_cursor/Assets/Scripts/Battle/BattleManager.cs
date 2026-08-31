using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KwCursor
{
    public enum BattlePhase
    {
        Intro,
        PlayerIdle,       // 等待选中我方单位
        UnitMenu,         // 指令菜单
        SelectMove,       // 选择移动目标格
        SelectAttack,     // 选择攻击目标
        SelectSkillTarget,// 选择策略目标
        Animating,        // 播放动画中
        EnemyTurn,
        Finished
    }

    /// <summary>战斗主流程：状态机、选中与指令、经验升级、章节流转。</summary>
    public class BattleManager : MonoBehaviour
    {
        public HexGrid Grid { get; private set; }
        public GridView View { get; private set; }
        public BattleHUD Hud { get; private set; }
        public TouchCameraControls CamCtrl { get; private set; }

        public List<UnitView> Players = new List<UnitView>();
        public List<UnitView> Enemies = new List<UnitView>();

        ScenarioData scenario;
        BattlePhase phase = BattlePhase.Intro;
        int turn = 1;

        UnitView selected;
        Dictionary<Hex, int> moveField;
        Dictionary<Hex, Hex> moveParents;
        List<Hex> moveDests;
        List<UnitView> attackTargets = new List<UnitView>();
        List<UnitView> skillTargets = new List<UnitView>();
        Skill pendingSkill;
        Hex preMovePos;
        bool movedThisSelection;

        void Start()
        {
            StartCoroutine(InitRoutine());
        }

        IEnumerator InitRoutine()
        {
            GridGeometry.Mode = GridMode.Hex;

            // 原版数据（用户自备，位于 StreamingAssets/kw/）：武将/策略/文本/头像/地图画面
            yield return OfficerTable.TryLoad();
            yield return StrategyTable.TryLoad();
            yield return ImsgLoader.TryLoad();
            yield return ItemLoader.TryLoad();
            yield return FaceLoader.TryLoad();
            yield return UnitSpriteLoader.TryLoad();

            int chapter = Campaign.LoadProgress();
            scenario = Campaign.Build(chapter);
            yield return RealMapLoader.TryApply(scenario);

            BuildWorld();

            if (AudioManager.I != null) AudioManager.I.PlayChapterBgm(chapter);
        }

        void BuildWorld()
        {
            Grid = scenario.realCells != null
                ? HexGrid.FromCells(scenario.realW, scenario.realH, scenario.realCells)
                : HexGrid.FromRows(scenario.mapRows);
            View = new GridView(Grid, transform, scenario.artSprite);

            foreach (UnitSpawn spawn in scenario.units)
            {
                var go = new GameObject(spawn.data.name);
                go.transform.SetParent(transform, false);
                var uv = go.AddComponent<UnitView>();
                uv.Init(spawn.data);
                Grid.PlaceUnit(uv, GridGeometry.FromColRow(spawn.col, spawn.row));
                if (spawn.data.isEnemy) Enemies.Add(uv); else Players.Add(uv);
            }

            SetupCamera();

            Hud = gameObject.AddComponent<BattleHUD>();
            Hud.Build(this);
            Hud.SetTurnLabel(turn, true);
            Hud.ShowMinimap(scenario.minimapSprite);

            phase = BattlePhase.Intro;
            Hud.ShowDialog(scenario.introLines, () => StartCoroutine(BeginPlayerTurn()));
        }

        void SetupCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
            }
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0.08f, 0.11f);

            Bounds b = View.WorldBounds();
            cam.transform.position = new Vector3(b.center.x, b.center.y, -10f);

            CamCtrl = cam.GetComponent<TouchCameraControls>();
            if (CamCtrl == null) CamCtrl = cam.gameObject.AddComponent<TouchCameraControls>();
            CamCtrl.MinBounds = new Vector2(b.min.x, b.min.y);
            CamCtrl.MaxBounds = new Vector2(b.max.x, b.max.y);
            CamCtrl.MaxZoom = Mathf.Max(9f, b.extents.y + 1.5f);
            CamCtrl.OnWorldTap = HandleWorldTap;
        }

        // ==================== 输入 ====================

        void HandleWorldTap(Vector3 worldPos)
        {
            Hex h = GridGeometry.FromWorld(worldPos);
            if (!Grid.InBounds(h)) return;

            switch (phase)
            {
                case BattlePhase.PlayerIdle: TapIdle(h); break;
                case BattlePhase.SelectMove: TapMoveTarget(h); break;
                case BattlePhase.SelectAttack: TapAttackTarget(h); break;
                case BattlePhase.SelectSkillTarget: TapSkillTarget(h); break;
                case BattlePhase.UnitMenu: break; // 菜单期间点地图无效，用取消退出
                default: break;
            }
        }

        void TapIdle(Hex h)
        {
            UnitView u = Grid.GetUnit(h);
            Hud.ShowUnitInfo(u, Grid.GetTerrain(h));
            if (u == null || u.Data.isEnemy || u.Acted) return;

            if (AudioManager.I != null) AudioManager.I.Play("select");
            selected = u;
            movedThisSelection = false;
            preMovePos = u.Pos;
            OpenUnitMenu();
        }

        void OpenUnitMenu()
        {
            phase = BattlePhase.UnitMenu;
            Hud.ShowUnitInfo(selected, Grid.GetTerrain(selected.Pos));
            attackTargets = Grid.TargetsFrom(selected.Pos, selected, selected.Data.MinRange, selected.Data.MaxRange);
            bool canSkill = HasUsableSkill(selected);
            Hud.ShowActionMenu(!movedThisSelection, attackTargets.Count > 0, canSkill);
            View.ClearHighlights();
        }

        bool HasUsableSkill(UnitView u)
        {
            foreach (Skill s in u.Data.skills)
            {
                if (u.Data.mp < s.mpCost) continue;
                if (s.kind == SkillKind.Damage &&
                    Grid.TargetsFrom(u.Pos, u, 1, s.range).Count > 0) return true;
                if (s.kind == SkillKind.Heal) return true;
            }
            return false;
        }

        public void OnMovePressed()
        {
            if (phase != BattlePhase.UnitMenu || selected == null) return;
            moveField = Grid.MoveField(selected, out moveParents);
            moveDests = Grid.Destinations(selected, moveField);
            View.ShowHighlights(moveDests, GridView.MoveColor);
            Hud.HideActionMenu();
            phase = BattlePhase.SelectMove;
        }

        void TapMoveTarget(Hex h)
        {
            if (moveDests != null && moveDests.Contains(h) && h != selected.Pos)
            {
                List<Hex> path = Grid.BuildPath(selected.Pos, h, moveParents);
                StartCoroutine(DoMove(path));
            }
            else if (h == selected.Pos)
            {
                View.ClearHighlights();
                OpenUnitMenu();
            }
            else
            {
                CancelSelection();
            }
        }

        IEnumerator DoMove(List<Hex> path)
        {
            phase = BattlePhase.Animating;
            View.ClearHighlights();
            Grid.RemoveUnit(selected);
            yield return StartCoroutine(selected.MoveAlong(path));
            Grid.PlaceUnit(selected, path[path.Count - 1]);
            movedThisSelection = true;
            OpenUnitMenu();
        }

        public void OnAttackPressed()
        {
            if (phase != BattlePhase.UnitMenu || selected == null) return;
            attackTargets = Grid.TargetsFrom(selected.Pos, selected, selected.Data.MinRange, selected.Data.MaxRange);
            if (attackTargets.Count == 0) return;
            var cells = new List<Hex>();
            foreach (UnitView t in attackTargets) cells.Add(t.Pos);
            View.ShowHighlights(cells, GridView.AttackColor);
            Hud.HideActionMenu();
            phase = BattlePhase.SelectAttack;
        }

        void TapAttackTarget(Hex h)
        {
            UnitView target = Grid.GetUnit(h);
            if (target != null && attackTargets.Contains(target))
            {
                StartCoroutine(DoPlayerAttack(target));
            }
            else
            {
                OpenUnitMenu();
            }
        }

        IEnumerator DoPlayerAttack(UnitView target)
        {
            phase = BattlePhase.Animating;
            View.ClearHighlights();
            yield return StartCoroutine(PerformAttack(selected, target));
            FinishAction();
        }

        public void OnSkillPressed()
        {
            if (phase != BattlePhase.UnitMenu || selected == null) return;
            Hud.ShowSkillList(selected.Data.skills, selected.Data.mp, PickSkill);
        }

        void PickSkill(int index)
        {
            if (selected == null || index < 0 || index >= selected.Data.skills.Count) return;
            Skill s = selected.Data.skills[index];
            if (selected.Data.mp < s.mpCost) return;

            pendingSkill = s;
            skillTargets.Clear();
            var cells = new List<Hex>();
            if (s.kind == SkillKind.Damage)
            {
                foreach (UnitView t in Grid.TargetsFrom(selected.Pos, selected, 1, s.range))
                {
                    skillTargets.Add(t);
                    cells.Add(t.Pos);
                }
                if (skillTargets.Count == 0) return;
                View.ShowHighlights(cells, GridView.SkillColor);
            }
            else
            {
                foreach (UnitView t in Grid.AlliesFrom(selected.Pos, selected, s.range))
                {
                    if (t.Data.hp >= t.Data.maxHp) continue;
                    skillTargets.Add(t);
                    cells.Add(t.Pos);
                }
                if (skillTargets.Count == 0) return;
                View.ShowHighlights(cells, GridView.HealColor);
            }
            Hud.HideActionMenu();
            phase = BattlePhase.SelectSkillTarget;
        }

        void TapSkillTarget(Hex h)
        {
            UnitView target = Grid.GetUnit(h);
            if (target != null && skillTargets.Contains(target))
            {
                StartCoroutine(DoSkill(target));
            }
            else
            {
                OpenUnitMenu();
            }
        }

        IEnumerator DoSkill(UnitView target)
        {
            phase = BattlePhase.Animating;
            View.ClearHighlights();
            Skill s = pendingSkill;
            selected.Data.mp -= s.mpCost;

            if (s.kind == SkillKind.Damage)
            {
                if (AudioManager.I != null) AudioManager.I.Play("fire");
                yield return StartCoroutine(selected.PlayAction(new[] { 4, 5, 6, 5, 4 }, 0.07f));
                int dmg = DamageCalc.SkillDamage(selected.Data, target.Data,
                    s, TerrainTable.DefenseBonus(Grid.GetTerrain(target.Pos)));
                yield return StartCoroutine(target.PlayAction(new[] { 3 }, 0.12f));
                target.ApplyDamage(dmg);
                PopupText.Spawn(target.transform.position, s.name + " " + dmg, new Color(1f, 0.55f, 0.2f));
                yield return new WaitForSeconds(0.55f);
                if (!target.Data.Alive) yield return StartCoroutine(KillUnit(target, selected));
            }
            else
            {
                if (AudioManager.I != null) AudioManager.I.Play("heal");
                yield return StartCoroutine(selected.PlayAction(new[] { 6, 7, 8, 7, 6 }, 0.07f));
                int heal = DamageCalc.SkillHeal(selected.Data, s);
                target.ApplyHeal(heal);
                PopupText.Spawn(target.transform.position, "+" + heal, new Color(0.35f, 0.95f, 0.45f));
                yield return new WaitForSeconds(0.55f);
            }

            if (phase != BattlePhase.Finished) FinishAction();
        }

        public void OnWaitPressed()
        {
            if (phase != BattlePhase.UnitMenu || selected == null) return;
            FinishAction();
        }

        public void OnCancelPressed()
        {
            if (selected == null) return;
            if (movedThisSelection)
            {
                Grid.MoveUnitInstant(selected, preMovePos);
                movedThisSelection = false;
            }
            CancelSelection();
        }

        void CancelSelection()
        {
            selected = null;
            View.ClearHighlights();
            Hud.HideActionMenu();
            Hud.ShowUnitInfo(null, TerrainType.Plain);
            if (phase != BattlePhase.Finished) phase = BattlePhase.PlayerIdle;
        }

        void FinishAction()
        {
            if (selected != null) selected.SetActed(true);
            selected = null;
            View.ClearHighlights();
            Hud.HideActionMenu();
            if (phase == BattlePhase.Finished) return;
            phase = BattlePhase.PlayerIdle;

            bool allActed = true;
            foreach (UnitView u in Players)
                if (u != null && u.Data.Alive && !u.Acted) { allActed = false; break; }
            if (allActed) StartCoroutine(EnemyTurnRoutine());
        }

        // ==================== 战斗结算 ====================

        /// <summary>执行一次攻击（含反击、经验与死亡处理），双方通用。</summary>
        public IEnumerator PerformAttack(UnitView attacker, UnitView defender)
        {
            if (AudioManager.I != null) AudioManager.I.Play("attack");
            yield return StartCoroutine(attacker.PlayAction(new[] { 4, 5, 6, 5, 4 }, 0.065f));
            int dmg = DamageCalc.Physical(attacker.Data, defender.Data,
                TerrainTable.DefenseBonus(Grid.GetTerrain(defender.Pos)));
            yield return StartCoroutine(defender.PlayAction(new[] { 3 }, 0.11f));
            defender.ApplyDamage(dmg);
            if (AudioManager.I != null) AudioManager.I.Play("hit");
            PopupText.Spawn(defender.transform.position, dmg.ToString(), new Color(1f, 0.35f, 0.3f));
            yield return new WaitForSeconds(0.5f);

            if (!defender.Data.Alive)
            {
                yield return StartCoroutine(KillUnit(defender, attacker));
                yield break;
            }

            // 反击（若在防守方射程内）
            int dist = GridGeometry.Distance(attacker.Pos, defender.Pos);
            if (dist >= defender.Data.MinRange && dist <= defender.Data.MaxRange)
            {
                yield return StartCoroutine(defender.PlayAction(new[] { 4, 5, 4 }, 0.065f));
                int back = Mathf.Max(1, Mathf.RoundToInt(
                    DamageCalc.Physical(defender.Data, attacker.Data,
                        TerrainTable.DefenseBonus(Grid.GetTerrain(attacker.Pos))) * 0.75f));
                yield return StartCoroutine(attacker.PlayAction(new[] { 3 }, 0.11f));
                attacker.ApplyDamage(back);
                PopupText.Spawn(attacker.transform.position, "反击 " + back, new Color(1f, 0.8f, 0.3f));
                yield return new WaitForSeconds(0.5f);
                if (!attacker.Data.Alive)
                    yield return StartCoroutine(KillUnit(attacker, defender));
            }
        }

        IEnumerator KillUnit(UnitView u, UnitView killer)
        {
            if (AudioManager.I != null) AudioManager.I.Play("die");
            Grid.RemoveUnit(u);
            Players.Remove(u);
            Enemies.Remove(u);
            PopupText.Spawn(u.transform.position, u.Data.name + " 阵亡", new Color(0.9f, 0.9f, 0.9f));
            GrantExp(killer, u);
            yield return StartCoroutine(u.DieRoutine());
            CheckBattleEnd(u);
        }

        void GrantExp(UnitView killer, UnitView victim)
        {
            if (killer == null || killer.Data.isEnemy || !killer.Data.Alive) return;
            int gain = 25 + Mathf.Max(0, (victim.Data.level - killer.Data.level) * 8) + (victim.Data.isLeader ? 30 : 0);
            killer.Data.exp += gain;
            PopupText.Spawn(killer.transform.position + Vector3.up * 0.25f, "EXP +" + gain, new Color(0.6f, 0.9f, 1f));
            while (killer.Data.exp >= UnitData.ExpPerLevel)
            {
                killer.Data.exp -= UnitData.ExpPerLevel;
                killer.Data.LevelUp();
                killer.UpdateHpBar();
                PopupText.Spawn(killer.transform.position + Vector3.up * 0.55f, "升级！Lv." + killer.Data.level, new Color(1f, 0.9f, 0.3f));
            }
        }

        void CheckBattleEnd(UnitView died)
        {
            if (phase == BattlePhase.Finished) return;

            bool playerLeaderDead = died != null && !died.Data.isEnemy && died.Data.isLeader;
            bool enemyLeaderDead = died != null && died.Data.isEnemy && died.Data.isLeader;

            if (playerLeaderDead || Players.Count == 0)
            {
                phase = BattlePhase.Finished;
                StopAllCoroutines();
                Hud.ShowEnd(false, "重试本章", RetryChapter);
                return;
            }
            if (enemyLeaderDead || Enemies.Count == 0)
            {
                phase = BattlePhase.Finished;
                StopAllCoroutines();
                OnVictory();
            }
        }

        void OnVictory()
        {
            int chapter = scenario.chapter;
            bool hasNext = chapter + 1 < Campaign.ChapterMaps.Length;
            System.Action proceed = () =>
            {
                if (hasNext)
                {
                    Campaign.SaveProgress(chapter + 1);
                    Hud.ShowEnd(true, "进入第 " + (chapter + 2) + " 章", GameBootstrap.Rebuild);
                }
                else
                {
                    Campaign.SaveProgress(0);
                    Hud.ShowEnd(true, "通关！重新开始", GameBootstrap.Rebuild);
                }
            };
            if (scenario.outroLines != null && scenario.outroLines.Count > 0)
                Hud.ShowDialog(scenario.outroLines, proceed);
            else
                proceed();
        }

        void RetryChapter()
        {
            GameBootstrap.Rebuild();
        }

        // ==================== 回合流转 ====================

        IEnumerator BeginPlayerTurn()
        {
            phase = BattlePhase.Animating;
            Hud.SetTurnLabel(turn, true);
            yield return StartCoroutine(Hud.ShowBanner("第 " + turn + " 回合  我军行动", new Color(0.5f, 0.75f, 1f)));
            foreach (UnitView u in Players) if (u != null) u.SetActed(false);
            phase = BattlePhase.PlayerIdle;
        }

        IEnumerator EnemyTurnRoutine()
        {
            phase = BattlePhase.EnemyTurn;
            Hud.SetTurnLabel(turn, false);
            Hud.ShowUnitInfo(null, TerrainType.Plain);
            yield return StartCoroutine(Hud.ShowBanner("敌军行动", new Color(1f, 0.45f, 0.4f)));

            var acting = new List<UnitView>(Enemies);
            foreach (UnitView e in acting)
            {
                if (e == null || !e.Data.Alive) continue;
                if (phase == BattlePhase.Finished) yield break;
                yield return StartCoroutine(CamCtrl.PanTo(e.transform.position));
                yield return StartCoroutine(EnemyAI.Act(this, e));
                yield return new WaitForSeconds(0.15f);
            }

            if (phase == BattlePhase.Finished) yield break;
            turn++;
            yield return StartCoroutine(BeginPlayerTurn());
        }

        public void OnRestartPressed()
        {
            GameBootstrap.Rebuild();
        }
    }
}
