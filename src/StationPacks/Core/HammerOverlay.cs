using Jotunn.Managers;
using UnityEngine;

namespace StationPacks.Core
{
    /// <summary>
    /// Lays the vanilla Hammer's own inventory icon across the lower portion of a pack icon, so the item
    /// reads at a glance as "this is a building tool" - the same affordance the Hammer, Hoe and Cultivator
    /// carry. We reuse the game's Hammer sprite rather than shipping art: it is the exact hammer players
    /// build with, already drawn at a diagonal, so it sits naturally "laid across" the station render.
    ///
    /// Purely cosmetic and defensive, like the rest of the icon path: any failure returns null and the
    /// caller keeps the un-stamped icon, so a pack is never left without one.
    /// </summary>
    public static class HammerOverlay
    {
        // Framing of the stamp, as fractions of the icon's own size so it works at any resolution
        // (rendered icons are 92px, the fallback PNGs are their native size). A small badge tucked in
        // the bottom-right corner: enough to signal "build tool" without fighting the station render.
        private const float WidthFrac = 0.4f;        // hammer width vs. icon width - a corner badge
        private const float MarginFrac = 0.04f;       // gap from the bottom and right edges
        private const float ShadowAlpha = 0.5f;       // darkness of the drop shadow under the hammer
        private const float ShadowOffsetFrac = 0.015f; // shadow shift, down-right, vs. icon width

        private static Sprite _hammer;
        private static bool _lookedUp;

        /// <summary>
        /// Returns a new sprite: <paramref name="icon"/> with the hammer stamped across it. Returns null
        /// if the hammer sprite or the icon texture is unavailable, in which case the caller keeps the
        /// original icon unchanged.
        /// </summary>
        public static Sprite Apply(Sprite icon)
        {
            try
            {
                var baseTex = icon != null ? icon.texture as Texture2D : null;
                if (baseTex == null) return null;

                var hammer = VanillaHammerSprite();
                if (hammer == null) return null;

                int w = baseTex.width, h = baseTex.height;
                var basePix = baseTex.GetPixels();

                var hr = hammer.textureRect;
                int hw = Mathf.RoundToInt(w * WidthFrac);
                int hh = Mathf.RoundToInt(hw * (hr.height / hr.width));
                var hamPix = ScaledHammerPixels(hammer, hw, hh);
                if (hamPix == null) return null;

                int margin = Mathf.RoundToInt(w * MarginFrac);
                int ox = w - hw - margin;   // tucked to the right edge
                int oy = margin;            // and the bottom edge
                int shadow = Mathf.RoundToInt(w * ShadowOffsetFrac);

                // Drop shadow first (black, offset down-right), then the hammer over it, so the tool
                // separates from whatever station is rendered behind it.
                Blend(basePix, w, h, hamPix, hw, hh, ox + shadow, oy - shadow, Color.black, ShadowAlpha);
                Blend(basePix, w, h, hamPix, hw, hh, ox, oy, Color.white, 1f);

                var outTex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
                outTex.SetPixels(basePix);
                outTex.Apply();
                return Sprite.Create(outTex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogWarning($"Hammer overlay threw {e.GetType().Name}: {e.Message}; icon left un-stamped.");
                return null;
            }
        }

        /// <summary>Source-over composite of <paramref name="srcPix"/> onto <paramref name="dstPix"/>,
        /// tinted (black for the shadow, white = identity for the hammer) and scaled by opacity.</summary>
        private static void Blend(Color[] dstPix, int dw, int dh, Color[] srcPix, int sw, int sh,
                                  int ox, int oy, Color tint, float opacity)
        {
            for (int y = 0; y < sh; y++)
            {
                int dy = oy + y;
                if (dy < 0 || dy >= dh) continue;
                for (int x = 0; x < sw; x++)
                {
                    int dx = ox + x;
                    if (dx < 0 || dx >= dw) continue;

                    var s = srcPix[y * sw + x];
                    float a = s.a * opacity;
                    if (a <= 0f) continue;

                    int di = dy * dw + dx;
                    var d = dstPix[di];
                    float na = 1f - a;
                    dstPix[di] = new Color(
                        s.r * tint.r * a + d.r * na,
                        s.g * tint.g * a + d.g * na,
                        s.b * tint.b * a + d.b * na,
                        a + d.a * na);
                }
            }
        }

        /// <summary>
        /// Extracts the hammer sprite's region and scales it to <paramref name="w"/>x<paramref name="h"/>
        /// into a readable pixel buffer. Going through a RenderTexture means it works even when the
        /// vanilla sprite's source texture is not CPU-readable, and handles atlassed sprites via the
        /// sprite's textureRect.
        /// </summary>
        private static Color[] ScaledHammerPixels(Sprite hammer, int w, int h)
        {
            var src = hammer.texture;
            var r = hammer.textureRect;
            var scale = new Vector2(r.width / src.width, r.height / src.height);
            var offset = new Vector2(r.x / src.width, r.y / src.height);

            var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
            var prev = RenderTexture.active;
            Texture2D tmp = null;
            try
            {
                Graphics.Blit(src, rt, scale, offset);
                RenderTexture.active = rt;
                tmp = new Texture2D(w, h, TextureFormat.RGBA32, false);
                tmp.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tmp.Apply();
                return tmp.GetPixels();
            }
            finally
            {
                RenderTexture.active = prev;
                RenderTexture.ReleaseTemporary(rt);
                if (tmp != null) Object.Destroy(tmp);
            }
        }

        private static Sprite VanillaHammerSprite()
        {
            if (_lookedUp) return _hammer;
            _lookedUp = true;

            // PrefabManager resolves vanilla prefabs reliably at registration time (it's what the icon
            // render itself uses); ObjectDB.instance is not populated yet this early.
            var prefab = PrefabManager.Instance.GetPrefab("Hammer");
            var drop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
            var icons = drop != null ? drop.m_itemData.m_shared.m_icons : null;
            _hammer = (icons != null && icons.Length > 0) ? icons[0] : null;

            if (_hammer == null)
                Plugin.Log.LogWarning("Hammer overlay: vanilla Hammer icon not found; packs keep their plain icons.");
            return _hammer;
        }
    }
}
