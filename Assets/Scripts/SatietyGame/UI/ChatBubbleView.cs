using DG.Tweening;
using TMPro;
using UnityEngine;

namespace SatietyGame
{
    public sealed class ChatBubbleView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform animatedRoot;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Vector2 positionOffset = new Vector2(0f, 72f);
        [SerializeField] private Vector2 entryOffset = new Vector2(0f, -22f);
        [SerializeField] private float entryRotation = -4f;
        [SerializeField, Min(0.01f)] private float entryDuration = 0.28f;
        [SerializeField, Min(0.01f)] private float holdPopDuration = 0.14f;
        [SerializeField, Min(0.01f)] private float exitDuration = 0.18f;
        [SerializeField, Range(1f, 1.08f)] private float breathScale = 1.015f;
        [SerializeField, Min(0.1f)] private float breathDuration = 1.25f;

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

            Hide(true);
        }

        public void ShowMessage(string message, RectTransform positionTarget)
        {
            if (positionTarget == null)
            {
                return;
            }

            activeSequence?.Kill();
            root.SetActive(true);
            homePosition = GetTargetPosition(positionTarget) + positionOffset;
            if (messageText != null)
            {
                messageText.text = message;
            }
            canvasGroup.alpha = 0f;

            if (animatedRoot != null)
            {
                animatedRoot.anchoredPosition = homePosition + entryOffset;
                animatedRoot.localScale = homeScale * 0.78f;
                animatedRoot.localRotation = Quaternion.Euler(0f, 0f, entryRotation);
            }

            activeSequence = DOTween.Sequence()
                .SetTarget(this)
                .SetUpdate(true)
                .Append(canvasGroup.DOFade(1f, entryDuration * 0.7f).SetEase(Ease.OutCubic));

            if (animatedRoot != null)
            {
                activeSequence.Join(animatedRoot.DOAnchorPos(homePosition, entryDuration).SetEase(Ease.OutBack));
                activeSequence.Join(animatedRoot.DOLocalRotate(homeRotation.eulerAngles, entryDuration).SetEase(Ease.OutBack));
                activeSequence.Join(animatedRoot.DOScale(homeScale, entryDuration).SetEase(Ease.OutBack));
                activeSequence.Append(animatedRoot.DOPunchScale(Vector3.one * 0.06f, holdPopDuration, 7, 0.7f));
                activeSequence.Append(animatedRoot.DOScale(homeScale * breathScale, breathDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo));
            }
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

        public void Hide(bool immediate = false)
        {
            activeSequence?.Kill();

            if (immediate)
            {
                ResetVisualState();
                root.SetActive(false);
                return;
            }

            Sequence exitSequence = DOTween.Sequence()
                .SetTarget(this)
                .SetUpdate(true)
                .Append(canvasGroup.DOFade(0f, exitDuration).SetEase(Ease.InCubic));

            if (animatedRoot != null)
            {
                exitSequence.Join(animatedRoot.DOAnchorPos(homePosition + Vector2.up * 10f, exitDuration)
                    .SetEase(Ease.InCubic));
                exitSequence.Join(animatedRoot.DOScale(homeScale * 0.84f, exitDuration)
                    .SetEase(Ease.InBack));
                exitSequence.Join(animatedRoot.DOLocalRotate(new Vector3(0f, 0f, 3f), exitDuration)
                    .SetEase(Ease.InCubic));
            }

            exitSequence.OnComplete(() =>
                {
                    ResetVisualState();
                    root.SetActive(false);
                });
        }

        private void ResetVisualState()
        {
            canvasGroup.alpha = 0f;

            if (animatedRoot != null)
            {
                animatedRoot.anchoredPosition = homePosition;
                animatedRoot.localScale = homeScale;
                animatedRoot.localRotation = homeRotation;
            }
        }
    }
}
