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
        [SerializeField] private Color vomitColor = new Color(1f, 0.3f, 0.22f);
        [SerializeField] private Color recoveryColor = new Color(0.55f, 1f, 0.35f);
        [SerializeField] private Vector2 positionOffset = new Vector2(0f, 54f);
        [SerializeField] private Vector2 leftSidePositionOffset = new Vector2(18f, 54f);
        [SerializeField] private Vector2 rightSidePositionOffset = new Vector2(-18f, 54f);
        [SerializeField] private Vector2 exitDownOffset = new Vector2(0f, -34f);
        [SerializeField] private Vector2 entryOffset = new Vector2(0f, -18f);
        [SerializeField, Min(0.01f)] private float entryDuration = 0.22f;
        [SerializeField, Min(0.01f)] private float holdDuration = 0.25f;
        [SerializeField, Min(0.01f)] private float exitDuration = 0.28f;
        [SerializeField] private float entryRotation = 7f;
        [Header("Message Motion")]
        [SerializeField] private Color messageColor = Color.white;
        [SerializeField, Min(0.01f)] private float messageAppearDuration = 0.28f;
        [SerializeField, Min(0.01f)] private float messagePauseDuration = 0.24f;
        [SerializeField, Min(0.01f)] private float messageFlightDuration = 0.62f;
        [SerializeField] private float messageStartScale = 0.42f;
        [SerializeField] private float messagePeakScale = 1.14f;
        [SerializeField] private float messageRotation = -4f;

        private Vector2 homePosition;
        private Vector2 startingPosition;
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
                startingPosition = homePosition;
                homeScale = animatedRoot.localScale;
                homeRotation = animatedRoot.localRotation;
            }

            HideImmediate();
        }

        public void ShowDelta(int delta, RectTransform positionTarget)
        {
            ShowVomitChange(delta, positionTarget);
        }

        public void ShowVomitChangeAtStart(int delta)
        {
            if (delta == 0 || valueText == null || animatedRoot == null)
            {
                return;
            }

            activeSequence?.Kill();
            root.SetActive(true);
            valueText.text = delta > 0 ? $"+{delta} до ригачіни" : $"+{-delta} відновлено";
            valueText.color = delta > 0 ? vomitColor : recoveryColor;
            canvasGroup.alpha = 0f;
            animatedRoot.anchoredPosition = startingPosition;
            animatedRoot.localScale = homeScale * 0.8f;
            animatedRoot.localRotation = homeRotation;

            activeSequence = DOTween.Sequence().SetTarget(this).SetUpdate(true)
                .Append(canvasGroup.DOFade(1f, entryDuration).SetEase(Ease.OutCubic))
                .Join(animatedRoot.DOScale(homeScale, entryDuration).SetEase(Ease.OutBack))
                .Append(animatedRoot.DOPunchScale(Vector3.one * 0.06f, 0.18f, 6, 0.7f))
                .AppendInterval(holdDuration)
                .Append(animatedRoot.DOAnchorPos(startingPosition + exitDownOffset, exitDuration).SetEase(Ease.InCubic))
                .Join(canvasGroup.DOFade(0f, exitDuration).SetEase(Ease.InCubic))
                .OnComplete(HideImmediate);
        }

        public void ShowMessage(string message, RectTransform positionTarget)
        {
            if (string.IsNullOrEmpty(message) || valueText == null || positionTarget == null || animatedRoot == null)
            {
                return;
            }

            activeSequence?.Kill();
            root.SetActive(true);
            valueText.text = message;
            valueText.color = messageColor;
            homePosition = GetTargetPosition(positionTarget);
            canvasGroup.alpha = 0f;

            Vector2 centerPosition = GetCanvasCenterPosition();
            animatedRoot.anchoredPosition = centerPosition;
            animatedRoot.localScale = homeScale * messageStartScale;
            animatedRoot.localRotation = Quaternion.Euler(0f, 0f, messageRotation);

            activeSequence = DOTween.Sequence()
                .SetTarget(this)
                .SetUpdate(true)
                .Append(canvasGroup.DOFade(1f, messageAppearDuration * 0.7f).SetEase(Ease.OutCubic))
                .Join(animatedRoot.DOScale(homeScale * messagePeakScale, messageAppearDuration).SetEase(Ease.OutBack))
                .AppendInterval(messagePauseDuration)
                .Append(animatedRoot.DOAnchorPos(homePosition, messageFlightDuration).SetEase(Ease.InOutCubic))
                .Join(animatedRoot.DOScale(homeScale, messageFlightDuration).SetEase(Ease.OutBack))
                .Join(animatedRoot.DOLocalRotate(homeRotation.eulerAngles, messageFlightDuration).SetEase(Ease.OutCubic))
                .Append(animatedRoot.DOPunchScale(Vector3.one * 0.06f, 0.18f, 6, 0.7f))
                .AppendInterval(0.55f)
                .Append(canvasGroup.DOFade(0f, exitDuration).SetEase(Ease.InCubic))
                .OnComplete(HideImmediate);
        }

        public void ShowStaticMessage(string message, Vector2 screenPosition)
        {
            if (string.IsNullOrEmpty(message) || valueText == null || animatedRoot == null)
            {
                return;
            }

            RectTransform parent = animatedRoot.parent as RectTransform;
            Canvas canvas = animatedRoot.GetComponentInParent<Canvas>();
            Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
            if (parent == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPosition, eventCamera, out Vector2 localPosition))
            {
                return;
            }

            activeSequence?.Kill();
            root.SetActive(true);
            valueText.text = message;
            valueText.color = messageColor;
            canvasGroup.alpha = 0f;
            animatedRoot.anchoredPosition = localPosition;
            animatedRoot.localScale = homeScale * 0.78f;
            animatedRoot.localRotation = homeRotation;

            activeSequence = DOTween.Sequence()
                .SetTarget(this)
                .SetUpdate(true)
                .Append(canvasGroup.DOFade(1f, 0.16f).SetEase(Ease.OutCubic))
                .Join(animatedRoot.DOScale(homeScale, 0.22f).SetEase(Ease.OutBack))
                .AppendInterval(0.65f)
                .Append(canvasGroup.DOFade(0f, 0.2f).SetEase(Ease.InCubic))
                .OnComplete(HideImmediate);
        }

        private Vector2 GetCanvasCenterPosition()
        {
            RectTransform parent = animatedRoot.parent as RectTransform;
            Canvas canvas = animatedRoot.GetComponentInParent<Canvas>();
            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
            if (parent == null || canvasRect == null)
            {
                return Vector2.zero;
            }

            return parent.InverseTransformPoint(canvasRect.TransformPoint(canvasRect.rect.center));
        }

        public void ShowVomitChange(int delta, RectTransform positionTarget)
        {
            if (delta == 0 || valueText == null || positionTarget == null)
            {
                return;
            }

            activeSequence?.Kill();
            root.SetActive(true);
            bool targetIsLeftSide = IsLeftSideTarget(positionTarget);
            homePosition = GetTargetPosition(positionTarget)
                + (targetIsLeftSide ? leftSidePositionOffset : rightSidePositionOffset);
            valueText.text = delta > 0 ? $"+{delta} до ригачіни" : $"+{-delta} відновлено";
            valueText.color = delta > 0 ? vomitColor : recoveryColor;
            valueText.alignment = targetIsLeftSide ? TextAlignmentOptions.Left : TextAlignmentOptions.Right;
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

        private bool IsLeftSideTarget(RectTransform target)
        {
            Canvas canvas = target.GetComponentInParent<Canvas>();
            Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
            Vector2 targetScreenPosition = RectTransformUtility.WorldToScreenPoint(eventCamera, target.position);
            return targetScreenPosition.x < Screen.width * 0.5f;
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
