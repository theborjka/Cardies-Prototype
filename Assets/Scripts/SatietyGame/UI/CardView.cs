using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SatietyGame
{
    public sealed class CardView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text satietyText;
        [SerializeField] private RectTransform targetSpawnArea;

        [Header("Motion")]
        [SerializeField] private RectTransform animatedRoot;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField, Min(0f)] private float previewMaxOffset = 110f;
        [SerializeField, Min(0f)] private float previewMaxTilt = 14f;
        [SerializeField, Min(1f)] private float previewMaxScale = 1.045f;
        [SerializeField, Min(0.01f)] private float previewFollowDuration = 0.08f;
        [SerializeField, Min(0.01f)] private float returnDuration = 0.22f;
        [SerializeField, Min(0.01f)] private float showDuration = 0.32f;
        [SerializeField, Min(0.01f)] private float choiceFeedbackDuration = 0.24f;
        [SerializeField, Min(0f)] private float choiceFeedbackOffset = 52f;
        [SerializeField, Min(0f)] private float choiceFeedbackTilt = 16f;
        [SerializeField, Min(0f)] private float holdFeedbackTiltX = 10f;
        [SerializeField, Min(0.01f)] private float actionDuration = 0.42f;
        [SerializeField, Min(0f)] private float actionOvershoot = 120f;
        [SerializeField, Min(0.01f)] private float foodFlyDuration = 0.48f;
        [SerializeField, Min(0.01f)] private float foodEjectDuration = 0.14f;
        [SerializeField, Range(0.1f, 1.5f)] private float foodEjectScale = 0.9f;
        [SerializeField] private Vector2 foodEjectOffset = new Vector2(0f, 24f);
        [SerializeField, Range(0.1f, 1f)] private float foodFlyStartScale = 0.72f;
        [SerializeField, Range(0.05f, 1f)] private float foodFlyEndScale = 0.34f;
        [SerializeField, Min(0.01f)] private float foodArrivalPopDuration = 0.1f;
        [SerializeField, Range(0.1f, 1.5f)] private float foodArrivalPopScale = 0.48f;
        [SerializeField, Min(0.01f)] private float cardDisappearDuration = 0.2f;
        [SerializeField] private Ease showEase = Ease.OutBack;
        [SerializeField] private Ease returnEase = Ease.OutCubic;
        [SerializeField] private Ease choiceEase = Ease.OutBack;
        [SerializeField] private Ease actionEase = Ease.InBack;

        [Header("Optional Direction Hints")]
        [SerializeField] private CanvasGroup eatHint;
        [SerializeField] private CanvasGroup passHint;
        [SerializeField] private CanvasGroup holdHint;

        public CardData CurrentCard { get; private set; }
        public RectTransform TargetSpawnArea => targetSpawnArea != null ? targetSpawnArea : animatedRoot;

        private RectTransform rectTransform;
        private Vector2 homeAnchoredPosition;
        private Vector3 homeScale;
        private Quaternion homeRotation;
        private Sequence activeSequence;

        private void Awake()
        {
            if (root == null)
            {
                root = gameObject;
            }

            rectTransform = transform as RectTransform;
            if (animatedRoot == null)
            {
                animatedRoot = rectTransform;
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            CacheHomeTransform();
            SetHints(0f, 0f, 0f);
        }

        private void Reset()
        {
            root = gameObject;
            rectTransform = transform as RectTransform;
            animatedRoot = rectTransform;
            targetSpawnArea = rectTransform;
            canvasGroup = GetComponent<CanvasGroup>();
            iconImage = GetComponentInChildren<Image>();
            titleText = GetComponentInChildren<TMP_Text>();
        }

        public void Show(CardData card)
        {
            CurrentCard = card;
            if (root == null)
            {
                root = gameObject;
            }

            if (root != null)
            {
                root.SetActive(card != null);
            }

            if (card == null)
            {
                ResetMotionState();
                return;
            }

            CacheHomeTransform();
            ResetMotionState();

            if (iconImage != null)
            {
                iconImage.sprite = card.Icon;
                iconImage.enabled = card.Icon != null;
            }
            else
            {
                Debug.LogWarning($"CardView on '{name}' has no icon image assigned.", this);
            }

            if (titleText != null)
            {
                titleText.text = card.Title;
            }
            else
            {
                Debug.LogWarning($"CardView on '{name}' has no title text assigned.", this);
            }

            if (satietyText != null)
            {
                satietyText.text = card.SatietyDescription;
            }
            else
            {
                Debug.LogWarning($"CardView on '{name}' has no satiety text assigned.", this);
            }
        }

        public void Hide()
        {
            Show(null);
        }

        public Tween PlayShow()
        {
            KillMotion();
            SetHints(0f, 0f, 0f);

            if (animatedRoot == null)
            {
                return null;
            }

            animatedRoot.anchoredPosition = homeAnchoredPosition;
            animatedRoot.localScale = homeScale * 0.72f;
            animatedRoot.localRotation = homeRotation;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }

            activeSequence = DOTween.Sequence()
                .SetTarget(this)
                .SetUpdate(true)
                .Append(animatedRoot.DOScale(homeScale, showDuration).SetEase(showEase))
                .Join(animatedRoot.DOPunchAnchorPos(Vector2.up * 10f, showDuration, 6, 0.45f));

            if (canvasGroup != null)
            {
                activeSequence.Join(canvasGroup.DOFade(1f, showDuration * 0.72f).SetEase(Ease.OutQuad));
            }

            return activeSequence;
        }

        public void PreviewSwipe(Vector2 delta, float commitDistance)
        {
            if (animatedRoot == null)
            {
                return;
            }

            KillMotion();

            float strength = commitDistance <= 0f ? 0f : Mathf.Clamp01(delta.magnitude / commitDistance);
            Vector2 offset = Vector2.ClampMagnitude(delta, previewMaxOffset);
            float tilt = Mathf.Clamp(-offset.x / Mathf.Max(1f, previewMaxOffset), -1f, 1f) * previewMaxTilt;
            Vector3 targetScale = Vector3.Lerp(homeScale, homeScale * previewMaxScale, strength);

            animatedRoot.DOAnchorPos(homeAnchoredPosition + offset, previewFollowDuration).SetEase(Ease.OutQuad).SetUpdate(true).SetTarget(this);
            animatedRoot.DOLocalRotate(new Vector3(0f, 0f, tilt), previewFollowDuration).SetEase(Ease.OutQuad).SetUpdate(true).SetTarget(this);
            animatedRoot.DOScale(targetScale, previewFollowDuration).SetEase(Ease.OutQuad).SetUpdate(true).SetTarget(this);

            float eat = delta.x < 0f ? Mathf.Clamp01(-delta.x / commitDistance) : 0f;
            float pass = delta.x > 0f ? Mathf.Clamp01(delta.x / commitDistance) : 0f;
            float hold = delta.y < 0f ? Mathf.Clamp01(-delta.y / commitDistance) : 0f;
            SetHints(eat, pass, hold);
        }

        public Tween CancelSwipePreview()
        {
            KillMotion();
            SetHints(0f, 0f, 0f);

            if (animatedRoot == null)
            {
                return null;
            }

            activeSequence = DOTween.Sequence()
                .SetTarget(this)
                .SetUpdate(true)
                .Append(animatedRoot.DOAnchorPos(homeAnchoredPosition, returnDuration).SetEase(returnEase))
                .Join(animatedRoot.DOLocalRotateQuaternion(homeRotation, returnDuration).SetEase(returnEase))
                .Join(animatedRoot.DOScale(homeScale, returnDuration).SetEase(Ease.OutBack));

            return activeSequence;
        }

        public Tween PlayAction(CardAction action)
        {
            return PlayResolution(null, action);
        }

        public Tween PlayChoiceFeedback(CardAction action)
        {
            KillMotion();
            SetHints(0f, 0f, 0f);

            if (animatedRoot == null)
            {
                return null;
            }

            Vector2 direction = GetActionDirection(action);
            Vector2 feedbackPosition = homeAnchoredPosition + direction * choiceFeedbackOffset;
            Vector3 feedbackRotation = GetChoiceRotation(action);

            activeSequence = DOTween.Sequence()
                .SetTarget(this)
                .SetUpdate(true)
                .Append(animatedRoot.DOAnchorPos(feedbackPosition, choiceFeedbackDuration).SetEase(choiceEase))
                .Join(animatedRoot.DOLocalRotate(feedbackRotation, choiceFeedbackDuration).SetEase(choiceEase))
                .Join(animatedRoot.DOScale(homeScale * previewMaxScale, choiceFeedbackDuration).SetEase(choiceEase));

            return activeSequence;
        }

        public Tween PlayResolution(PlayerSide? receiver, CardAction action)
        {
            KillMotion();
            SetHints(0f, 0f, 0f);

            if (animatedRoot == null)
            {
                return null;
            }

            Vector2 direction = GetResolutionDirection(receiver);
            Vector2 exitPosition = homeAnchoredPosition + GetExitDistance() * direction;
            float exitTilt = direction.x < 0f ? choiceFeedbackTilt * 1.8f : direction.x > 0f ? -choiceFeedbackTilt * 1.8f : choiceFeedbackTilt * 0.65f;

            activeSequence = DOTween.Sequence()
                .SetTarget(this)
                .SetUpdate(true)
                .Append(animatedRoot.DOPunchScale(Vector3.one * 0.06f, 0.12f, 7, 0.7f))
                .Append(animatedRoot.DOAnchorPos(exitPosition, actionDuration).SetEase(actionEase))
                .Join(animatedRoot.DOLocalRotate(new Vector3(0f, 0f, exitTilt), actionDuration).SetEase(Ease.InCubic))
                .Join(animatedRoot.DOScale(homeScale * 0.92f, actionDuration).SetEase(Ease.InQuad));

            if (canvasGroup != null)
            {
                activeSequence.Join(canvasGroup.DOFade(0f, actionDuration * 0.82f).SetEase(Ease.InQuad));
            }

            return activeSequence;
        }

        public Tween PlayFoodFlyToMouth(
            RectTransform mouthTarget,
            Action onMouthReached = null,
            Action onArrivalPop = null)
        {
            if (iconImage == null || iconImage.sprite == null || mouthTarget == null)
            {
                return null;
            }

            RectTransform iconRect = iconImage.transform as RectTransform;
            RectTransform parent = GetTopCanvasRect();
            if (iconRect == null || parent == null)
            {
                return null;
            }

            GameObject cloneObject = new GameObject($"{iconImage.sprite.name} Fly Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform cloneRect = cloneObject.transform as RectTransform;
            Image cloneImage = cloneObject.GetComponent<Image>();
            cloneRect.SetParent(parent, false);
            cloneImage.sprite = iconImage.sprite;
            cloneImage.preserveAspect = iconImage.preserveAspect;
            cloneImage.raycastTarget = false;

            cloneRect.sizeDelta = iconRect.rect.size;
            cloneRect.localScale = Vector3.one * foodFlyStartScale;
            cloneRect.localRotation = Quaternion.identity;
            cloneRect.anchoredPosition = WorldToLocalPoint(parent, iconRect.position);

            Vector2 mouthPosition = WorldToLocalPoint(parent, mouthTarget.position);
            Vector2 startPosition = cloneRect.anchoredPosition;
            Vector2 ejectPosition = startPosition + foodEjectOffset;
            Vector2 arcPoint = Vector2.Lerp(ejectPosition, mouthPosition, 0.5f) + Vector2.up * 80f;

            iconImage.enabled = false;

            Sequence flightSequence = DOTween.Sequence()
                .Append(cloneRect.DOAnchorPos(arcPoint, foodFlyDuration * 0.42f).SetEase(Ease.OutQuad))
                .Append(cloneRect.DOAnchorPos(mouthPosition, foodFlyDuration * 0.58f).SetEase(Ease.InQuad));
            flightSequence.Insert(0f, cloneRect.DOScale(Vector3.one * foodFlyEndScale, foodFlyDuration)
                .SetEase(Ease.InOutCubic));
            flightSequence.Insert(0f, cloneRect.DORotate(new Vector3(0f, 0f, 24f), foodFlyDuration, RotateMode.FastBeyond360)
                .SetEase(Ease.InOutSine));

            Sequence sequence = DOTween.Sequence()
                .SetTarget(this)
                .SetUpdate(true)
                .Append(cloneRect.DOAnchorPos(ejectPosition, foodEjectDuration).SetEase(Ease.OutBack))
                .Join(cloneRect.DOScale(Vector3.one * foodEjectScale, foodEjectDuration).SetEase(Ease.OutBack))
                .Join(cloneRect.DOPunchRotation(new Vector3(0f, 0f, 12f), foodEjectDuration, 6, 0.7f))
                .Join(animatedRoot != null
                    ? animatedRoot.DOPunchScale(Vector3.one * 0.035f, foodEjectDuration, 7, 0.7f)
                    : DOTween.Sequence())
                .Join(animatedRoot != null
                    ? animatedRoot.DOPunchAnchorPos(Vector2.up * 7f, foodEjectDuration, 6, 0.65f)
                    : DOTween.Sequence())
                .Append(flightSequence)
                .AppendCallback(() => onMouthReached?.Invoke())
                .AppendCallback(() => onArrivalPop?.Invoke())
                .Append(cloneRect.DOScale(Vector3.one * foodArrivalPopScale, foodArrivalPopDuration)
                    .SetEase(Ease.OutCubic))
                .OnComplete(() =>
                {
                    if (cloneObject != null)
                    {
                        Destroy(cloneObject);
                    }
                });

            return sequence;
        }

        public Tween PlayDisappearScaleDown()
        {
            KillMotion();
            SetHints(0f, 0f, 0f);

            if (animatedRoot == null)
            {
                return null;
            }

            activeSequence = DOTween.Sequence()
                .SetTarget(this)
                .SetUpdate(true)
                .Append(animatedRoot.DOScale(homeScale * 0.72f, cardDisappearDuration).SetEase(Ease.InBack));

            if (canvasGroup != null)
            {
                activeSequence.Join(canvasGroup.DOFade(0f, cardDisappearDuration).SetEase(Ease.InQuad));
            }

            return activeSequence;
        }

        private void CacheHomeTransform()
        {
            if (animatedRoot == null)
            {
                return;
            }

            homeAnchoredPosition = animatedRoot.anchoredPosition;
            homeScale = animatedRoot.localScale;
            homeRotation = animatedRoot.localRotation;
        }

        private void ResetMotionState()
        {
            KillMotion();

            if (animatedRoot != null)
            {
                animatedRoot.anchoredPosition = homeAnchoredPosition;
                animatedRoot.localScale = homeScale;
                animatedRoot.localRotation = homeRotation;
            }

            if (iconImage != null && CurrentCard != null)
            {
                iconImage.enabled = CurrentCard.Icon != null;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            SetHints(0f, 0f, 0f);
        }

        private void KillMotion()
        {
            activeSequence?.Kill();
            DOTween.Kill(this);
        }

        private Vector2 GetActionDirection(CardAction action)
        {
            switch (action)
            {
                case CardAction.Eat:
                    return Vector2.left;
                case CardAction.Hold:
                    return Vector2.down;
                case CardAction.Pass:
                default:
                    return Vector2.right;
            }
        }

        private Vector3 GetChoiceRotation(CardAction action)
        {
            switch (action)
            {
                case CardAction.Eat:
                    return new Vector3(0f, 0f, choiceFeedbackTilt);
                case CardAction.Pass:
                    return new Vector3(0f, 0f, -choiceFeedbackTilt);
                case CardAction.Hold:
                    return new Vector3(holdFeedbackTiltX, 0f, 0f);
                case CardAction.None:
                default:
                    return Vector3.zero;
            }
        }

        private Vector2 GetResolutionDirection(PlayerSide? receiver)
        {
            if (receiver == PlayerSide.Player)
            {
                return Vector2.left;
            }

            if (receiver == PlayerSide.Bot)
            {
                return Vector2.right;
            }

            return Vector2.up;
        }

        private float GetExitDistance()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
            if (canvasRect == null)
            {
                return 1400f + actionOvershoot;
            }

            return Mathf.Max(canvasRect.rect.width, canvasRect.rect.height) * 0.72f + actionOvershoot;
        }

        private void SetHints(float eat, float pass, float hold)
        {
            SetHint(eatHint, eat);
            SetHint(passHint, pass);
            SetHint(holdHint, hold);
        }

        private void SetHint(CanvasGroup hint, float alpha)
        {
            if (hint != null)
            {
                hint.alpha = alpha;
            }
        }

        private RectTransform GetTopCanvasRect()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            return canvas != null ? canvas.transform as RectTransform : transform.parent as RectTransform;
        }

        private Vector2 WorldToLocalPoint(RectTransform parent, Vector3 worldPosition)
        {
            Canvas canvas = parent.GetComponentInParent<Canvas>();
            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(camera, worldPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, camera, out Vector2 localPoint);
            return localPoint;
        }
    }
}
