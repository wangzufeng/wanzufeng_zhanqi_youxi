using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KwGeminid
{
    public enum BattlePhase
    {
        Intro,
        PlayerIdle,
        UnitMenu,
        SelectMove,
        SelectAttack,
        SelectSkillTarget,
        Animating,
        Duel,
        EnemyTurn,
        Finished
    }

    public class BattleManager : MonoBehaviour
    {
        public HexGrid Grid { get; private set; }
        public GridView View { get; private set; }
        public BattleHUD Hud { get; private set; }
        public TouchCameraControls CamCtrl { get; private set; }

        public List<UnitView> Players = new List<UnitView>();
        public List<UnitView> Enemies = new List<UnitView>();

        public WeatherType CurrentWeather = WeatherType.Sunny;

        ScenarioData scenario;
        BattlePhase phase = BattlePhase.Intro;
        int turn = 1;

        UnitView selected;
        Dictionary<Hex, int> moveField;
        Dictionary<Hex, Hex> moveParents;
        List<UnitView> attackTargets = new List<UnitView>();
        Hex preMovePos;
        bool movedThisSelection;

        void Start()
        {
            StartCoroutine(InitRoutine());
        }

        IEnumerator InitRoutine()
        {
            GridGeometry.Mode = GridMode.Hex;

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
            UpdateWeather();
            Hud.SetTurnLabel(turn, true, GetWeatherName());
            Hud.ShowMinimap(scenario.minimapSprite);

            phase = BattlePhase.Intro;
            Hud.ShowDialog(scenario.introLines, () => StartCoroutine(BeginPlayerTurn()));
        }

        void UpdateWeather()
        {
            WeatherType[] weathers = { WeatherType.Sunny, WeatherType.Cloudy, WeatherType.Rainy, WeatherType.HeavyRain, WeatherType.Snowy };
            int idx = Mathf.FloorToInt((turn - 1) / 3f) % weathers.Length;
            CurrentWeather = weathers[idx];
        }

        public string GetWeatherName()
        {
            switch (CurrentWeather)
            {
                case WeatherType.Sunny: return "晴天";
                case WeatherType.Cloudy: return "阴天";
                case WeatherType.Rainy: return "雨天";
                case WeatherType.HeavyRain: return "豪雨";
                case WeatherType.Snowy: return "雪天";
                default: return "晴天";
            }
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

        void HandleWorldTap(Vector3 worldPos)
        {
            Hex h = GridGeometry.FromWorld(worldPos);
            if (!Grid.InBounds(h)) return;

            switch (phase)
            {
                case BattlePhase.PlayerIdle: TapIdle(h); break;
                case BattlePhase.SelectMove: TapMoveTarget(h); break;
                case BattlePhase.SelectAttack: TapAttackTarget(h); break;
                default: break;
            }
        }

        void TapIdle(Hex h)
        {
            UnitView u = Grid.GetUnit(h);
            if (u != null)
            {
                Hud.ShowUnitInfo(u, Grid.GetTerrain(h));
                if (!u.Data.isEnemy && !u.HasActed)
                {
                    selected = u;
                    preMovePos = u.Position;
                    movedThisSelection = false;
                    EnterSelectMove();
                }
            }
            else
            {
                Hud.HideUnitInfo();
            }
        }

        void EnterSelectMove()
        {
            phase = BattlePhase.SelectMove;
            moveField = Grid.Reachable(selected, out moveParents);
            View.ClearHighlights();
            View.HighlightCells(moveField.Keys, new Color(0.2f, 0.6f, 1f, 0.45f));
        }

        void TapMoveTarget(Hex h)
        {
            if (moveField == null || !moveField.ContainsKey(h))
            {
                CancelSelection();
                return;
            }
            UnitView occ = Grid.GetUnit(h);
            if (occ != null && occ != selected)
            {
                CancelSelection();
                return;
            }
            StartCoroutine(DoMoveAndOpenMenu(h));
        }

        IEnumerator DoMoveAndOpenMenu(Hex dest)
        {
            phase = BattlePhase.Animating;
            View.ClearHighlights();

            var path = ReconstructPath(dest);
            if (path.Count > 1)
            {
                Grid.RemoveUnit(selected);
                yield return selected.MoveAlong(path);
                Grid.PlaceUnit(selected, dest);
                movedThisSelection = true;
            }

            phase = BattlePhase.UnitMenu;
            Hud.ShowMenu(true);
            Hud.ShowUnitInfo(selected, Grid.GetTerrain(selected.Position));
        }

        List<Hex> ReconstructPath(Hex end)
        {
            var p = new List<Hex>();
            Hex cur = end;
            p.Add(cur);
            while (moveParents != null && moveParents.ContainsKey(cur) && moveParents[cur] != cur)
            {
                cur = moveParents[cur];
                p.Insert(0, cur);
            }
            return p;
        }

        public void OnMenuMove()
        {
            if (movedThisSelection) return;
            Hud.ShowMenu(false);
            EnterSelectMove();
        }

        public void OnMenuAttack()
        {
            Hud.ShowMenu(false);
            phase = BattlePhase.SelectAttack;

            attackTargets.Clear();
            List<Hex> inRange = Grid.AttackRange(selected.Position, selected.Data.MinRange, selected.Data.MaxRange);
            foreach (Hex h in inRange)
            {
                UnitView target = Grid.GetUnit(h);
                if (target != null && target.Data.isEnemy) attackTargets.Add(target);
            }

            if (attackTargets.Count == 0)
            {
                PopupText.Show(selected.transform.position, "范围内无敌军", Color.yellow);
                phase = BattlePhase.UnitMenu;
                Hud.ShowMenu(true);
                return;
            }

            View.ClearHighlights();
            var targetHexes = new List<Hex>();
            foreach (var t in attackTargets) targetHexes.Add(t.Position);
            View.HighlightCells(targetHexes, new Color(1f, 0.2f, 0.2f, 0.5f));
        }

        public void OnMenuSkill()
        {
            OnMenuAttack();
        }

        public void OnMenuWait()
        {
            Hud.ShowMenu(false);
            selected.HasActed = true;
            selected.UpdateVisual();
            CancelSelection();
            CheckTurnOver();
        }

        public void OnMenuCancel()
        {
            Hud.ShowMenu(false);
            if (movedThisSelection && preMovePos != selected.Position)
            {
                Grid.MoveUnit(selected, preMovePos);
            }
            CancelSelection();
        }

        void CancelSelection()
        {
            selected = null;
            moveField = null;
            moveParents = null;
            attackTargets.Clear();
            View.ClearHighlights();
            Hud.ShowMenu(false);
            phase = BattlePhase.PlayerIdle;
        }

        void TapAttackTarget(Hex h)
        {
            UnitView target = Grid.GetUnit(h);
            if (target != null && attackTargets.Contains(target))
            {
                if (selected.Data.isLeader && target.Data.isLeader && turn <= 3)
                {
                    StartCoroutine(DoDuel(selected, target));
                }
                else
                {
                    StartCoroutine(DoAttack(selected, target));
                }
            }
            else
            {
                CancelSelection();
            }
        }

        IEnumerator DoDuel(UnitView att, UnitView def)
        {
            phase = BattlePhase.Duel;
            View.ClearHighlights();

            bool finished = false;
            Hud.ShowDialog(new List<string[]>
            {
                new[] { att.Data.name, "来将通名！可敢与我一决胜负？！" },
                new[] { def.Data.name, "休得张狂！看刀！" },
                new[] { "单挑特写", "⚔️ 两马交错，刀光剑影！" + att.Data.name + " 斩落敌酋！" }
            }, () => finished = true);

            while (!finished) yield return null;

            def.Data.TakeDamage(999);
            def.UpdateVisual();
            PopupText.Show(def.transform.position, "单挑斩杀!", Color.yellow);
            yield return HandleUnitKilled(att, def);

            att.HasActed = true;
            att.UpdateVisual();
            CancelSelection();
            CheckTurnOver();
        }

        IEnumerator DoAttack(UnitView att, UnitView def)
        {
            phase = BattlePhase.Animating;
            View.ClearHighlights();

            yield return att.PlayActionAnim();
            float terrainDef = TerrainTable.DefenseBonus(Grid.GetTerrain(def.Position));
            bool isCrit;
            int dmg = DamageCalc.Physical(att.Data, def.Data, terrainDef, out isCrit);
            def.Data.TakeDamage(dmg);
            def.UpdateVisual();

            string hitText = (isCrit ? "⚡暴击 -" : "-") + dmg;
            PopupText.Show(def.transform.position, hitText, isCrit ? Color.yellow : Color.red);
            yield return def.PlayHurtAnim();

            if (!def.Data.Alive)
            {
                yield return HandleUnitKilled(att, def);

                // 方天画戟引导奋战
                if (att.Data.passive == TreasurePassive.CleaveAttack)
                {
                    UnitView cleaveTarget = null;
                    for (int dir = 0; dir < att.Position.NeighborCount; dir++)
                    {
                        Hex nh = att.Position.Neighbor(dir);
                        UnitView candidate = Grid.GetUnit(nh);
                        if (candidate != null && candidate.Data.isEnemy && candidate.Data.Alive)
                        {
                            cleaveTarget = candidate;
                            break;
                        }
                    }
                    if (cleaveTarget != null)
                    {
                        PopupText.Show(att.transform.position, "【引导奋战】!", Color.yellow);
                        yield return new WaitForSeconds(0.3f);
                        yield return DoAttack(att, cleaveTarget);
                        yield break;
                    }
                }
            }
            else
            {
                // 反击判定 (青龙偃月刀封杀反击)
                if (att.Data.passive != TreasurePassive.NoCounter)
                {
                    int dist = att.Position.Distance(def.Position);
                    if (dist >= def.Data.MinRange && dist <= def.Data.MaxRange)
                    {
                        yield return new WaitForSeconds(0.2f);
                        yield return def.PlayActionAnim();
                        float attTerrain = TerrainTable.DefenseBonus(Grid.GetTerrain(att.Position));
                        bool counterCrit;
                        int cdmg = Mathf.RoundToInt(DamageCalc.Physical(def.Data, att.Data, attTerrain, out counterCrit) * 0.75f);
                        att.Data.TakeDamage(cdmg);
                        att.UpdateVisual();
                        PopupText.Show(att.transform.position, "-" + cdmg, Color.red);
                        yield return att.PlayHurtAnim();

                        if (!att.Data.Alive) yield return HandleUnitKilled(def, att);
                    }
                }
            }

            att.HasActed = true;
            att.UpdateVisual();
            CancelSelection();
            CheckTurnOver();
        }

        IEnumerator HandleUnitKilled(UnitView killer, UnitView victim)
        {
            Grid.RemoveUnit(victim);
            if (victim.Data.isEnemy) Enemies.Remove(victim);
            else Players.Remove(victim);

            yield return victim.FadeOut(0.4f);
            Destroy(victim.gameObject);

            if (!killer.Data.isEnemy)
            {
                int expGain = (killer.Data.passive == TreasurePassive.ExpBonus50) ? 45 : 30;
                killer.Data.exp += expGain;
                if (killer.Data.exp >= UnitData.ExpPerLevel)
                {
                    killer.Data.exp -= UnitData.ExpPerLevel;
                    killer.Data.LevelUp();
                    killer.UpdateVisual();
                    PopupText.Show(killer.transform.position, "升级!", Color.yellow);
                }
            }

            CheckGameOverCondition();
        }

        void CheckGameOverCondition()
        {
            bool playerLeaderAlive = false;
            foreach (var p in Players) if (p.Data.isLeader) playerLeaderAlive = true;

            if (!playerLeaderAlive)
            {
                phase = BattlePhase.Finished;
                Hud.ShowDialog(new List<string[]> { new[] { "系统", "主帅阵亡，战斗失败……" } }, () =>
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene(
                        UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
                });
                return;
            }

            bool enemyLeaderAlive = false;
            foreach (var e in Enemies) if (e.Data.isLeader) enemyLeaderAlive = true;

            if (!enemyLeaderAlive || Enemies.Count == 0)
            {
                phase = BattlePhase.Finished;
                int cur = Campaign.LoadProgress();
                Campaign.SaveProgress(cur + 1);

                Hud.ShowDialog(scenario.outroLines, () =>
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene(
                        UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
                });
            }
        }

        void CheckTurnOver()
        {
            if (phase == BattlePhase.Finished) return;
            bool allActed = true;
            foreach (var p in Players)
                if (!p.HasActed) { allActed = false; break; }

            if (allActed) StartCoroutine(BeginEnemyTurn());
        }

        public void OnPlayerEndTurnClicked()
        {
            if (phase == BattlePhase.PlayerIdle || phase == BattlePhase.SelectMove)
            {
                CancelSelection();
                StartCoroutine(BeginEnemyTurn());
            }
        }

        IEnumerator BeginPlayerTurn()
        {
            phase = BattlePhase.PlayerIdle;
            UpdateWeather();
            Hud.SetTurnLabel(turn, true, GetWeatherName());
            foreach (var p in Players)
            {
                p.HasActed = false;
                p.Data.OnRoundStart();
                p.UpdateVisual();
            }
            yield return null;
        }

        IEnumerator BeginEnemyTurn()
        {
            phase = BattlePhase.EnemyTurn;
            Hud.SetTurnLabel(turn, false, GetWeatherName());

            yield return new WaitForSeconds(0.4f);

            var list = new List<UnitView>(Enemies);
            foreach (var e in list)
            {
                if (e == null || !e.Data.Alive) continue;
                yield return EnemyAI.Execute(this, e);
                yield return new WaitForSeconds(0.2f);
                if (phase == BattlePhase.Finished) yield break;
            }

            turn++;
            StartCoroutine(BeginPlayerTurn());
        }
    }
}
