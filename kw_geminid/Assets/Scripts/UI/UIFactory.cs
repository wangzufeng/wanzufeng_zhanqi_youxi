using System;
using UnityEngine;
using UnityEngine.UI;

namespace KwGeminid
{
    public static class UIFactory
    {
        public static Font DefaultFont()
        {
            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return f;
        }

        public static GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 anchoredPos, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = anchoredPos;

            var img = go.GetComponent<Image>();
            img.color = color;
            return go;
        }

        public static Button CreateButton(Transform parent, string text, Vector2 size, Action onClick)
        {
            var go = new GameObject("Btn_" + text, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;

            var img = go.GetComponent<Image>();
            img.color = new Color(0.2f, 0.25f, 0.35f, 0.95f);

            var btn = go.GetComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.3f, 0.4f, 0.55f);
            colors.pressedColor = new Color(0.15f, 0.2f, 0.28f);
            btn.colors = colors;
            btn.onClick.AddListener(() => { if (onClick != null) onClick(); });

            var txtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            txtGo.transform.SetParent(go.transform, false);
            var txtRt = txtGo.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero;

            var t = txtGo.GetComponent<Text>();
            t.text = text;
            t.font = DefaultFont();
            t.fontSize = 20;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;

            return btn;
        }
    }
}
