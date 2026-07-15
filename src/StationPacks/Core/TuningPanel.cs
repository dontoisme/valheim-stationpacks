using System.Linq;
using StationPacks.Config;
using UnityEngine;

namespace StationPacks.Core
{
    /// <summary>
    /// Live slider panel for placing the pack on your back, toggled with a hotkey (default F6). Gated
    /// behind 'Enable dev tools' so players never see it. Writes straight to the equipped pack's back
    /// mesh every frame; "copy bake line" copies the final numbers to the clipboard for baking into
    /// PackDefinition.
    /// </summary>
    internal sealed class TuningPanel : MonoBehaviour
    {
        private bool _open;
        private bool _seeded;
        private Rect _win = new Rect(20, 60, 320, 430);
        private float _scale = 0.002f;
        private Vector3 _pos;
        private Vector3 _rot;

        private void Update()
        {
            if (!SPConfig.DevTools.Value) { _open = false; return; }
            if (SPConfig.TuneKey.Value.IsDown())
            {
                _open = !_open;
                if (_open) Seed();
            }
        }

        private System.Collections.Generic.List<Transform> Instances()
        {
            var player = Player.m_localPlayer;
            if (player == null) return null;
            return player.GetComponentsInChildren<PackBackTag>(true).Select(t => t.transform).ToList();
        }

        private void Seed()
        {
            var inst = Instances();
            if (inst == null || inst.Count == 0) { _seeded = false; return; }
            var t = inst[0];
            _scale = t.localScale.x;
            _pos = t.localPosition;
            _rot = t.localEulerAngles;
            if (_rot.x > 180) _rot.x -= 360;
            if (_rot.y > 180) _rot.y -= 360;
            if (_rot.z > 180) _rot.z -= 360;
            _seeded = true;
        }

        private void OnGUI()
        {
            if (!_open || !SPConfig.DevTools.Value) return;
            _win = GUILayout.Window(0x5A7C, _win, DrawWindow, "StationPacks - back mesh");
        }

        private void DrawWindow(int id)
        {
            var inst = Instances();
            if (inst == null || inst.Count == 0)
            {
                GUILayout.Label("Equip a pack to tune it.");
                if (GUILayout.Button("Close")) _open = false;
                GUI.DragWindow();
                return;
            }
            if (!_seeded) Seed();

            _scale = Slider("Scale", _scale, 0.0005f, 0.02f, "0.####");
            GUILayout.Space(4);
            _pos.x = Slider("Pos X (left/right)", _pos.x, -0.03f, 0.03f, "0.####");
            _pos.y = Slider("Pos Y (up back)", _pos.y, -0.03f, 0.03f, "0.####");
            _pos.z = Slider("Pos Z (from spine)", _pos.z, -0.03f, 0.03f, "0.####");
            GUILayout.Space(4);
            _rot.x = Slider("Pitch", _rot.x, -180f, 180f, "0.#");
            _rot.y = Slider("Yaw", _rot.y, -180f, 180f, "0.#");
            _rot.z = Slider("Roll", _rot.z, -180f, 180f, "0.#");

            foreach (var t in inst)
            {
                t.localScale = Vector3.one * _scale;
                t.localPosition = _pos;
                t.localRotation = Quaternion.Euler(_rot);
            }

            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Resync")) Seed();
            if (GUILayout.Button("Copy bake line")) CopyBakeLine();
            if (GUILayout.Button("Close")) _open = false;
            GUILayout.EndHorizontal();

            GUILayout.Label(BakeLine());
            GUI.DragWindow();
        }

        private static float Slider(string label, float value, float min, float max, string fmt)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(140));
            GUILayout.Label(value.ToString(fmt), GUILayout.Width(60));
            GUILayout.EndHorizontal();
            return GUILayout.HorizontalSlider(value, min, max);
        }

        private string BakeLine() =>
            $"BackScale={_scale:0.####}  BackOffset=({_pos.x:0.####},{_pos.y:0.####},{_pos.z:0.####})  " +
            $"BackEuler=({_rot.x:0.#},{_rot.y:0.#},{_rot.z:0.#})";

        private void CopyBakeLine()
        {
            var line = BakeLine();
            GUIUtility.systemCopyBuffer = line;
            if (Console.instance != null) Console.instance.Print("<color=lime>copied: " + line + "</color>");
            Plugin.Log.LogInfo("[tune] " + line);
        }
    }
}
