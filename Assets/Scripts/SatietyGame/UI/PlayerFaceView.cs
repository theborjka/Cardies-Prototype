using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace SatietyGame
{
    public sealed class PlayerFaceView : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private PlayerSide side;

        [Header("References")]
        [SerializeField] private Image faceImage;
        [SerializeField] private RectTransform animatedRoot;
        [SerializeField] private RectTransform mouthTarget;
        [SerializeField] private Image poisonEffectImage;

        [Header("Sprites")]
        [SerializeField] private Sprite idleSprite;
        [SerializeField] private Sprite lookingAtFoodSprite;
        [SerializeField] private Sprite openMouthSprite;
        [SerializeField] private Sprite chewSprite;
        [SerializeField] private Sprite badFoodSprite;
        [SerializeField] private Sprite laughSprite;
        [SerializeField] private Sprite angrySprite;
        [SerializeField] private Sprite happySprite;
        [SerializeField] private Sprite pukeSprite;

        [Header("Idle Motion")]
        [SerializeField, Min(0f)] private float idleBobDistance = 5f;
        [SerializeField, Min(0.1f)] private float idleBobDuration = 1.45f;
        [SerializeField, Min(0f)] private float idleTilt = 1.25f;
        [SerializeField, Min(1f)] private float idleBreathScale = 1.025f;

        [Header("Reaction Motion")]
        [SerializeField, Min(0f)] private float lookLeanDistance = 14f;
        [SerializeField, Min(0.01f)] private float lookDuration = 0.22f;
        [SerializeField, Min(0.01f)] private float eatAnticipationDuration = 0.18f;
        [SerializeField, Min(0.01f)] private float chewBeatDuration = 0.12f;
        [SerializeField, Min(1)] private int chewBeats = 4;
        [SerializeField, Min(0.01f)] private float badFoodHoldDuration = 0.9f;
        [SerializeField, Min(0.01f)] private float angryHoldDuration = 0.8f;
        [SerializeField, Min(0.01f)] private float happyHoldDuration = 0.8f;
        [SerializeField, Min(0.01f)] private float pukeDuration = 1.8f;
        [SerializeField, Min(0.01f)] private float poisonFadeDuration = 0.3f;

        [Header("Turn Highlight Shader")]
        [SerializeField] private string turnFadeProperty = "_HologramFade";
        [SerializeField] private string turnOutlineFadeProperty = "_OutlineFade";
        [SerializeField] private float turnFadeInactiveValue = 0f;
        [SerializeField] private float turnFadeActiveValue = 1f;
        [SerializeField, Min(0.01f)] private float turnFadeDuration = 0.2f;

        public PlayerSide Side => side;
        public RectTransform MouthTarget => mouthTarget != null ? mouthTarget : animatedRoot;

        private Vector2 homeAnchoredPosition;
        private Vector3 homeScale;
        private Quaternion homeRotation;
        private Sequence activeSequence;
        private Material poisonMaterial;
        private Material turnMaterial;
        private Tween turnFadeTween;
        private static readonly int PoisonFadeId = Shader.PropertyToID("_PoisonFade");

        private void Awake()
        {
            if (faceImage == null)
            {
                faceImage = GetComponentInChildren<Image>();
            }

            if (animatedRoot == null)
            {
                animatedRoot = transform as RectTransform;
            }

            InitializePoisonMaterial();
            InitializeTurnMaterial();
            CacheHomeTransform();
            SetPoisonFade(0f);
            SetTurnEffect(false, true);
        }

        private void Start()
        {
            // Shader helper components can initialize their material after Awake.
            InitializePoisonMaterial();
            InitializeTurnMaterial();
            SetPoisonFade(0f);
            SetTurnEffect(false, true);
        }

        private void OnEnable()
        {
            PlayIdle();
        }

        private void OnDisable()
        {
            KillMotion();
            SetPoisonFade(0f);
            SetTurnEffect(false, true);
        }

        private void OnDestroy()
        {
            if (poisonMaterial != null)
            {
                Destroy(poisonMaterial);
            }

            if (turnMaterial != null && turnMaterial != poisonMaterial)
            {
                Destroy(turnMaterial);
            }
        }

        public void SetTurnEffect(bool enabled, bool immediate = false)
        {
            InitializeTurnMaterial();
            if (turnMaterial == null)
            {
                return;
            }

            turnFadeTween?.Kill();
            float targetValue = enabled ? turnFadeActiveValue : turnFadeInactiveValue;
            if (immediate)
            {
                SetTurnShaderValue(turnFadeProperty, targetValue);
                SetTurnShaderValue(turnOutlineFadeProperty, targetValue);
                return;
            }

            turnFadeTween = DOTween.To(
                    GetTurnShaderValue,
                    SetTurnShaderValue,
                    targetValue,
                    turnFadeDuration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true);
        }

        private float GetTurnShaderValue()
        {
            if (!string.IsNullOrWhiteSpace(turnFadeProperty) && turnMaterial.HasProperty(turnFadeProperty))
            {
                return turnMaterial.GetFloat(turnFadeProperty);
            }

            if (!string.IsNullOrWhiteSpace(turnOutlineFadeProperty) && turnMaterial.HasProperty(turnOutlineFadeProperty))
            {
                return turnMaterial.GetFloat(turnOutlineFadeProperty);
            }

            return turnFadeInactiveValue;
        }

        private void SetTurnShaderValue(float value)
        {
            SetTurnShaderValue(turnFadeProperty, value);
            SetTurnShaderValue(turnOutlineFadeProperty, value);
        }

        private void SetTurnShaderValue(string propertyName, float value)
        {
            if (turnMaterial != null && !string.IsNullOrWhiteSpace(propertyName) && turnMaterial.HasProperty(propertyName))
            {
                turnMaterial.SetFloat(propertyName, value);
            }
        }

        private void Reset()
        {
            faceImage = GetComponentInChildren<Image>();
            animatedRoot = transform as RectTransform;
        }

        public void PlayIdle()
        {
            KillMotion();
            SetSprite(idleSprite);
            ResetTransform();

            if (animatedRoot == null)
            {
                return;
            }

            activeSequence = DOTween.Sequence()
                .SetTarget(this)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true)
                .Append(animatedRoot.DOAnchorPosY(homeAnchoredPosition.y + idleBobDistance, idleBobDuration).SetEase(Ease.InOutSine))
                .Join(animatedRoot.DOScale(homeScale * idleBreathScale, idleBobDuration).SetEase(Ease.InOutSine))
                .Join(animatedRoot.DOLocalRotate(new Vector3(0f, 0f, GetSideSign() * idleTilt), idleBobDuration).SetEase(Ease.InOutSine));
        }

        public Tween LookAtFood()
        {
            KillMotion();
            SetSprite(lookingAtFoodSprite != null ? lookingAtFoodSprite : idleSprite);
            ResetTransform();

            if (animatedRoot == null)
            {
                return null;
            }

            activeSequence = DOTween.Sequence()
                .SetTarget(this)
                .SetUpdate(true)
                .Append(animatedRoot.DOAnchorPos(homeAnchoredPosition + Vector2.right * GetLookDirection() * lookLeanDistance, lookDuration).SetEase(Ease.OutBack))
                .Join(animatedRoot.DOLocalRotate(new Vector3(0f, 0f, -GetLookDirection() * 3f), lookDuration).SetEase(Ease.OutCubic))
                .Join(animatedRoot.DOScale(homeScale * 1.04f, lookDuration).SetEase(Ease.OutBack))
                .Append(animatedRoot.DOPunchScale(Vector3.one * 0.035f, 0.16f, 6, 0.7f));

            return activeSequence;
        }

        public Tween PlayEatSequence(bool badFood)
        {
            KillMotion();
            ResetTransform();

            if (animatedRoot == null)
            {
                SetSprite(badFood ? badFoodSprite : idleSprite);
                return null;
            }

            activeSequence = DOTween.Sequence()
                .SetTarget(this)
                .SetUpdate(true)
                .AppendCallback(() => SetSprite(openMouthSprite != null ? openMouthSprite : lookingAtFoodSprite))
                .Append(animatedRoot.DOScale(homeScale * 1.15f, eatAnticipationDuration).SetEase(Ease.OutBack))
                .Join(animatedRoot.DOAnchorPos(homeAnchoredPosition + Vector2.down * 8f, eatAnticipationDuration).SetEase(Ease.OutCubic))
                .AppendCallback(() => SetSprite(chewSprite != null ? chewSprite : idleSprite));

            for (int i = 0; i < chewBeats; i++)
            {
                activeSequence.Append(animatedRoot.DOScale(i % 2 == 0 ? homeScale * 1.08f : homeScale * 0.98f, chewBeatDuration).SetEase(Ease.InOutSine))
                    .Join(animatedRoot.DOLocalRotate(new Vector3(0f, 0f, (i % 2 == 0 ? 2.8f : -2.8f) * GetSideSign()), chewBeatDuration).SetEase(Ease.InOutSine));
            }

            if (badFood)
            {
                activeSequence.AppendCallback(() => SetSprite(badFoodSprite != null ? badFoodSprite : angrySprite))
                    .Append(animatedRoot.DOShakeAnchorPos(0.32f, new Vector2(9f, 4f), 16, 80f, false, true).SetEase(Ease.OutQuad))
                    .AppendInterval(badFoodHoldDuration)
                    .AppendCallback(PlayIdle);
            }
            else
            {
                activeSequence.AppendCallback(() => SetSprite(happySprite != null ? happySprite : idleSprite))
                    .Append(animatedRoot.DOPunchScale(Vector3.one * 0.08f, 0.22f, 7, 0.65f))
                    .AppendCallback(PlayIdle);
            }

            return activeSequence;
        }

        public Tween OpenMouthForIncomingFood()
        {
            KillMotion();
            ResetTransform();
            SetSprite(openMouthSprite != null ? openMouthSprite : lookingAtFoodSprite);

            if (animatedRoot == null)
            {
                return null;
            }

            activeSequence = DOTween.Sequence()
                .SetTarget(this)
                .SetUpdate(true)
                .Append(animatedRoot.DOScale(homeScale * 1.12f, eatAnticipationDuration).SetEase(Ease.OutBack))
                .Join(animatedRoot.DOAnchorPos(homeAnchoredPosition + Vector2.down * 7f, eatAnticipationDuration).SetEase(Ease.OutCubic));

            return activeSequence;
        }

        public Tween ChewAfterFoodArrives(bool badFood)
        {
            KillMotion();
            ResetTransform();

            if (animatedRoot == null)
            {
                SetSprite(badFood ? badFoodSprite : idleSprite);
                return null;
            }

            SetSprite(chewSprite != null ? chewSprite : idleSprite);

            activeSequence = DOTween.Sequence()
                .SetTarget(this)
                .SetUpdate(true);

            for (int i = 0; i < chewBeats; i++)
            {
                activeSequence.Append(animatedRoot.DOScale(i % 2 == 0 ? homeScale * 1.08f : homeScale * 0.98f, chewBeatDuration).SetEase(Ease.InOutSine))
                    .Join(animatedRoot.DOLocalRotate(new Vector3(0f, 0f, (i % 2 == 0 ? 2.8f : -2.8f) * GetSideSign()), chewBeatDuration).SetEase(Ease.InOutSine));
            }

            if (badFood)
            {
                activeSequence.AppendCallback(() => SetSprite(badFoodSprite != null ? badFoodSprite : angrySprite))
                    .Append(animatedRoot.DOShakeAnchorPos(0.32f, new Vector2(9f, 4f), 16, 80f, false, true).SetEase(Ease.OutQuad))
                    .AppendInterval(badFoodHoldDuration)
                    .AppendCallback(PlayIdle);
            }
            else
            {
                activeSequence.AppendCallback(() => SetSprite(happySprite != null ? happySprite : idleSprite))
                    .Append(animatedRoot.DOPunchScale(Vector3.one * 0.08f, 0.22f, 7, 0.65f))
                    .AppendCallback(PlayIdle);
            }

            return activeSequence;
        }

        public Tween PlayMiniGameLost()
        {
            KillMotion();
            ResetTransform();
            SetSprite(angrySprite != null ? angrySprite : idleSprite);

            if (animatedRoot == null)
            {
                return null;
            }

            activeSequence = DOTween.Sequence()
                .SetTarget(this)
                .SetUpdate(true)
                .Append(animatedRoot.DOShakeAnchorPos(0.36f, new Vector2(14f, 4f), 22, 85f, false, true).SetEase(Ease.OutQuad))
                .Join(animatedRoot.DOScale(homeScale * 1.08f, 0.14f).SetEase(Ease.OutBack))
                .AppendInterval(angryHoldDuration)
                .Append(animatedRoot.DOAnchorPos(
                    homeAnchoredPosition + Vector2.right * GetSideSign() * 3f,
                    0.52f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo))
                .Join(animatedRoot.DOLocalRotate(
                    new Vector3(0f, 0f, GetSideSign() * 1.8f),
                    0.52f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo));

            return activeSequence;
        }

        public Tween PlayMiniGameWon()
        {
            KillMotion();
            ResetTransform();
            SetSprite(happySprite != null ? happySprite : idleSprite);

            if (animatedRoot == null)
            {
                return null;
            }

            activeSequence = DOTween.Sequence()
                .SetTarget(this)
                .SetUpdate(true)
                .Append(animatedRoot.DOPunchAnchorPos(Vector2.up * 12f, 0.28f, 8, 0.65f))
                .Join(animatedRoot.DOPunchScale(Vector3.one * 0.09f, 0.28f, 8, 0.7f))
                .AppendInterval(happyHoldDuration)
                .AppendCallback(PlayIdle);

            return activeSequence;
        }

        public Tween PlayLaughReaction()
        {
            KillMotion();
            ResetTransform();
            SetSprite(laughSprite != null ? laughSprite : happySprite != null ? happySprite : idleSprite);

            if (animatedRoot == null)
            {
                return null;
            }

            activeSequence = DOTween.Sequence()
                .SetTarget(this)
                .SetUpdate(true)
                .Append(animatedRoot.DOPunchScale(Vector3.one * 0.1f, 0.24f, 8, 0.75f))
                .Join(animatedRoot.DOPunchRotation(new Vector3(0f, 0f, GetSideSign() * 5f), 0.24f, 8, 0.7f))
                .Append(animatedRoot.DOShakeAnchorPos(0.85f, new Vector2(7f, 3f), 18, 65f, false, true)
                    .SetEase(Ease.OutQuad))
                .Join(animatedRoot.DOScale(homeScale * 1.04f, 0.85f).SetEase(Ease.InOutSine))
                .AppendInterval(0.25f)
                .AppendCallback(PlayIdle);

            return activeSequence;
        }

        public void ShowFinalVictory()
        {
            PlayFinalResult(happySprite != null ? happySprite : idleSprite, true);
        }

        public void ShowFinalDefeat()
        {
            PlayFinalResult(angrySprite != null ? angrySprite : idleSprite, false);
        }

        private void PlayFinalResult(Sprite resultSprite, bool victory)
        {
            KillMotion();
            ResetTransform();
            SetSprite(resultSprite);

            if (animatedRoot == null)
            {
                return;
            }

            activeSequence = DOTween.Sequence()
                .SetTarget(this)
                .SetUpdate(true)
                .Append(animatedRoot.DOPunchScale(Vector3.one * (victory ? 0.1f : 0.07f), 0.28f, 8, 0.7f));

            float lean = victory ? 2f : 3.5f;
            float offset = victory ? 4f : 3f;
            activeSequence.Append(animatedRoot.DOScale(homeScale * (victory ? 1.025f : 1.015f), 0.7f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo));
            activeSequence.Join(animatedRoot.DOLocalRotate(new Vector3(0f, 0f, GetSideSign() * lean), 0.7f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo));
            activeSequence.Join(animatedRoot.DOAnchorPos(homeAnchoredPosition + Vector2.right * GetSideSign() * offset, 0.7f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo));
        }

        public Tween PlayOvereatReaction()
        {
            KillMotion();
            ResetTransform();
            SetSprite(pukeSprite != null ? pukeSprite : badFoodSprite);

            if (animatedRoot == null)
            {
                return null;
            }

            activeSequence = DOTween.Sequence()
                .SetTarget(this)
                .SetUpdate(true)
                .AppendCallback(() => SetPoisonFade(0f))
                .Append(animatedRoot.DOShakeAnchorPos(0.55f, new Vector2(18f, 8f), 28, 88f, false, true).SetEase(Ease.OutQuad))
                .Join(animatedRoot.DOScale(homeScale * 1.12f, 0.2f).SetEase(Ease.OutBack))
                .Join(FadePoison(1f, poisonFadeDuration))
                .Append(animatedRoot.DOAnchorPos(homeAnchoredPosition + Vector2.down * 12f, 0.32f).SetEase(Ease.InOutSine))
                .Join(animatedRoot.DOLocalRotate(new Vector3(0f, 0f, -GetSideSign() * 8f), 0.32f).SetEase(Ease.InOutSine))
                .AppendInterval(Mathf.Max(0.05f, pukeDuration - 0.55f - 0.32f - 0.38f))
                .Append(animatedRoot.DOShakeAnchorPos(0.38f, new Vector2(10f, 4f), 18, 75f, false, true).SetEase(Ease.OutQuad))
                .Join(FadePoison(0f, poisonFadeDuration))
                .AppendCallback(PlayIdle);

            return activeSequence;
        }

        public Tween PlayFoodArrivalPop()
        {
            if (animatedRoot == null)
            {
                return null;
            }

            DOTween.Kill(this);
            Vector2 currentPosition = animatedRoot.anchoredPosition;
            Vector3 currentScale = animatedRoot.localScale;
            Quaternion currentRotation = animatedRoot.localRotation;

            return DOTween.Sequence()
                .SetTarget(this)
                .SetUpdate(true)
                .Append(animatedRoot.DOPunchScale(Vector3.one * 0.045f, 0.18f, 7, 0.7f).SetEase(Ease.OutBack))
                .Join(animatedRoot.DOPunchAnchorPos(Vector2.up * 5f, 0.18f, 6, 0.7f))
                .OnComplete(() =>
                {
                    animatedRoot.anchoredPosition = currentPosition;
                    animatedRoot.localScale = currentScale;
                    animatedRoot.localRotation = currentRotation;
                });
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

        private void ResetTransform()
        {
            if (animatedRoot == null)
            {
                return;
            }

            animatedRoot.anchoredPosition = homeAnchoredPosition;
            animatedRoot.localScale = homeScale;
            animatedRoot.localRotation = homeRotation;
        }

        private void SetSprite(Sprite sprite)
        {
            if (faceImage != null && sprite != null)
            {
                faceImage.sprite = sprite;
            }
        }

        private void InitializePoisonMaterial()
        {
            if (poisonEffectImage == null)
            {
                poisonEffectImage = faceImage;
            }

            if (poisonEffectImage == null || poisonEffectImage.material == null ||
                !poisonEffectImage.material.HasProperty(PoisonFadeId))
            {
                return;
            }

            if (poisonMaterial == poisonEffectImage.material)
            {
                return;
            }

            if (poisonMaterial != null)
            {
                Destroy(poisonMaterial);
            }

            poisonMaterial = new Material(poisonEffectImage.material)
            {
                name = poisonEffectImage.material.name + " (Runtime Instance)"
            };
            poisonEffectImage.material = poisonMaterial;
        }

        private void InitializeTurnMaterial()
        {
            if (faceImage == null || faceImage.material == null || string.IsNullOrWhiteSpace(turnFadeProperty)
                || !faceImage.material.HasProperty(turnFadeProperty))
            {
                return;
            }

            if (poisonEffectImage == faceImage && poisonMaterial != null && poisonMaterial.HasProperty(turnFadeProperty))
            {
                turnMaterial = poisonMaterial;
                return;
            }

            if (turnMaterial == faceImage.material)
            {
                return;
            }

            if (turnMaterial != null)
            {
                Destroy(turnMaterial);
            }

            turnMaterial = new Material(faceImage.material)
            {
                name = faceImage.material.name + " (Turn Runtime Instance)"
            };
            faceImage.material = turnMaterial;
        }

        private Tween FadePoison(float fade, float duration)
        {
            if (poisonMaterial == null)
            {
                return DOTween.Sequence().SetUpdate(true);
            }

            return DOTween.To(GetPoisonFade, SetPoisonFade, Mathf.Clamp01(fade), duration)
                .SetEase(Ease.InOutQuad)
                .SetUpdate(true)
                .SetTarget(this);
        }

        private float GetPoisonFade()
        {
            return poisonMaterial != null && poisonMaterial.HasProperty(PoisonFadeId)
                ? poisonMaterial.GetFloat(PoisonFadeId)
                : 0f;
        }

        private void SetPoisonFade(float fade)
        {
            if (poisonMaterial == null)
            {
                return;
            }

            float clampedFade = Mathf.Clamp01(fade);
            poisonMaterial.SetFloat(PoisonFadeId, clampedFade);

            Material renderingMaterial = poisonEffectImage != null ? poisonEffectImage.materialForRendering : null;
            if (renderingMaterial != null && renderingMaterial != poisonMaterial && renderingMaterial.HasProperty(PoisonFadeId))
            {
                renderingMaterial.SetFloat(PoisonFadeId, clampedFade);
            }
        }

        private void KillMotion()
        {
            activeSequence?.Kill();
            DOTween.Kill(this);
        }

        private float GetSideSign()
        {
            return side == PlayerSide.Player ? 1f : -1f;
        }

        private float GetLookDirection()
        {
            return side == PlayerSide.Player ? 1f : -1f;
        }
    }
}
