using StationPacks.Config;
using UnityEngine;

namespace StationPacks.Core
{
    /// <summary>
    /// Live slider panel for framing a pack's rendered inventory icon, toggled with a hotkey (default F7)
    /// and gated behind 'Enable dev tools' so players never see it. This is the icon-side twin of
    /// <see cref="TuningPanel"/>: pick a pack, drag field-of-view / zoom / angle / size, and the icon is
    /// re-rendered and swapped straight onto the item so you see it live in your inventory. "Copy bake
    /// line" copies the numbers for pasting into that pack's <see cref="PackDefinition"/>.
    ///
    /// Icon framing can only be judged by looking, so this is the iterate-and-look instrument for it - the
    /// same workflow as the back-mesh tuning: nudge, look, bake.
    /// </summary>
    internal sealed class IconTuningPanel : MonoBehaviour
    {
        private bool _open;
        private Rect _win = new Rect(360, 60, 340, 470);

        private int _index;
        private float _fov;
        private float _dist;
        private float _size;
        private Vector3 _rot;

        // The rounded values the current preview was rendered at, so we only re-render when something
        // actually moved (and not on every drag frame).
        private Vector4 _renderedAt = new Vector4(float.NaN, 0, 0, 0);
        private Vector3 _renderedRot;
        private Sprite _preview;

        private PackDefinition Def => PackDefinition.All[_index];

        private void Update()
        {
            if (!SPConfig.DevTools.Value) { _open = false; return; }
            if (SPConfig.IconTuneKey.Value.IsDown())
            {
                _open = !_open;
                if (_open) Seed();
            }
        }

        private void Seed()
        {
            var def = Def;
            _fov = def.IconFieldOfView;
            _dist = def.IconDistanceMultiplier;
            _size = def.IconRenderSize;
            _rot = def.IconRotationEuler;
            Normalize(ref _rot);
            _renderedAt = new Vector4(float.NaN, 0, 0, 0);   // force a fresh render
        }

        private static void Normalize(ref Vector3 e)
        {
            if (e.x > 180) e.x -= 360;
            if (e.y > 180) e.y -= 360;
            if (e.z > 180) e.z -= 360;
        }

        private void OnGUI()
        {
            if (!_open || !SPConfig.DevTools.Value) return;
            _win = GUILayout.Window(0x5A7D, _win, DrawWindow, "StationPacks - icon framing");
        }

        private void DrawWindow(int id)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("<")) Cycle(-1);
            GUILayout.Label($" {Def.DisplayName} ", GUILayout.ExpandWidth(true));
            if (GUILayout.Button(">")) Cycle(1);
            GUILayout.EndHorizontal();

            GUILayout.Space(4);
            _fov = Slider("Field of view", _fov, 0.1f, 60f, "0.##");
            _dist = Slider("Zoom (distance x)", _dist, 0.4f, 3f, "0.###");
            _size = Slider("Render size (px)", _size, 32f, 256f, "0");
            GUILayout.Space(4);
            _rot.x = Slider("Pitch", _rot.x, -180f, 180f, "0.#");
            _rot.y = Slider("Yaw", _rot.y, -180f, 180f, "0.#");
            _rot.z = Slider("Roll", _rot.z, -180f, 180f, "0.#");

            // Re-render when a value changed, but wait until the mouse is released so we render once per
            // drag rather than every frame RenderManager can't keep up with.
            if (Changed() && !Input.GetMouseButton(0)) RenderPreview();

            GUILayout.Space(8);
            if (_preview != null && _preview.texture != null)
            {
                var prev = GUILayoutUtility.GetRect(128, 128, GUILayout.ExpandWidth(false));
                GUI.Box(prev, GUIContent.none);
                GUI.DrawTexture(new Rect(prev.x + (prev.width - 128) / 2, prev.y, 128, 128),
                                _preview.texture, ScaleMode.ScaleToFit, alphaBlend: true);
            }
            else
            {
                GUILayout.Label("<no preview - equip nothing needed; enter a world first>");
            }

            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reseed")) Seed();
            if (GUILayout.Button("Copy bake line")) CopyBakeLine();
            if (GUILayout.Button("Close")) _open = false;
            GUILayout.EndHorizontal();

            GUILayout.Label(BakeLine());
            GUI.DragWindow();
        }

        private void Cycle(int delta)
        {
            _index = (_index + delta + PackDefinition.All.Count) % PackDefinition.All.Count;
            Seed();
        }

        private bool Changed() =>
            !Mathf.Approximately(_renderedAt.x, _fov) ||
            !Mathf.Approximately(_renderedAt.y, _dist) ||
            !Mathf.Approximately(_renderedAt.z, _size) ||
            _renderedRot != _rot;

        /// <summary>
        /// Writes the panel's values onto the live def, re-renders the icon, and swaps it onto the item's
        /// shared data so it updates everywhere the icon is drawn (SharedData is shared across every
        /// instance of the item). The old sprite/texture is dropped so a long session doesn't leak.
        /// </summary>
        private void RenderPreview()
        {
            var def = Def;
            def.IconFieldOfView = _fov;
            def.IconDistanceMultiplier = _dist;
            def.IconRenderSize = Mathf.RoundToInt(_size);
            def.IconRotationEuler = _rot;

            var sprite = StationIconRenderer.Render(def);
            _renderedAt = new Vector4(_fov, _dist, _size, 0);
            _renderedRot = _rot;
            if (sprite == null) return;

            var old = _preview;
            _preview = sprite;

            var shared = SharedDataFor(def);
            if (shared != null) shared.m_icons = new[] { sprite };

            if (old != null)
            {
                if (old.texture != null) Destroy(old.texture);
                Destroy(old);
            }
        }

        private static ItemDrop.ItemData.SharedData SharedDataFor(PackDefinition def)
        {
            var db = ObjectDB.instance;
            if (db == null) return null;
            var prefab = db.GetItemPrefab(def.PrefabName);
            var drop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
            return drop != null ? drop.m_itemData.m_shared : null;
        }

        private static float Slider(string label, float value, float min, float max, string fmt)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(140));
            GUILayout.Label(value.ToString(fmt), GUILayout.Width(60));
            GUILayout.EndHorizontal();
            return GUILayout.HorizontalSlider(value, min, max);
        }

        // Emits paste-ready C# field initializers - drop straight into the pack's PackDefinition block
        // next to its BackScale/BackOffset lines. Trailing commas so it slots between other fields.
        private string BakeLine() =>
            $"IconFieldOfView = {_fov:0.##}f,\n" +
            $"IconDistanceMultiplier = {_dist:0.###}f,\n" +
            $"IconRotationEuler = new Vector3({_rot.x:0.#}f, {_rot.y:0.#}f, {_rot.z:0.#}f),\n" +
            $"IconRenderSize = {Mathf.RoundToInt(_size)},";

        private void CopyBakeLine()
        {
            var line = BakeLine();
            GUIUtility.systemCopyBuffer = line;
            if (Console.instance != null) Console.instance.Print("<color=lime>copied: " + line + "</color>");
            Plugin.Log.LogInfo("[icontune] " + Def.DisplayName + "  " + line);
        }
    }
}
