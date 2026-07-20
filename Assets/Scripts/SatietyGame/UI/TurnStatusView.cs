using DG.Tweening;
using TMPro;
using UnityEngine;

namespace SatietyGame
{
    public sealed class TurnStatusView : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
        [SerializeField, Range(1f, 1.5f)] private float peakScale = 1.12f;
        [SerializeField, Min(0.01f)] private float appearDuration = 0.46f;
        [SerializeField, Min(0.01f)] private float pauseDuration = 0.32f;
        [SerializeField, Min(0.01f)] private float flightDuration = 0.62f;

        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Vector2 homePosition;
        private Vector3 homeScale;
        private Quaternion homeRotation;
        private bool initialized;
        private Sequence sequence;

        public Tween Show(string message)
        {
            return Show(message, PlayerSide.Player);
        }

        public Tween Show(string message, PlayerSide side)
        {
            Initialize();
            if (text == null || rectTransform == null || canvasGroup == null)
            {
                return null;
            }

            sequence?.Kill();
            text.text = message;
            rectTransform.anchoredPosition = GetScreenCenterPosition();
            rectTransform.localScale = homeScale * 0.42f;
            rectTransform.localRotation = Quaternion.Euler(0f, 0f, -4f);
            canvasGroup.alpha = 0f;

            sequence = DOTween.Sequence().SetTarget(this).SetUpdate(true)
                .Append(canvasGroup.DOFade(1f, appearDuration * 0.65f).SetEase(Ease.OutCubic))
                .Join(rectTransform.DOScale(homeScale * peakScale, appearDuration).SetEase(Ease.OutBack))
                .AppendInterval(pauseDuration)
                .Append(rectTransform.DOAnchorPos(homePosition, flightDuration).SetEase(Ease.InOutCubic))
                .Join(rectTransform.DOScale(homeScale, flightDuration).SetEase(Ease.OutBack))
                .Join(rectTransform.DOLocalRotateQuaternion(homeRotation, flightDuration).SetEase(Ease.OutCubic))
                .Append(rectTransform.DOPunchScale(Vector3.one * 0.055f, 0.2f, 7, 0.7f));

            return sequence;
        }

        private void Initialize()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            if (text == null)
            {
                text = GetComponent<TMP_Text>();
            }

            if (text == null)
            {
                return;
            }

            rectTransform = text.transform as RectTransform;
            homePosition = rectTransform.anchoredPosition;
            homeScale = rectTransform.localScale;
            homeRotation = rectTransform.localRotation;
            canvasGroup = text.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = text.gameObject.AddComponent<CanvasGroup>();
            }
        }

        private Vector2 GetScreenCenterPosition()
        {
            RectTransform parent = rectTransform.parent as RectTransform;
            Canvas canvas = rectTransform.GetComponentInParent<Canvas>();
            if (parent == null || canvas == null)
            {
                return homePosition;
            }

            Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenCenter, eventCamera, out Vector2 localCenter)
                ? localCenter
                : homePosition;
        }
    }
}
