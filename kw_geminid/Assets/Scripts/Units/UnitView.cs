using System.Collections;
using UnityEngine;

namespace KwGeminid
{
    public class UnitView : MonoBehaviour
    {
        public UnitData Data { get; private set; }
        public Hex Position { get; set; }
        public bool HasActed { get; set; }

        SpriteRenderer sr;
        SpriteRenderer hpBg;
        SpriteRenderer hpFill;
        TextMesh label;
        bool usesOriginalSprite;

        public void Init(UnitData data)
        {
            Data = data;
            name = (data.isEnemy ? "[敌]" : "[我]") + data.name;

            // 主精灵
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 10;
            ApplySpriteFrame(0);

            // 棋子上的单字标签（仅程序化棋子显示）
            if (!usesOriginalSprite)
            {
                var labelGo = new GameObject("Label");
                labelGo.transform.SetParent(transform, false);
                labelGo.transform.localPosition = new Vector3(0, -0.05f, 0);
                label = labelGo.AddComponent<TextMesh>();
                label.text = string.IsNullOrEmpty(data.shortLabel) ? data.name.Substring(0, 1) : data.shortLabel;
                label.fontSize = 32;
                label.characterSize = 0.08f;
                label.anchor = TextAnchor.MiddleCenter;
                label.alignment = TextAlignment.Center;
                label.color = Color.white;
                var mr = labelGo.GetComponent<MeshRenderer>();
                mr.sortingOrder = 11;
            }

            // 血条
            var hpGo = new GameObject("HPBar");
            hpGo.transform.SetParent(transform, false);
            float barY = usesOriginalSprite ? 0.55f : 0.42f;
            hpGo.transform.localPosition = new Vector3(0, barY, 0);

            var bgGo = new GameObject("Bg");
            bgGo.transform.SetParent(hpGo.transform, false);
            hpBg = bgGo.AddComponent<SpriteRenderer>();
            hpBg.sprite = SpriteFactory.PixelSprite();
            hpBg.color = new Color(0, 0, 0, 0.7f);
            hpBg.sortingOrder = 12;
            bgGo.transform.localScale = new Vector3(0.7f, 0.08f, 1f);

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(hpGo.transform, false);
            hpFill = fillGo.AddComponent<SpriteRenderer>();
            hpFill.sprite = SpriteFactory.PixelSprite();
            hpFill.color = data.isEnemy ? new Color(0.9f, 0.2f, 0.2f) : new Color(0.2f, 0.85f, 0.3f);
            hpFill.sortingOrder = 13;
            fillGo.transform.localScale = new Vector3(0.66f, 0.05f, 1f);

            UpdateVisual();
        }

        void ApplySpriteFrame(int frame)
        {
            Sprite sp = (Data != null && Data.spriteId >= 0)
                ? UnitSpriteLoader.GetFrame(Data.spriteId, frame)
                : null;

            if (sp != null)
            {
                sr.sprite = sp;
                usesOriginalSprite = true;
            }
            else
            {
                sr.sprite = SpriteFactory.MakePawnSprite(Data.unitClass, Data.isEnemy, Data.isLeader);
                usesOriginalSprite = false;
            }
        }

        public void SetFacingLeft(bool left)
        {
            if (sr != null) sr.flipX = left;
        }

        public void UpdateVisual()
        {
            float hpPercent = Mathf.Clamp01((float)Data.hp / Data.maxHp);
            hpFill.transform.localScale = new Vector3(0.66f * hpPercent, 0.05f, 1f);
            hpFill.transform.localPosition = new Vector3(-0.33f * (1f - hpPercent), 0, 0);

            if (HasActed)
            {
                sr.color = new Color(0.5f, 0.5f, 0.5f, 0.85f);
                if (label != null) label.color = new Color(0.7f, 0.7f, 0.7f, 0.85f);
            }
            else
            {
                sr.color = Color.white;
                if (label != null) label.color = Color.white;
            }
        }

        public IEnumerator PlayActionAnim()
        {
            if (!usesOriginalSprite) yield break;
            for (int f = 4; f <= 8; f++)
            {
                ApplySpriteFrame(f);
                yield return new WaitForSeconds(0.06f);
            }
            ApplySpriteFrame(0);
        }

        public IEnumerator PlayHurtAnim()
        {
            if (usesOriginalSprite) ApplySpriteFrame(3);
            yield return new WaitForSeconds(0.18f);
            if (usesOriginalSprite) ApplySpriteFrame(0);
        }

        public IEnumerator MoveAlong(System.Collections.Generic.List<Hex> path, float speed = 5f)
        {
            for (int i = 0; i < path.Count; i++)
            {
                Hex h = path[i];
                Vector3 target = GridGeometry.HexToWorld(h);
                if (target.x < transform.position.x) SetFacingLeft(true);
                else if (target.x > transform.position.x) SetFacingLeft(false);

                int stepFrame = 0;
                while (Vector3.Distance(transform.position, target) > 0.02f)
                {
                    transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
                    if (usesOriginalSprite)
                    {
                        stepFrame = (int)(Time.time * 8f) % 3;
                        ApplySpriteFrame(stepFrame);
                    }
                    yield return null;
                }
                transform.position = target;
            }
            ApplySpriteFrame(0);
        }

        public IEnumerator FadeOut(float duration = 0.5f)
        {
            float t = 0;
            Color c0 = sr.color;
            while (t < duration)
            {
                t += Time.deltaTime;
                float a = Mathf.Lerp(c0.a, 0f, t / duration);
                sr.color = new Color(c0.r, c0.g, c0.b, a);
                hpBg.color = new Color(0, 0, 0, a * 0.7f);
                hpFill.color = new Color(hpFill.color.r, hpFill.color.g, hpFill.color.b, a);
                yield return null;
            }
        }
    }
}
