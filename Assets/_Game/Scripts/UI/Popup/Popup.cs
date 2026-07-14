using DG.Tweening;
using NFramework;
using UnityEngine;
using UnityEngine.UI;

namespace ZombieWar.UI
{
    /// <summary>
    /// Base for every real popup: close-button wiring, open SFX, scale/fade tween.
    /// Tweens run on unscaled time because popups open while the game is frozen
    /// (pause, tutorial) or in end-of-match slow motion.
    /// </summary>
    public class Popup : BaseUIView
    {
        [SerializeField] protected Transform _root; // centre panel to tween
        [SerializeField] private Ease _ease = Ease.OutBack;
        [SerializeField] protected Button _closeButton;

        protected virtual void Awake()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(() =>
                {
                    SoundManager.I.PlaySFXResource(Define.SoundName.CLICK_BUTTON);
                    CloseSelf();
                });
            }
        }

        public override void OnOpen()
        {
            base.OnOpen();
            SoundManager.I.PlaySFXResource(Define.SoundName.OPEN_POPUP);
            if (_root != null)
            {
                _root.DOKill();
                _root.DOScale(1f, 0.5f).From(0.5f).SetEase(_ease).SetUpdate(true);
                CanvasGroup.DOFade(1f, 0.5f).From(0f).SetEase(Ease.OutCirc).SetUpdate(true);
            }
        }

        public override void HandleOnKeyBack() => CloseSelf();
    }
}
