using UnityEngine;

namespace KwCursor
{
    /// <summary>战场飘字（伤害/回复/MISS 等）。</summary>
    public class PopupText : MonoBehaviour
    {
        TextMesh tm;
        float t;

        public static void Spawn(Vector3 worldPos, string msg, Color color)
        {
            var go = new GameObject("Popup");
            go.transform.position = worldPos + new Vector3(0f, 0.35f, -0.5f);
            var tm = go.AddComponent<TextMesh>();
            tm.text = msg;
            tm.fontSize = 56;
            tm.characterSize = 0.028f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            tm.fontStyle = FontStyle.Bold;
            Font f = UIFactory.DefaultFont();
            if (f != null)
            {
                tm.font = f;
                var mr = go.GetComponent<MeshRenderer>();
                mr.sharedMaterial = f.material;
                mr.sortingOrder = 20;
            }
            var p = go.AddComponent<PopupText>();
            p.tm = tm;
        }

        void Update()
        {
            t += Time.deltaTime;
            transform.position += Vector3.up * (Time.deltaTime * 0.9f);
            if (tm != null)
            {
                Color c = tm.color;
                c.a = Mathf.Clamp01(1.3f - t);
                tm.color = c;
            }
            if (t > 1.2f) Destroy(gameObject);
        }
    }
}
