using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KwGeminid
{
    public static class EnemyAI
    {
        public static IEnumerator Execute(BattleManager bm, UnitView ai)
        {
            if (ai == null || !ai.Data.Alive) yield break;

            Dictionary<Hex, Hex> parents;
            Dictionary<Hex, int> reachable = bm.Grid.Reachable(ai, out parents);

            UnitView bestTarget = null;
            Hex bestAttackFrom = ai.Position;
            int lowestHp = int.MaxValue;

            foreach (var kvp in reachable)
            {
                Hex candidatePos = kvp.Key;
                UnitView occ = bm.Grid.GetUnit(candidatePos);
                if (occ != null && occ != ai) continue;

                List<Hex> attackRange = bm.Grid.AttackRange(candidatePos, ai.Data.MinRange, ai.Data.MaxRange);
                foreach (Hex th in attackRange)
                {
                    UnitView target = bm.Grid.GetUnit(th);
                    if (target != null && !target.Data.isEnemy && target.Data.Alive)
                    {
                        if (target.Data.hp < lowestHp)
                        {
                            lowestHp = target.Data.hp;
                            bestTarget = target;
                            bestAttackFrom = candidatePos;
                        }
                    }
                }
            }

            if (bestTarget != null)
            {
                if (bestAttackFrom != ai.Position)
                {
                    var path = ReconstructPath(parents, bestAttackFrom);
                    bm.Grid.RemoveUnit(ai);
                    yield return ai.MoveAlong(path);
                    bm.Grid.PlaceUnit(ai, bestAttackFrom);
                }

                yield return ai.PlayActionAnim();
                float terrainDef = TerrainTable.DefenseBonus(bm.Grid.GetTerrain(bestTarget.Position));
                int dmg = DamageCalc.Physical(ai.Data, bestTarget.Data, terrainDef);
                bestTarget.Data.hp = Mathf.Max(0, bestTarget.Data.hp - dmg);
                bestTarget.UpdateVisual();
                PopupText.Show(bestTarget.transform.position, "-" + dmg, Color.red);
                yield return bestTarget.PlayHurtAnim();

                if (!bestTarget.Data.Alive)
                {
                    bm.Grid.RemoveUnit(bestTarget);
                    bm.Players.Remove(bestTarget);
                    yield return bestTarget.FadeOut(0.4f);
                    Object.Destroy(bestTarget.gameObject);
                }
            }
            else
            {
                UnitView closestPlayer = null;
                int minDist = int.MaxValue;
                foreach (var p in bm.Players)
                {
                    if (!p.Data.Alive) continue;
                    int d = ai.Position.Distance(p.Position);
                    if (d < minDist) { minDist = d; closestPlayer = p; }
                }

                if (closestPlayer != null)
                {
                    Hex bestStep = ai.Position;
                    int minStepDist = int.MaxValue;
                    foreach (var kvp in reachable)
                    {
                        Hex h = kvp.Key;
                        UnitView occ = bm.Grid.GetUnit(h);
                        if (occ != null && occ != ai) continue;

                        int d = h.Distance(closestPlayer.Position);
                        if (d < minStepDist)
                        {
                            minStepDist = d;
                            bestStep = h;
                        }
                    }

                    if (bestStep != ai.Position)
                    {
                        var path = ReconstructPath(parents, bestStep);
                        bm.Grid.RemoveUnit(ai);
                        yield return ai.MoveAlong(path);
                        bm.Grid.PlaceUnit(ai, bestStep);
                    }
                }
            }

            ai.HasActed = true;
            ai.UpdateVisual();
        }

        static List<Hex> ReconstructPath(Dictionary<Hex, Hex> parents, Hex end)
        {
            var p = new List<Hex>();
            Hex cur = end;
            p.Add(cur);
            while (parents != null && parents.ContainsKey(cur) && parents[cur] != cur)
            {
                cur = parents[cur];
                p.Insert(0, cur);
            }
            return p;
        }
    }
}
