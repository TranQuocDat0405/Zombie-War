using UnityEngine;

namespace ZombieWar
{
    /// <summary>App-level flow constants. Per-weapon/zombie tuning stays on their own assets.</summary>
    [CreateAssetMenu(menuName = "ZombieWar/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        public float levelDuration = 180f;
        public float endSlowMotionScale = 0.4f; // timeScale while the result popup is up
    }
}
