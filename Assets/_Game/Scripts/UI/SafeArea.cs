using UnityEngine;

namespace ZombieWar.UI
{
    /// <summary>Fits this RectTransform to the device safe area (notches, punch-holes).</summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        private Rect applied;

        private void Awake() { Apply(); }
        private void OnRectTransformDimensionsChange() { Apply(); }

        private void Apply()
        {
            Rect safe = Screen.safeArea;
            if (safe == applied) return;
            applied = safe;

            var rt = GetComponent<RectTransform>();
            Vector2 min = safe.position;
            Vector2 max = safe.position + safe.size;
            min.x /= Screen.width; min.y /= Screen.height;
            max.x /= Screen.width; max.y /= Screen.height;
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
