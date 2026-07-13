using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SatietyGame
{
    public sealed class MiniGameCounterView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField, Min(1f)] private float punchScale = 1.18f;
        [SerializeField, Min(0.01f)] private float fadeDuration = 0.16f;
        [SerializeField, Min(0.01f)] private float punchDuration = 0.18f;

        private RectTransform rectTransform;
        private Vector3 homeScale;
        private int value;

        private void Awake()
        {
            if (root == null)
            {
                root = gameObject;
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            rectTransform = transform as RectTransform;
            homeScale = rectTransform != null ? rectTransform.localScale : transform.localScale;
            Hide(true);
        }

        private void Reset()
        {
            root = gameObject;
            canvasGroup = GetComponent<CanvasGroup>();
            icon = GetComponentInChildren<Image>();
            valueText = GetComponentInChildren<TMP_Text>();
        }

        public void Show(int startValue = 0)
        {
            value = startValue;
            ApplyValue();

            if (root != null)
            {
                root.SetActive(true);
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.DOFade(1f, fadeDuration).SetEase(Ease.OutQuad).SetUpdate(true).SetTarget(this);
            }
        }

        public void Hide(bool immediate = false)
        {
            DOTween.Kill(this);

            if (immediate || canvasGroup == null)
            {
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                }

                if (root != null)
                {
                    root.SetActive(false);
                }

                return;
            }

            canvasGroup.DOFade(0f, fadeDuration)
                .SetEase(Ease.InQuad)
                .SetUpdate(true)
                .SetTarget(this)
                .OnComplete(() =>
                {
                    if (root != null)
                    {
                        root.SetActive(false);
                    }
                });
        }

        public void SetValue(int newValue)
        {
            value = newValue;
            ApplyValue();
            PlayPunch();
        }

        private void ApplyValue()
        {
            if (valueText != null)
            {
                valueText.text = value.ToString();
            }

            if (icon != null)
            {
                icon.color = normalColor;
            }
        }

        private void PlayPunch()
        {
            Transform target = rectTransform != null ? rectTransform : transform;
            target.DOKill();
            target.localScale = homeScale;
            target.DOPunchScale(Vector3.one * (punchScale - 1f), punchDuration, 7, 0.7f)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
        }
    }
}
