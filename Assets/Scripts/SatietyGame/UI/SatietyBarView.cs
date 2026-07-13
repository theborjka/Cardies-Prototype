using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SatietyGame
{
    public sealed class SatietyBarView : MonoBehaviour
    {
        [SerializeField] private Image fillImage;
        [SerializeField] private Slider slider;
        [SerializeField] private TMP_Text label;
        [SerializeField] private RectTransform animatedRoot;
        [SerializeField, Min(0.01f)] private float defaultAnimationDuration = 0.45f;
        [SerializeField, Min(0.01f)] private float overfillDuration = 0.24f;
        [SerializeField, Min(0.01f)] private float overfillDrainDuration = 1.05f;

        private Tween activeTween;

        private void Awake()
        {
            if (animatedRoot == null)
            {
                animatedRoot = transform as RectTransform;
            }
        }

        public void SetValue(int current, int max)
        {
            activeTween?.Kill();
            ApplyValue(current, max);
        }

        public Tween SetValueAnimated(int from, int to, int max, float duration = -1f)
        {
            activeTween?.Kill();

            float resolvedDuration = duration > 0f ? duration : defaultAnimationDuration;
            float displayedValue = from;
            ApplyValue(displayedValue, max);

            activeTween = DOTween.To(
                    () => displayedValue,
                    value =>
                    {
                        displayedValue = value;
                        ApplyValue(displayedValue, max);
                    },
                    (float)to,
                    resolvedDuration)
                .SetEase(Ease.InOutCubic)
                .SetUpdate(true);

            return activeTween;
        }

        public Tween PlayOverfillAndDrain(int from, int attempted, int finalValue, int max)
        {
            activeTween?.Kill();

            float displayedValue = from;
            ApplyValue(displayedValue, max);

            Sequence sequence = DOTween.Sequence()
                .SetTarget(this)
                .SetUpdate(true)
                .Append(DOTween.To(
                        () => displayedValue,
                        value =>
                        {
                            displayedValue = value;
                            ApplyValue(displayedValue, max);
                        },
                        (float)attempted,
                        overfillDuration)
                    .SetEase(Ease.OutBack));

            if (animatedRoot != null)
            {
                sequence.Join(animatedRoot.DOPunchScale(Vector3.one * 0.08f, overfillDuration, 7, 0.7f));
                sequence.Join(animatedRoot.DOShakeAnchorPos(overfillDuration, new Vector2(5f, 2f), 14, 70f, false, true));
            }

            activeTween = sequence
                .Append(DOTween.To(
                        () => displayedValue,
                        value =>
                        {
                            displayedValue = value;
                            ApplyValue(displayedValue, max);
                        },
                        (float)finalValue,
                        overfillDrainDuration)
                    .SetEase(Ease.InOutCubic));

            return activeTween;
        }

        private void ApplyValue(float current, int max)
        {
            float normalized = max <= 0 ? 0f : Mathf.Clamp01((float)current / max);

            if (fillImage != null)
            {
                fillImage.fillAmount = normalized;
            }

            if (slider != null)
            {
                slider.minValue = 0f;
                slider.maxValue = max;
                slider.value = current;
            }

            if (label != null)
            {
                label.text = $"{Mathf.RoundToInt(current)}/{max}";
            }
        }
    }
}
