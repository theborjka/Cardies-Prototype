using UnityEngine;
using UnityEngine.UI;

namespace SatietyGame
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class SwipeHintView : MonoBehaviour
    {
        [SerializeField] private Sprite leftArrowSprite;
        [SerializeField] private Sprite fingerSprite;
        [SerializeField] private Sprite rightArrowSprite;
        [SerializeField] private Vector2 leftArrowPosition = new Vector2(-214f, 0f);
        [SerializeField] private Vector2 fingerPosition = new Vector2(0f, -6f);
        [SerializeField] private Vector2 rightArrowPosition = new Vector2(214f, 0f);
        [SerializeField] private Vector2 arrowSize = new Vector2(150f, 82f);
        [SerializeField] private Vector2 fingerSize = new Vector2(150f, 184f);

        private void Awake()
        {
            Image legacyImage = GetComponent<Image>();
            if (legacyImage != null && legacyImage.sprite != null)
            {
                // Keep the authored combined arrows/finger artwork when it exists.
                legacyImage.enabled = true;
                return;
            }

            CreateImage("Left Arrow", leftArrowSprite, leftArrowPosition, arrowSize);
            CreateImage("Finger", fingerSprite, fingerPosition, fingerSize);
            CreateImage("Right Arrow", rightArrowSprite, rightArrowPosition, arrowSize);
        }

        private void CreateImage(string objectName, Sprite sprite, Vector2 position, Vector2 size)
        {
            if (sprite == null)
            {
                return;
            }

            GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(transform, false);

            RectTransform imageTransform = imageObject.transform as RectTransform;
            imageTransform.anchorMin = new Vector2(0.5f, 0.5f);
            imageTransform.anchorMax = new Vector2(0.5f, 0.5f);
            imageTransform.pivot = new Vector2(0.5f, 0.5f);
            imageTransform.anchoredPosition = position;
            imageTransform.sizeDelta = size;

            Image image = imageObject.GetComponent<Image>();
            image.sprite = sprite;
            image.enabled = true;
            image.preserveAspect = true;
            image.raycastTarget = false;
            imageObject.transform.SetAsLastSibling();
        }
    }
}
