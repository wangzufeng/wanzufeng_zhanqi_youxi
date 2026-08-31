using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KwCursor
{
    /// <summary>
    /// 敌方 AI：
    /// 1. 若警戒范围内没有我方单位则原地待机（复现原作“靠近才动”的守株行为）；
    /// 2. 优先寻找“移动后可攻击”的位置，选血量最少的目标；
    /// 3. 否则朝最近的我方单位推进。
    /// </summary>
    public static class EnemyAI
    {
        const int AggroExtra = 3; // 警戒范围 = 移动力 + 射程 + AggroExtra

        public static IEnumerator Act(BattleManager mgr, UnitView self)
        {
            UnitView nearest = FindNearest(mgr.Players, self.Pos);
            if (nearest == null) yield break;

            int aggro = self.Data.Move + self.Data.MaxRange + AggroExtra;
            if (self.Data.isLeader) aggro = self.Data.Move + self.Data.MaxRange; // 主将驻守，靠近才动
            if (GridGeometry.Distance(self.Pos, nearest.Pos) > aggro)
                yield break; // 未进入警戒范围，原地待机

            Dictionary<Hex, Hex> parents;
            Dictionary<Hex, int> field = mgr.Grid.MoveField(self, out parents);
            List<Hex> dests = mgr.Grid.Destinations(self, field);

            // 1) 找“站上去就能打到人”的格子
            Hex bestCell = self.Pos;
            UnitView bestTarget = null;
            int bestScore = int.MaxValue;
            foreach (Hex cell in dests)
            {
                foreach (UnitView p in mgr.Players)
                {
                    if (p == null || !p.Data.Alive) continue;
                    int d = GridGeometry.Distance(cell, p.Pos);
                    if (d < self.Data.MinRange || d > self.Data.MaxRange) continue;
                    // 评分：目标血量优先，其次路径耗费
                    int score = p.Data.hp * 100 + field[cell];
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestCell = cell;
                        bestTarget = p;
                    }
                }
            }

            if (bestTarget != null)
            {
                if (bestCell != self.Pos)
                    yield return MoveRoutine(mgr, self, bestCell, parents);
                yield return mgr.PerformAttack(self, bestTarget);
                yield break;
            }

            // 2) 无法攻击：向最近目标推进
            Hex approach = self.Pos;
            int bestDist = int.MaxValue;
            foreach (Hex cell in dests)
            {
                int d = GridGeometry.Distance(cell, nearest.Pos);
                if (d < bestDist || (d == bestDist && field[cell] < field[approach]))
                {
                    bestDist = d;
                    approach = cell;
                }
            }
            if (approach != self.Pos)
                yield return MoveRoutine(mgr, self, approach, parents);
        }

        static IEnumerator MoveRoutine(BattleManager mgr, UnitView self, Hex dest, Dictionary<Hex, Hex> parents)
        {
            List<Hex> path = mgr.Grid.BuildPath(self.Pos, dest, parents);
            mgr.Grid.RemoveUnit(self);
            yield return self.MoveAlong(path);
            mgr.Grid.PlaceUnit(self, dest);
        }

        static UnitView FindNearest(List<UnitView> list, Hex from)
        {
            UnitView best = null;
            int bestD = int.MaxValue;
            foreach (UnitView u in list)
            {
                if (u == null || !u.Data.Alive) continue;
                int d = GridGeometry.Distance(from, u.Pos);
                if (d < bestD)
                {
                    bestD = d;
                    best = u;
                }
            }
            return best;
        }
    }
}
