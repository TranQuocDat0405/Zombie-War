using System.Collections;
using UnityEngine;
using ZombieWar.Core;

namespace ZombieWar.Weapons
{
    /// <summary>Pooled bullet tracer: a line that fades out in a few frames.</summary>
    [RequireComponent(typeof(LineRenderer))]
    public class Tracer : MonoBehaviour
    {
        [SerializeField] private float duration = 0.07f;
        [SerializeField] private float startWidth = 0.06f;

        private LineRenderer line;

        private void Awake()
        {
            line = GetComponent<LineRenderer>();
        }

        public void Show(Vector3 from, Vector3 to)
        {
            line.SetPosition(0, from);
            line.SetPosition(1, to);
            StartCoroutine(FadeRoutine());
        }

        private IEnumerator FadeRoutine()
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float w = Mathf.Lerp(startWidth, 0f, t / duration);
                line.startWidth = w;
                line.endWidth = w * 0.5f;
                yield return null;
            }
            ObjectPool.Release(gameObject);
        }
    }
}
