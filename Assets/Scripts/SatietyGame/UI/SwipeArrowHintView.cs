using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace SatietyGame
{
    public sealed class SwipeArrowHintView : MonoBehaviour
    {
        [Header("Arrow Images")]
        [SerializeField] private Image leftArrow;
        [SerializeField] private Image rightArrow;

        [Header("Shader Fade")]
        [SerializeField] private string fadeProperty = "_DirectionalAlphaFadeFade";
        [SerializeField] private float hiddenFade = -20f;
        [SerializeField] private float visibleFade = 0f;
        [SerializeField, Min(0.01f)] private float updateDuration = 0.08f;

        private Material leftMaterial;
        private Material rightMaterial;

        private void Awake()
        {
            leftMaterial = CreateMaterial(leftArrow);
            rightMaterial = CreateMaterial(rightArrow);
            ResetArrows(true);
        }

        public void SetDrag(Vector2 delta, float commitDistance)
        {
            float horizontal = delta.x;
            float strength = commitDistance <= 0f
                ? 0f
                : Mathf.Clamp01(Mathf.Abs(horizontal) / commitDistance);
            float fade = Mathf.Lerp(hiddenFade, visibleFade, strength);

            SetFade(leftMaterial, horizontal < 0f ? fade : hiddenFade);
            SetFade(rightMaterial, horizontal > 0f ? fade : hiddenFade);
        }

        public void ShowAction(CardAction action)
        {
            ShowAction(action, PlayerSide.Player);
        }

        public void ShowAction(CardAction action, PlayerSide actingSide)
        {
            bool receiverIsPlayer = action == CardAction.EatSelf
                ? actingSide == PlayerSide.Player
                : actingSide == PlayerSide.Bot;
            bool left = receiverIsPlayer;
            AnimateFade(left ? leftMaterial : rightMaterial, visibleFade);
            AnimateFade(left ? rightMaterial : leftMaterial, hiddenFade);
        }

        public void ResetArrows(bool immediate = false)
        {
            if (immediate)
            {
                SetFade(leftMaterial, hiddenFade);
                SetFade(rightMaterial, hiddenFade);
                return;
            }

            AnimateFade(leftMaterial, hiddenFade);
            AnimateFade(rightMaterial, hiddenFade);
        }

        private Material CreateMaterial(Image image)
        {
            if (image == null || image.material == null)
            {
                return null;
            }

            Material material = new Material(image.material)
            {
                name = image.material.name + " (Swipe Arrow Runtime)"
            };
            image.material = material;
            return material;
        }

        private void SetFade(Material material, float value)
        {
            if (material != null && !string.IsNullOrWhiteSpace(fadeProperty) && material.HasProperty(fadeProperty))
            {
                material.SetFloat(fadeProperty, value);
            }
        }

        private void AnimateFade(Material material, float value)
        {
            if (material == null || !material.HasProperty(fadeProperty))
            {
                return;
            }

            DOTween.To(
                    () => material.GetFloat(fadeProperty),
                    current => material.SetFloat(fadeProperty, current),
                    value,
                    updateDuration)
                .SetEase(Ease.OutCubic)
                .SetTarget(this)
                .SetUpdate(true);
        }

        private void OnDestroy()
        {
            DOTween.Kill(this);
            if (leftMaterial != null) Destroy(leftMaterial);
            if (rightMaterial != null) Destroy(rightMaterial);
        }
    }
}
