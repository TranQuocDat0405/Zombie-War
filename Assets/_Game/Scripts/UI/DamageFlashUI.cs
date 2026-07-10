using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ZombieWar.UI
{
    /// <summary>Full-screen red vignette pulse when the player takes damage.</summary>
    [RequireComponent(typeof(Image))]
    public class DamageFlashUI : MonoBehaviour
    {
        [SerializeField] private float peakAlpha = 0.35f;
        [SerializeField] private float fadeTime = 0.4f;

        private Image image;
        private Coroutine routine;

        private void Awake()
        {
            image = GetComponent<Image>();
            image.raycastTarget = false;
            SetAlpha(0f);
        }

        public void Flash()
        {
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            SetAlpha(peakAlpha);
            float t = 0f;
            while (t < fadeTime)
            {
                t += Time.unscaledDeltaTime;
                SetAlpha(Mathf.Lerp(peakAlpha, 0f, t / fadeTime));
                yield return null;
            }
            SetAlpha(0f);
        }

        private void SetAlpha(float a)
        {
            var c = image.color;
            c.a = a;
            image.color = c;
        }
    }
}
