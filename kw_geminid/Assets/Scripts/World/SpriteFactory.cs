using UnityEngine;

namespace KwGeminid
{
    public static class SpriteFactory
    {
        static Sprite pixelSprite;

        public static Sprite PixelSprite()
        {
            if (pixelSprite == null)
            {
                var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();
                pixelSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            }
            return pixelSprite;
        }

        public static Sprite MakeHexSprite(TerrainType t)
        {
            Color c = TerrainTable.BaseColor(t);
            int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, c);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size * 1.5f);
        }

        public static Sprite MakeHighlightSprite(Color color)
        {
            int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, Color.white);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size * 1.2f);
        }

        public static Sprite MakePawnSprite(UnitClassType c, bool isEnemy, bool isLeader)
        {
            int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color baseColor = isEnemy ? new Color(0.85f, 0.25f, 0.25f) : new Color(0.25f, 0.55f, 0.95f);
            if (isLeader) baseColor = Color.Lerp(baseColor, Color.yellow, 0.4f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(size / 2, size / 2));
                    if (dist < size * 0.45f)
                    {
                        tex.SetPixel(x, y, dist > size * 0.4f ? Color.white : baseColor);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size * 1.5f);
        }
    }
}
