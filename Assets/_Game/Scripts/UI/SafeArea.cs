using UnityEngine;

namespace ZombieWar.UI
{
    /// <summary>Fits this RectTransform to the device safe area (notches, punch-holes).</summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        private Rect applied;

        private void Awake() { Apply(); }
        private void OnEnable() { Apply(); }
        private void OnRectTransformDimensionsChange() { Apply(); }

        private void Apply()
        {
            Rect safe = Screen.safeArea;

            // The Editor's Game view can report a safeArea from a device preset
            // whose resolution differs from Screen.width/height (e.g. a 2340x1080
            // notch profile while the view renders 1920x1080). Mixing the two
            // coordinate systems blows the HUD outside the visible screen, so
            // treat any inconsistent rect as "no notch". Real devices always
            // report a safeArea inside the screen, keeping notch handling intact.
            if (safe.width <= 0f || safe.height <= 0f ||
                safe.xMax > Screen.width + 1f || safe.yMax > Screen.height + 1f ||
                safe.x < -1f || safe.y < -1f)
            {
                safe = new Rect(0f, 0f, Screen.width, Screen.height);
            }

            if (safe == applied) return;
            applied = safe;

            var rt = GetComponent<RectTransform>();
            Vector2 min = safe.position;
            Vector2 max = safe.position + safe.size;
            min.x = Mathf.Clamp01(min.x / Screen.width);
            min.y = Mathf.Clamp01(min.y / Screen.height);
            max.x = Mathf.Clamp01(max.x / Screen.width);
            max.y = Mathf.Clamp01(max.y / Screen.height);
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
