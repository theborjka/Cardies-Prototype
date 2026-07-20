using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using System;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SatietyGame
{
    public sealed class UIClickFeedbackView : MonoBehaviour
    {
        [SerializeField] private GameObject effectPrefab;
        [SerializeField] private RectTransform effectParent;
        [SerializeField] private Canvas targetCanvas;
        [SerializeField, Min(0.01f)] private float duration = 0.42f;
        [SerializeField, Min(0.01f)] private float maxClickDuration = 0.35f;
        [SerializeField, Min(0f)] private float maxClickDistance = 28f;
        [SerializeField, Min(0.01f)] private float startScale = 0.35f;
        [SerializeField, Min(0.01f)] private float endScale = 1.25f;
        [SerializeField] private Ease scaleEase = Ease.OutCubic;
        [SerializeField] private Ease fadeEase = Ease.OutQuad;

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                HandlePointer(Mouse.current.leftButton.wasPressedThisFrame,
                    Mouse.current.leftButton.wasReleasedThisFrame,
                    Mouse.current.position.ReadValue());
            }

            if (Touchscreen.current != null)
            {
                var touch = Touchscreen.current.primaryTouch;
                HandlePointer(touch.press.wasPressedThisFrame, touch.press.wasReleasedThisFrame, touch.position.ReadValue());
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            HandlePointer(UnityEngine.Input.GetMouseButtonDown(0), UnityEngine.Input.GetMouseButtonUp(0), UnityEngine.Input.mousePosition);
#endif
        }

        public event Action<Vector2> ClickCompleted;

        private Vector2 pointerStartPosition;
        private float pointerStartTime;
        private bool pointerHeld;

        private void HandlePointer(bool began, bool ended, Vector2 position)
        {
            if (began)
            {
                pointerStartPosition = position;
                pointerStartTime = Time.unscaledTime;
                pointerHeld = true;
                return;
            }

            if (!ended || !pointerHeld)
            {
                return;
            }

            pointerHeld = false;
            if (Time.unscaledTime - pointerStartTime <= maxClickDuration
                && Vector2.Distance(pointerStartPosition, position) <= maxClickDistance)
            {
                ShowAtScreenPosition(position);
                ClickCompleted?.Invoke(position);
            }
        }

        public void ShowAtScreenPosition(Vector2 screenPosition)
        {
            if (effectPrefab == null)
            {
                return;
            }

            RectTransform parent = effectParent != null
                ? effectParent
                : effectPrefab.transform.parent as RectTransform;
            if (parent == null)
            {
                return;
            }

            GameObject effectObject = Instantiate(effectPrefab, parent);
            RectTransform effect = effectObject.transform as RectTransform;
            if (effect == null)
            {
                Destroy(effectObject);
                return;
            }

            effectObject.SetActive(true);
            Canvas canvas = targetCanvas != null ? targetCanvas : parent.GetComponentInParent<Canvas>();
            Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parent,
                    screenPosition,
                    eventCamera,
                    out Vector2 localPosition))
            {
                Destroy(effectObject);
                return;
            }

            effect.anchoredPosition = localPosition;
            effect.localScale = Vector3.one * startScale;

            CanvasGroup group = effect.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = effect.gameObject.AddComponent<CanvasGroup>();
            }

            group.alpha = 1f;
            Sequence sequence = DOTween.Sequence().SetTarget(effect).SetUpdate(true);
            sequence.Join(effect.DOScale(endScale, duration).SetEase(scaleEase));
            sequence.Join(group.DOFade(0f, duration).SetEase(fadeEase));
            sequence.OnComplete(() =>
            {
                if (effect != null)
                {
                    Destroy(effect.gameObject);
                }
            });
        }
    }
}
