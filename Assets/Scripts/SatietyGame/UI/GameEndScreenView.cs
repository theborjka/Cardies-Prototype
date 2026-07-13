using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace SatietyGame
{
    public sealed class GameEndScreenView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject root;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform animatedImage;
        [SerializeField] private Button restartButton;
        [SerializeField] private Transform confettiRoot;

        [Header("Entry Motion")]
        [SerializeField] private Vector2 entryOffset = new Vector2(720f, 0f);
        [SerializeField] private float entryRotation = 18f;
        [SerializeField, Min(0.01f)] private float entryDuration = 0.42f;
        [SerializeField, Min(0.01f)] private float breakDuration = 0.16f;
        [SerializeField] private float breakOvershoot = 20f;
        [SerializeField] private float breakRotation = 4f;
        [SerializeField, Min(0.01f)] private float settleDuration = 0.18f;

        private Vector2 homePosition;
        private Vector3 homeScale;
        private Quaternion homeRotation;
        private Sequence activeSequence;
        private Action restartAction;

        private void Awake()
        {
            if (root == null)
            {
                root = gameObject;
            }

            if (canvasGroup == null)
            {
                canvasGroup = root.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = root.AddComponent<CanvasGroup>();
                }
            }

            if (animatedImage == null)
            {
                animatedImage = transform as RectTransform;
            }

            if (animatedImage != null)
            {
                homePosition = animatedImage.anchoredPosition;
                homeScale = animatedImage.localScale;
                homeRotation = animatedImage.localRotation;
            }

            if (restartButton != null)
            {
                restartButton.onClick.AddListener(OnRestartClicked);
            }

            Hide(true);
        }

        private void OnDestroy()
        {
            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(OnRestartClicked);
            }

            activeSequence?.Kill();
        }

        public void Show(Action onRestart)
        {
            restartAction = onRestart;
            activeSequence?.Kill();

            root.SetActive(true);
            canvasGroup.alpha = 0f;

            if (animatedImage != null)
            {
                animatedImage.anchoredPosition = homePosition + entryOffset;
                animatedImage.localScale = homeScale * 0.82f;
                animatedImage.localRotation = Quaternion.Euler(0f, 0f, entryRotation);
            }

            if (confettiRoot != null)
            {
                confettiRoot.gameObject.SetActive(false);
            }

            activeSequence = DOTween.Sequence()
                .SetTarget(this)
                .SetUpdate(true)
                .Append(canvasGroup.DOFade(1f, entryDuration * 0.7f).SetEase(Ease.OutCubic));

            if (animatedImage != null)
            {
                activeSequence.Join(animatedImage.DOAnchorPos(homePosition, entryDuration).SetEase(Ease.OutCubic));
                activeSequence.Join(animatedImage.DOLocalRotate(Vector3.zero, entryDuration).SetEase(Ease.OutCubic));
                activeSequence.Join(animatedImage.DOScale(homeScale, entryDuration).SetEase(Ease.OutBack));
                activeSequence.Append(animatedImage.DOAnchorPos(homePosition + Vector2.right * breakOvershoot, breakDuration)
                    .SetEase(Ease.OutQuad));
                activeSequence.Join(animatedImage.DOLocalRotate(new Vector3(0f, 0f, -breakRotation), breakDuration)
                    .SetEase(Ease.OutQuad));
                activeSequence.Append(animatedImage.DOAnchorPos(homePosition, settleDuration).SetEase(Ease.OutBack));
                activeSequence.Join(animatedImage.DOLocalRotate(Vector3.zero, settleDuration).SetEase(Ease.OutBack));
            }

            activeSequence.OnComplete(EnableConfetti);
        }

        public void Hide(bool immediate = false)
        {
            activeSequence?.Kill();

            if (immediate || canvasGroup == null)
            {
                ResetVisualState();
                root.SetActive(false);
                return;
            }

            canvasGroup.DOFade(0f, 0.15f)
                .SetUpdate(true)
                .SetTarget(this)
                .OnComplete(() =>
                {
                    ResetVisualState();
                    root.SetActive(false);
                });
        }

        private void OnRestartClicked()
        {
            restartAction?.Invoke();
        }

        private void EnableConfetti()
        {
            if (confettiRoot != null)
            {
                confettiRoot.gameObject.SetActive(true);
            }
        }

        private void ResetVisualState()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }

            if (animatedImage != null)
            {
                animatedImage.anchoredPosition = homePosition;
                animatedImage.localScale = homeScale;
                animatedImage.localRotation = homeRotation;
            }

            if (confettiRoot != null)
            {
                confettiRoot.gameObject.SetActive(false);
            }
        }
    }
}
