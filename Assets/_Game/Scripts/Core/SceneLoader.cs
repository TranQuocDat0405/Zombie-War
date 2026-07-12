using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ZombieWar.Core
{
    /// <summary>
    /// Persistent scene loader with a full-screen loading overlay + progress bar.
    /// A prefab instance lives in every scene; later duplicates self-destruct.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        public static SceneLoader Instance { get; private set; }

        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image progressFill;   // sliced image resized via anchors
        [SerializeField] private float minShowTime = 0.7f;

        private bool loading;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (overlayRoot != null) overlayRoot.SetActive(false);
        }

        /// <summary>Loads a scene behind the loading overlay. Safe to call while paused.</summary>
        public static void Load(string sceneName)
        {
            if (Instance != null) Instance.StartLoad(sceneName);
            else SceneManager.LoadScene(sceneName); // fallback, no overlay
        }

        private void StartLoad(string sceneName)
        {
            if (loading) return;
            loading = true;
            StartCoroutine(LoadRoutine(sceneName));
        }

        private IEnumerator LoadRoutine(string sceneName)
        {
            Time.timeScale = 1f; // leaving a paused/slow-mo state

            overlayRoot.SetActive(true);
            canvasGroup.alpha = 0f;
            SetProgress(0f);

            while (canvasGroup.alpha < 1f)
            {
                canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 1f, Time.unscaledDeltaTime * 6f);
                yield return null;
            }

            float start = Time.unscaledTime;
            var op = SceneManager.LoadSceneAsync(sceneName);
            op.allowSceneActivation = false;

            while (op.progress < 0.9f || Time.unscaledTime - start < minShowTime)
            {
                float loadFrac = Mathf.Clamp01(op.progress / 0.9f);
                float timeFrac = Mathf.Clamp01((Time.unscaledTime - start) / minShowTime);
                SetProgress(Mathf.Min(loadFrac, timeFrac));
                yield return null;
            }

            SetProgress(1f);
            op.allowSceneActivation = true;
            while (!op.isDone) yield return null;
            yield return null; // let the new scene run its first frame

            while (canvasGroup.alpha > 0f)
            {
                canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 0f, Time.unscaledDeltaTime * 4f);
                yield return null;
            }

            overlayRoot.SetActive(false);
            loading = false;
        }

        private void SetProgress(float p)
        {
            if (progressFill == null) return;
            var rt = progressFill.rectTransform;
            rt.anchorMax = new Vector2(Mathf.Lerp(0.03f, 1f, Mathf.Clamp01(p)), 1f);
        }
    }
}
