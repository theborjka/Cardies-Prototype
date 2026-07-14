using DG.Tweening;
using TMPro;
using UnityEngine;

namespace SatietyGame
{
    public sealed class SatietyChangeAnnouncerView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform animatedRoot;
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private Color positiveColor = new Color(0.55f, 1f, 0.35f);
        [SerializeField] private Color negativeColor = new Color(1f, 0.3f, 0.22f);
        [SerializeField] private Vector2 positionOffset = new Vector2(0f, 54f);
        [SerializeField] private Vector2 entryOffset = new Vector2(0f, -18f);
        [SerializeField, Min(0.01f)] private float entryDuration = 0.22f;
        [SerializeField, Min(0.01f)] private float holdDuration = 0.25f;
        [SerializeField, Min(0.01f)] private float exitDuration = 0.28f;
        [SerializeField] private float entryRotation = 7f;

        private Vector2 homePosition;
        private Vector3 homeScale;
        private Quaternion homeRotation;
        private Sequence activeSequence;

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

            if (animatedRoot == null)
            {
                animatedRoot = transform as RectTransform;
            }

            if (animatedRoot != null)
            {
                homePosition = animatedRoot.anchoredPosition;
                homeScale = animatedRoot.localScale;
                homeRotation = animatedRoot.localRotation;
            }

            HideImmediate();
        }

        public void ShowDelta(int delta, RectTransform positionTarget)
        {
            if (delta == 0 || valueText == null || positionTarget == null)
            {
                return;
            }

            activeSequence?.Kill();
            root.SetActive(true);
            homePosition = GetTargetPosition(positionTarget) + positionOffset;
            valueText.text = delta > 0 ? $"+{delta}" : delta.ToString();
            valueText.color = delta > 0 ? positiveColor : negativeColor;
            canvasGroup.alpha = 0f;

            if (animatedRoot != null)
            {
                float side = delta > 0 ? 1f : -1f;
                animatedRoot.anchoredPosition = homePosition + entryOffset;
                animatedRoot.localScale = homeScale * 0.65f;
                animatedRoot.localRotation = Quaternion.Euler(0f, 0f, side * entryRotation);
            }

            activeSequence = DOTween.Sequence()
                .SetTarget(this)
                .SetUpdate(true)
                .Append(canvasGroup.DOFade(1f, entryDuration).SetEase(Ease.OutCubic));

            if (animatedRoot != null)
            {
                activeSequence.Join(animatedRoot.DOAnchorPos(homePosition, entryDuration).SetEase(Ease.OutBack));
                activeSequence.Join(animatedRoot.DOScale(homeScale * 1.12f, entryDuration).SetEase(Ease.OutBack));
                activeSequence.Join(animatedRoot.DOLocalRotate(homeRotation.eulerAngles, entryDuration).SetEase(Ease.OutBack));
                activeSequence.Append(animatedRoot.DOScale(homeScale, 0.12f).SetEase(Ease.InOutSine));
            }

            activeSequence.AppendInterval(holdDuration);
            activeSequence.Append(canvasGroup.DOFade(0f, exitDuration).SetEase(Ease.InCubic));

            if (animatedRoot != null)
            {
                activeSequence.Join(animatedRoot.DOAnchorPos(homePosition + Vector2.up * 16f, exitDuration)
                    .SetEase(Ease.InCubic));
                activeSequence.Join(animatedRoot.DOScale(homeScale * 0.86f, exitDuration)
                    .SetEase(Ease.InCubic));
            }

            activeSequence.OnComplete(HideImmediate);
        }

        private Vector2 GetTargetPosition(RectTransform target)
        {
            RectTransform parent = animatedRoot != null ? animatedRoot.parent as RectTransform : null;
            if (parent == null)
            {
                return target.anchoredPosition;
            }

            return parent.InverseTransformPoint(target.position);
        }

        private void HideImmediate()
        {
            activeSequence?.Kill();
            canvasGroup.alpha = 0f;
            root.SetActive(false);

            if (animatedRoot != null)
            {
                animatedRoot.anchoredPosition = homePosition;
                animatedRoot.localScale = homeScale;
                animatedRoot.localRotation = homeRotation;
            }
        }
    }
}
