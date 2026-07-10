using System.Collections;
using UnityEngine;
using ZombieWar.Core;

namespace ZombieWar.Zombie
{
    /// <summary>
    /// Animates the ZombieWar/Dissolve shader through MaterialPropertyBlocks so
    /// pooled zombies never allocate material instances.
    /// </summary>
    public class DissolveEffect : MonoBehaviour, IPoolable
    {
        [SerializeField] private float flashDuration = 0.12f;

        private Renderer[] renderers;
        private MaterialPropertyBlock mpb;
        private Coroutine flashRoutine;

        private static readonly int DissolveId = Shader.PropertyToID("_DissolveAmount");
        private static readonly int FlashId = Shader.PropertyToID("_FlashAmount");

        private void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            mpb = new MaterialPropertyBlock();
        }

        public void Flash()
        {
            if (!isActiveAndEnabled) return;
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(FlashRoutine());
        }

        public Coroutine PlayDissolve(float duration)
        {
            return StartCoroutine(DissolveRoutine(duration));
        }

        private IEnumerator FlashRoutine()
        {
            SetFloat(FlashId, 0.65f);
            yield return new WaitForSeconds(flashDuration);
            SetFloat(FlashId, 0f);
            flashRoutine = null;
        }

        private IEnumerator DissolveRoutine(float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                SetFloat(DissolveId, Mathf.SmoothStep(0f, 1f, t / duration));
                yield return null;
            }
            SetFloat(DissolveId, 1f);
        }

        private void SetFloat(int id, float value)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null) continue;
                r.GetPropertyBlock(mpb);
                mpb.SetFloat(id, value);
                r.SetPropertyBlock(mpb);
            }
        }

        public void OnSpawned()
        {
            SetFloat(DissolveId, 0f);
            SetFloat(FlashId, 0f);
        }

        public void OnDespawned()
        {
            StopAllCoroutines();
            flashRoutine = null;
        }
    }
}
