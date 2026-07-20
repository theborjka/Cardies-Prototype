using DG.Tweening;
using TMPro;
using UnityEngine;

namespace SatietyGame
{
    public sealed class NotYourTurnAnnouncerView : MonoBehaviour
    {
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private string message = "Not your turn";
        [SerializeField, Min(0.01f)] private float showDuration = 0.2f;
        [SerializeField, Min(0.01f)] private float holdDuration = 0.65f;
        [SerializeField, Min(0.01f)] private float hideDuration = 0.22f;
        [SerializeField] private Vector2 startScale = new Vector2(0.78f, 0.78f);

        public void Show()
        {
            if (messageText != null)
            {
                Canvas canvas = messageText.GetComponentInParent<Canvas>();
                Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                    ? canvas.worldCamera
                    : null;
                ShowAtScreenPosition(RectTransformUtility.WorldToScreenPoint(eventCamera, messageText.transform.position));
            }
        }

        public void ShowAtScreenPosition(Vector2 screenPosition)
        {
            if (messageText == null)
            {
                return;
            }

            RectTransform rect = messageText.transform as RectTransform;
            RectTransform parent = rect != null ? rect.parent as RectTransform : null;
            Canvas canvas = messageText.GetComponentInParent<Canvas>();
            Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
            if (rect == null || parent == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPosition, eventCamera, out Vector2 localPosition))
            {
                return;
            }

            if (canvasGroup == null)
            {
                canvasGroup = messageText.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = messageText.gameObject.AddComponent<CanvasGroup>();
                }
            }

            DOTween.Kill(this);
            messageText.text = message;
            rect.anchoredPosition = localPosition;
            rect.localScale = startScale;
            canvasGroup.alpha = 0f;
            Sequence sequence = DOTween.Sequence().SetTarget(this).SetUpdate(true)
                .Append(canvasGroup.DOFade(1f, showDuration).SetEase(Ease.OutCubic))
                .Join(rect.DOScale(Vector3.one, showDuration).SetEase(Ease.OutBack))
                .AppendInterval(holdDuration)
                .Append(canvasGroup.DOFade(0f, hideDuration).SetEase(Ease.InCubic));
        }

        public void Configure(TMP_Text text)
        {
            messageText = text;
        }
    }
}
