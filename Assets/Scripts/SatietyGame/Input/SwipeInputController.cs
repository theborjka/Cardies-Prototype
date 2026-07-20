using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SatietyGame
{
    public sealed class SwipeInputController : MonoBehaviour
    {
        [SerializeField, Min(10f)] private float minSwipeDistance = 80f;
        [SerializeField, Range(0f, 1f)] private float directionThreshold = 0.55f;
        [SerializeField, Min(10f)] private float arrowRevealDistance = 240f;
        [SerializeField] private SwipeArrowHintView swipeArrowHint;
        [SerializeField] private UIClickFeedbackView clickFeedback;

        public event Action<CardAction> ActionSelected;
        public event Action DragStarted;
        public event Action<Vector2> DragUpdated;
        public event Action DragCanceled;

        public float MinSwipeDistance => minSwipeDistance;

        private bool inputEnabled;
        private Vector2 startPosition;
        private bool isPressing;

        private void Awake()
        {
            if (swipeArrowHint == null)
            {
                swipeArrowHint = FindAnyObjectByType<SwipeArrowHintView>();
            }

            if (clickFeedback == null)
            {
                clickFeedback = FindAnyObjectByType<UIClickFeedbackView>();
            }
        }

        public void EnableInput()
        {
            inputEnabled = true;
            isPressing = false;
            swipeArrowHint?.ResetArrows(true);
        }

        public void DisableInput()
        {
            inputEnabled = false;
            isPressing = false;
        }

        public void ResetSwipeHint()
        {
            swipeArrowHint?.ResetArrows();
        }

        public void ShowActionHint(CardAction action, PlayerSide actingSide = PlayerSide.Player)
        {
            swipeArrowHint?.ShowAction(action, actingSide);
        }

        private void Update()
        {
            if (!inputEnabled)
            {
                return;
            }

#if ENABLE_INPUT_SYSTEM
            if (HandleMouseInput())
            {
                return;
            }

            HandleTouchInput();
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (UnityEngine.Input.touchCount > 0)
            {
                Touch touch = UnityEngine.Input.GetTouch(0);
                HandlePointer(touch.phase == TouchPhase.Began, touch.phase == TouchPhase.Ended, touch.position);
                return;
            }

            HandlePointer(UnityEngine.Input.GetMouseButtonDown(0), UnityEngine.Input.GetMouseButtonUp(0), UnityEngine.Input.mousePosition);
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private bool HandleMouseInput()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return false;
            }

            bool began = mouse.leftButton.wasPressedThisFrame;
            bool ended = mouse.leftButton.wasReleasedThisFrame;
            bool pressed = mouse.leftButton.isPressed;
            Vector2 position = mouse.position.ReadValue();

            if (pressed && !began && !ended)
            {
                HandlePointer(false, false, position);
                return true;
            }

            if (!began && !ended)
            {
                return false;
            }

            HandlePointer(began, ended, position);
            return true;
        }

        private void HandleTouchInput()
        {
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen == null)
            {
                return;
            }

            var touch = touchscreen.primaryTouch;
            bool began = touch.press.wasPressedThisFrame;
            bool ended = touch.press.wasReleasedThisFrame;
            bool pressed = touch.press.isPressed;
            Vector2 position = touch.position.ReadValue();

            if (pressed && !began && !ended)
            {
                HandlePointer(false, false, position);
                return;
            }

            if (!began && !ended)
            {
                return;
            }

            HandlePointer(began, ended, position);
        }
#endif

        private void HandlePointer(bool began, bool ended, Vector2 position)
        {
            if (began)
            {
                startPosition = position;
                isPressing = true;
                DragStarted?.Invoke();
                return;
            }

            if (!ended && isPressing)
            {
                Vector2 dragDelta = position - startPosition;
                swipeArrowHint?.SetDrag(dragDelta, arrowRevealDistance);
                DragUpdated?.Invoke(dragDelta);
                return;
            }

            if (!ended || !isPressing)
            {
                return;
            }

            isPressing = false;
            Vector2 delta = position - startPosition;
            if (delta.magnitude < minSwipeDistance)
            {
                DragCanceled?.Invoke();
                return;
            }

            Vector2 direction = delta.normalized;
            if (direction.x <= -directionThreshold)
            {
                swipeArrowHint?.ShowAction(CardAction.EatSelf, PlayerSide.Player);
                Select(CardAction.EatSelf);
            }
            else if (direction.x >= directionThreshold)
            {
                swipeArrowHint?.ShowAction(CardAction.FeedOpponent, PlayerSide.Player);
                Select(CardAction.FeedOpponent);
            }
        }

        private void Select(CardAction action)
        {
            DisableInput();
            ActionSelected?.Invoke(action);
        }
    }
}
