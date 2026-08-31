using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KwCursor
{
    /// <summary>棋盘上的单位表现：占位棋子、名字单字、血条、移动动画。</summary>
    public class UnitView : MonoBehaviour
    {
        public UnitData Data;
        public Hex Pos;
        public bool Acted;

        SpriteRenderer body;
        Color baseColor;
        Transform hpFill;
        SpriteRenderer hpFillSr;
        TextMesh label;
        bool hasOriginalSprite;

        static readonly Color PlayerColor = new Color(0.18f, 0.44f, 0.85f);
        static readonly Color EnemyColor = new Color(0.77f, 0.23f, 0.18f);
        static readonly Color LeaderRing = new Color(0.95f, 0.80f, 0.25f);

        public void Init(UnitData data)
        {
            Data = data;
            baseColor = data.isEnemy ? EnemyColor : PlayerColor;

            Sprite unitSprite = UnitSpriteLoader.Get(data.spriteId);
            hasOriginalSprite = unitSprite != null;
            float barY = 0.55f;

            // 阵营脚圈（有小人图时更重要，用于区分敌我）
            var ring = new GameObject("FactionRing");
            ring.transform.SetParent(transform, false);
            ring.transform.localPosition = new Vector3(0f, unitSprite != null ? -0.06f : 0f, 0f);
            var rsr = ring.AddComponent<SpriteRenderer>();
            Color rc = data.isLeader ? LeaderRing : baseColor;
            rsr.sprite = SpriteFactory.Circle(new Color(rc.r, rc.g, rc.b, 0.35f), rc,
                unitSprite != null ? 0.30f : 0.48f);
            rsr.sortingOrder = 4;
            if (!data.isLeader && unitSprite == null) ring.SetActive(false);
            if (unitSprite != null) ring.transform.localScale = new Vector3(1f, 0.55f, 1f); // 椭圆脚圈

            var bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(transform, false);
            body = bodyGo.AddComponent<SpriteRenderer>();
            if (unitSprite != null)
            {
                // 原版小人图：脚部对齐格子中心
                body.sprite = unitSprite;
                bodyGo.transform.localPosition = new Vector3(0f, 0.34f, -0.05f);
                barY = 0.85f;
            }
            else
            {
                body.sprite = SpriteFactory.Circle(baseColor, Color.black, 0.42f);
            }
            body.sortingOrder = 5;

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(transform, false);
            labelGo.transform.localPosition = new Vector3(0f, 0f, -0.2f);
            label = labelGo.AddComponent<TextMesh>();
            label.text = data.shortLabel;
            label.fontSize = 48;
            label.characterSize = 0.022f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = Color.white;
            label.fontStyle = FontStyle.Bold;
            Font f = UIFactory.DefaultFont();
            if (f != null)
            {
                label.font = f;
                var mr = labelGo.GetComponent<MeshRenderer>();
                mr.sharedMaterial = f.material;
                mr.sortingOrder = 10;
            }
            if (unitSprite != null) labelGo.SetActive(false); // 有小人图时不显示单字

            var hpBg = new GameObject("HpBg");
            hpBg.transform.SetParent(transform, false);
            hpBg.transform.localPosition = new Vector3(-0.5f, barY, -0.15f);
            var bgSr = hpBg.AddComponent<SpriteRenderer>();
            bgSr.sprite = SpriteFactory.Bar();
            bgSr.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);
            bgSr.sortingOrder = 8;

            var hpGo = new GameObject("HpFill");
            hpGo.transform.SetParent(transform, false);
            hpGo.transform.localPosition = new Vector3(-0.5f, barY, -0.16f);
            hpFillSr = hpGo.AddComponent<SpriteRenderer>();
            hpFillSr.sprite = SpriteFactory.Bar();
            hpFillSr.sortingOrder = 9;
            hpFill = hpGo.transform;
            UpdateHpBar();
        }

        public void SetActed(bool v)
        {
            Acted = v;
            Color normal = hasOriginalSprite ? Color.white : baseColor;
            body.color = v ? Color.Lerp(normal, Color.gray, 0.60f) : normal;
        }

        public void ApplyDamage(int dmg)
        {
            Data.hp = Mathf.Max(0, Data.hp - dmg);
            UpdateHpBar();
        }

        public void ApplyHeal(int amount)
        {
            Data.hp = Mathf.Min(Data.maxHp, Data.hp + amount);
            UpdateHpBar();
        }

        public void UpdateHpBar()
        {
            float f = Data.maxHp > 0 ? (float)Data.hp / Data.maxHp : 0f;
            hpFill.localScale = new Vector3(Mathf.Clamp01(f), 1f, 1f);
            if (f > 0.5f) hpFillSr.color = new Color(0.25f, 0.85f, 0.30f);
            else if (f > 0.25f) hpFillSr.color = new Color(0.95f, 0.80f, 0.20f);
            else hpFillSr.color = new Color(0.95f, 0.25f, 0.20f);
        }

        public IEnumerator MoveAlong(List<Hex> path, float stepTime = 0.14f)
        {
            for (int i = 1; i < path.Count; i++)
            {
                Vector3 a = GridGeometry.ToWorld(path[i - 1]);
                Vector3 b = GridGeometry.ToWorld(path[i]);
                if (hasOriginalSprite && Mathf.Abs(b.x - a.x) > 0.01f)
                    body.flipX = b.x < a.x;
                float t = 0f;
                while (t < 1f)
                {
                    t += Time.deltaTime / stepTime;
                    transform.position = Vector3.Lerp(a, b, Mathf.Clamp01(t));
                    if (hasOriginalSprite)
                    {
                        int walkFrame = Mathf.Clamp(Mathf.FloorToInt(t * 3f), 0, 2);
                        Sprite frame = UnitSpriteLoader.GetFrame(Data.spriteId, walkFrame);
                        if (frame != null) body.sprite = frame;
                    }
                    yield return null;
                }
            }
            if (hasOriginalSprite)
            {
                Sprite idle = UnitSpriteLoader.GetFrame(Data.spriteId, 0);
                if (idle != null) body.sprite = idle;
            }
        }

        /// <summary>
        /// 播放原版动作姿势。空帧由加载器回退为站立；没有原版形象时做缩放反馈。
        /// 3 为下蹲/受击，4–8 多为攻击或施法姿势（不同形象略有差异）。
        /// </summary>
        public IEnumerator PlayAction(int[] frames, float frameTime = 0.09f)
        {
            if (hasOriginalSprite)
            {
                foreach (int frameIndex in frames)
                {
                    Sprite frame = UnitSpriteLoader.GetFrame(Data.spriteId, frameIndex);
                    if (frame != null) body.sprite = frame;
                    yield return new WaitForSeconds(frameTime);
                }
                body.sprite = UnitSpriteLoader.GetFrame(Data.spriteId, 0);
            }
            else
            {
                Vector3 original = transform.localScale;
                transform.localScale = original * 1.12f;
                yield return new WaitForSeconds(frameTime * Mathf.Max(1, frames.Length));
                transform.localScale = original;
            }
        }

        public IEnumerator DieRoutine()
        {
            float t = 0f;
            Vector3 s = transform.localScale;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.30f;
                transform.localScale = Vector3.Lerp(s, Vector3.zero, Mathf.Clamp01(t));
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
