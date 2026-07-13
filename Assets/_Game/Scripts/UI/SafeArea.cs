using System.Collections.Generic;
using UnityEngine;

namespace ZombieWar.UI
{
    /// <summary>
    /// Fits RectTransforms to the device safe area (notches, punch-holes, home bars).
    /// Attach to the panel holding interactive elements — not to a full-screen
    /// background, which should stay edge-to-edge behind the notch.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        [Tooltip("Inset horizontally. Turn off for panels that should span the full width.")]
        [SerializeField] private bool conformX = true;
        [Tooltip("Inset vertically. Turn off for panels that should span the full height.")]
        [SerializeField] private bool conformY = true;
        [Tooltip("Panels to fit. Leave empty to fit this RectTransform.")]
        [SerializeField] private List<RectTransform> panels = new List<RectTransform>();

        private Rect lastSafeArea;
        private int lastWidth, lastHeight;
        private ScreenOrientation lastOrientation;

        private void Awake()
        {
            if (panels.Count == 0) panels.Add(GetComponent<RectTransform>());
        }

        private void OnEnable() { Apply(); }
        private void OnRectTransformDimensionsChange() { Apply(); }

        // Rotating the device changes safeArea without changing this rect, so poll.
        private void Update()
        {
            if (Screen.width != lastWidth || Screen.height != lastHeight ||
                Screen.orientation != lastOrientation || Screen.safeArea != lastSafeArea)
            {
                Apply();
            }
        }

        private void Apply()
        {
            if (Screen.width <= 0 || Screen.height <= 0) return;
            if (panels.Count == 0) return;

            Rect safe = GetSafeArea();

            lastSafeArea = Screen.safeArea;
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            lastOrientation = Screen.orientation;

            if (!conformX) { safe.x = 0f; safe.width = Screen.width; }
            if (!conformY) { safe.y = 0f; safe.height = Screen.height; }

            Vector2 min = safe.position;
            Vector2 max = safe.position + safe.size;
            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;

            // Some Samsung devices report NaN/negative anchors on the first frame.
            if (float.IsNaN(min.x) || float.IsNaN(min.y) || float.IsNaN(max.x) || float.IsNaN(max.y)) return;
            if (min.x < 0f || min.y < 0f || max.x < 0f || max.y < 0f) return;

            for (int i = 0; i < panels.Count; i++)
            {
                var rt = panels[i];
                if (rt == null) continue;
                rt.anchorMin = min;
                rt.anchorMax = max;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
        }

        private Rect GetSafeArea()
        {
            Rect safe = Screen.safeArea;

            // The Editor's Game view can report a safeArea taken from a device preset
            // whose resolution differs from Screen.width/height (e.g. a 2340x1080 notch
            // profile while the view renders at 1920x1080). Mixing the two coordinate
            // systems produces anchors above 1.0 and throws the whole HUD off-screen.
            // Treat any rect that doesn't sit inside the screen as "no notch"; a real
            // device always reports a safeArea within its own bounds, so notch handling
            // on hardware is unaffected.
            bool inconsistent =
                safe.width <= 0f || safe.height <= 0f ||
                safe.xMax > Screen.width + 1f || safe.yMax > Screen.height + 1f ||
                safe.x < -1f || safe.y < -1f;

            if (inconsistent) safe = new Rect(0f, 0f, Screen.width, Screen.height);
            return safe;
        }
    }
}
