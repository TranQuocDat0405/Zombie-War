using System;
using NFramework;
using UnityEngine;
using UnityEngine.UI;

namespace ZombieWar.UI
{
    /// <summary>
    /// Full-screen loading overlay (replaces the old SceneLoader overlay).
    /// Opened by GameManager during boot and scene transitions.
    /// </summary>
    public class LoadingPopup : BaseUIView
    {
        [SerializeField] private Image progressFill;
        [SerializeField] private float minShowTime = 0.7f; // same pacing as the old SceneLoader

        private Action _onComplete;
        private float _startTime;

        /// <summary>Called when the bar finishes. Pass null to keep it up until Close.</summary>
        public void AssignEvent(Action onComplete)
        {
            _onComplete = onComplete;
        }

        public override void OnOpen()
        {
            base.OnOpen();
            _startTime = Time.unscaledTime;
            SetProgress(0f);
        }

        private void Update()
        {
            float t = Mathf.Clamp01((Time.unscaledTime - _startTime) / minShowTime);
            SetProgress(t);
            if (t >= 1f && _onComplete != null)
            {
                var cb = _onComplete;
                _onComplete = null;
                cb.Invoke();
                CloseSelf();
            }
        }

        private void SetProgress(float p)
        {
            if (progressFill == null) return;
            // Anchor-based fill, same technique the old SceneLoader bar used.
            var rt = progressFill.rectTransform;
            rt.anchorMax = new Vector2(Mathf.Lerp(0.03f, 1f, p), 1f);
        }
    }
}
