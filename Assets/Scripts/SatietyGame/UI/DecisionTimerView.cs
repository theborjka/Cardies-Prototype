using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SatietyGame
{
    public sealed class DecisionTimerView : MonoBehaviour
    {
        [SerializeField] private Image radialFill;
        [SerializeField] private Slider slider;
        [SerializeField] private TMP_Text label;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform animatedRoot;
        [SerializeField, Range(0.5f, 1f)] private float hiddenScale = 0.82f;
        [SerializeField, Min(0.01f)] private float visibilityDuration = 0.16f;

        private Vector3 homeScale = Vector3.one;
        private Tween visibilityTween;

        private void Awake()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            if (animatedRoot == null)
            {
                animatedRoot = transform as RectTransform;
            }

            if (animatedRoot != null)
            {
                homeScale = animatedRoot.localScale;
            }

            Hide(true);
        }

        public void Show()
        {
            visibilityTween?.Kill();
            gameObject.SetActive(true);
            canvasGroup.alpha = 0f;

            if (animatedRoot != null)
            {
                animatedRoot.localScale = homeScale * hiddenScale;
            }

            Sequence sequence = DOTween.Sequence()
                .SetTarget(this)
                .SetUpdate(true)
                .Append(canvasGroup.DOFade(1f, visibilityDuration).SetEase(Ease.OutCubic));

            if (animatedRoot != null)
            {
                sequence.Join(animatedRoot.DOScale(homeScale, visibilityDuration).SetEase(Ease.OutBack));
            }

            visibilityTween = sequence;
        }

        public void Hide(bool immediate = false)
        {
            visibilityTween?.Kill();

            if (immediate)
            {
                canvasGroup.alpha = 0f;
                if (animatedRoot != null)
                {
                    animatedRoot.localScale = homeScale;
                }

                return;
            }

            Sequence sequence = DOTween.Sequence()
                .SetTarget(this)
                .SetUpdate(true)
                .Append(canvasGroup.DOFade(0f, visibilityDuration).SetEase(Ease.InCubic));

            if (animatedRoot != null)
            {
                sequence.Join(animatedRoot.DOScale(homeScale * hiddenScale, visibilityDuration).SetEase(Ease.InBack));
            }

            visibilityTween = sequence;
        }

        public void SetTime(float remaining, float duration)
        {
            float normalized = duration <= 0f ? 0f : Mathf.Clamp01(remaining / duration);

            if (radialFill != null)
            {
                radialFill.fillAmount = normalized;
            }

            if (slider != null)
            {
                slider.minValue = 0f;
                slider.maxValue = duration;
                slider.value = remaining;
            }

            if (label != null)
            {
                label.text = Mathf.CeilToInt(remaining).ToString();
            }
        }
    }
}
