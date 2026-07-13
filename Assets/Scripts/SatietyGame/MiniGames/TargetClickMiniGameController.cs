using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SatietyGame
{
    public sealed class TargetClickMiniGameController : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private RectTransform targetContainer;
        [SerializeField] private RectTransform targetPrefab;
        [SerializeField] private MiniGameProgressBarView progressBar;
        [SerializeField] private MiniGameCounterView playerCounter;
        [SerializeField] private MiniGameCounterView botCounter;
        [SerializeField, Min(1)] private int scoreToWin = 5;
        [SerializeField, Min(1)] private int targetsPerWave = 3;
        [SerializeField, Min(0.05f)] private float waveIntervalSeconds = 1.4f;
        [SerializeField] private Vector2 spawnPadding = new Vector2(80f, 80f);
        [SerializeField] private RectTransform spawnFocusArea;
        [SerializeField] private Vector2 focusExtraPadding = new Vector2(70f, 90f);
        [SerializeField] private Vector2 botPopIntervalRange = new Vector2(0.75f, 1.25f);
        [SerializeField, Min(0.2f)] private float maxMiniGameDuration = 12f;

        [Header("Target Feedback")]
        [SerializeField, Min(1f)] private float targetHoverScale = 1.12f;
        [SerializeField, Min(0.01f)] private float hoverScaleDuration = 0.08f;

        [Header("Click Effect")]
        [SerializeField] private GameObject clickEffectPrefab;
        [SerializeField] private RectTransform uiEffectParent;
        [SerializeField, Min(0.01f)] private float effectLifetimeSeconds = 1.5f;

        private readonly List<GameObject> spawnedTargets = new List<GameObject>();
        private readonly List<GameObject> spawnedEffects = new List<GameObject>();
        private int playerScore;
        private int botScore;
        private bool miniGameRunning;

        private void Awake()
        {
            SetVisible(false);
        }

        public void SetSpawnFocusArea(RectTransform focusArea)
        {
            spawnFocusArea = focusArea;
        }

        public IEnumerator Play(Action<PlayerSide> completed)
        {
            if (targetContainer == null || targetPrefab == null)
            {
                yield return new WaitForSeconds(UnityEngine.Random.Range(botPopIntervalRange.x, botPopIntervalRange.y));
                completed?.Invoke(UnityEngine.Random.value < 0.5f ? PlayerSide.Player : PlayerSide.Bot);
                yield break;
            }

            SetVisible(true);
            ClearTargets();
            ClearEffects();
            playerScore = 0;
            botScore = 0;
            miniGameRunning = true;
            progressBar?.Initialize(scoreToWin);
            playerCounter?.Show(0);
            botCounter?.Show(0);

            float elapsed = 0f;
            float nextWaveTime = 0f;
            float nextBotPopTime = GetNextBotPopDelay();

            while (miniGameRunning && elapsed < maxMiniGameDuration)
            {
                if (elapsed >= nextWaveTime)
                {
                    SpawnTargetWave();
                    nextWaveTime = elapsed + waveIntervalSeconds;
                }

                if (elapsed >= nextBotPopTime)
                {
                    BotPopTarget();
                    nextBotPopTime = elapsed + GetNextBotPopDelay();
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            PlayerSide winner = playerScore >= botScore ? PlayerSide.Player : PlayerSide.Bot;
            miniGameRunning = false;
            ClearTargets();
            SetVisible(false);
            completed?.Invoke(winner);
        }

        private void SpawnTargetWave()
        {
            for (int i = 0; i < targetsPerWave; i++)
            {
                SpawnTarget();
            }
        }

        private void SpawnTarget()
        {
            RectTransform target = Instantiate(targetPrefab, targetContainer);
            target.gameObject.SetActive(true);
            target.anchoredPosition = GetRandomAnchoredPosition();
            spawnedTargets.Add(target.gameObject);

            Graphic graphic = target.GetComponent<Graphic>();
            if (graphic != null)
            {
                graphic.raycastTarget = true;
            }

            MiniGameTargetInteraction interaction = target.GetComponent<MiniGameTargetInteraction>();
            if (interaction == null)
            {
                interaction = target.gameObject.AddComponent<MiniGameTargetInteraction>();
            }

            interaction.Initialize(targetHoverScale, hoverScaleDuration);
            interaction.Clicked += ClickTarget;
        }

        private void ClickTarget(RectTransform target, Vector2 screenPosition)
        {
            if (!miniGameRunning || target == null || !target.gameObject.activeSelf)
            {
                return;
            }

            SpawnClickEffect(screenPosition);
            RemoveTarget(target.gameObject);
            AddScore(PlayerSide.Player);
        }

        private void BotPopTarget()
        {
            if (!miniGameRunning)
            {
                return;
            }

            AddScore(PlayerSide.Bot);
        }

        private void AddScore(PlayerSide side)
        {
            if (side == PlayerSide.Player)
            {
                playerScore++;
                playerCounter?.SetValue(playerScore);
            }
            else
            {
                botScore++;
                botCounter?.SetValue(botScore);
            }

            progressBar?.SetValue(playerScore, botScore, scoreToWin);
            if (playerScore >= scoreToWin || botScore >= scoreToWin)
            {
                miniGameRunning = false;
            }
        }

        private float GetNextBotPopDelay()
        {
            float min = Mathf.Min(botPopIntervalRange.x, botPopIntervalRange.y);
            float max = Mathf.Max(botPopIntervalRange.x, botPopIntervalRange.y);
            return UnityEngine.Random.Range(min, max);
        }

        private void RemoveTarget(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            spawnedTargets.Remove(target);
            target.SetActive(false);
            Destroy(target);
        }

        private Vector2 GetRandomAnchoredPosition()
        {
            Rect rect = GetSpawnRect();
            float x = UnityEngine.Random.Range(rect.xMin + spawnPadding.x, rect.xMax - spawnPadding.x);
            float y = UnityEngine.Random.Range(rect.yMin + spawnPadding.y, rect.yMax - spawnPadding.y);
            return new Vector2(x, y);
        }

        private Rect GetSpawnRect()
        {
            if (spawnFocusArea == null || targetContainer == null)
            {
                return targetContainer.rect;
            }

            Vector3[] corners = new Vector3[4];
            spawnFocusArea.GetWorldCorners(corners);

            Vector2 min = WorldToTargetContainerLocal(corners[0]);
            Vector2 max = min;
            for (int i = 1; i < corners.Length; i++)
            {
                Vector2 local = WorldToTargetContainerLocal(corners[i]);
                min = Vector2.Min(min, local);
                max = Vector2.Max(max, local);
            }

            min -= focusExtraPadding;
            max += focusExtraPadding;

            Rect containerRect = targetContainer.rect;
            min = Vector2.Max(min, containerRect.min);
            max = Vector2.Min(max, containerRect.max);

            if (max.x - min.x < spawnPadding.x * 2f)
            {
                min.x = containerRect.xMin;
                max.x = containerRect.xMax;
            }

            if (max.y - min.y < spawnPadding.y * 2f)
            {
                min.y = containerRect.yMin;
                max.y = containerRect.yMax;
            }

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private Vector2 WorldToTargetContainerLocal(Vector3 worldPosition)
        {
            Canvas canvas = targetContainer.GetComponentInParent<Canvas>();
            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(camera, worldPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(targetContainer, screenPoint, camera, out Vector2 localPoint);
            return localPoint;
        }

        private void ClearTargets()
        {
            for (int i = 0; i < spawnedTargets.Count; i++)
            {
                if (spawnedTargets[i] != null)
                {
                    Destroy(spawnedTargets[i]);
                }
            }

            spawnedTargets.Clear();
        }

        private void ClearEffects()
        {
            for (int i = 0; i < spawnedEffects.Count; i++)
            {
                if (spawnedEffects[i] != null)
                {
                    Destroy(spawnedEffects[i]);
                }
            }

            spawnedEffects.Clear();
        }

        private void SpawnClickEffect(Vector2 screenPosition)
        {
            if (clickEffectPrefab == null)
            {
                return;
            }

            GameObject effect = SpawnUiEffect(screenPosition);
            if (effect == null)
            {
                return;
            }

            spawnedEffects.Add(effect);
            Destroy(effect, effectLifetimeSeconds);
        }

        private GameObject SpawnUiEffect(Vector2 screenPosition)
        {
            RectTransform parent = ResolveUiEffectParent();
            if (parent == null)
            {
                Debug.LogWarning("Click effect needs a UI parent. Assign UI Effect Parent or put the mini game under a Canvas.", this);
                return null;
            }

            GameObject effect = Instantiate(clickEffectPrefab, parent);
            RectTransform rectTransform = effect.transform as RectTransform;

            if (rectTransform != null)
            {
                Canvas canvas = parent.GetComponentInParent<Canvas>();
                Camera canvasCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPosition, canvasCamera, out Vector2 localPoint);
                rectTransform.anchoredPosition = localPoint;
                rectTransform.localScale = Vector3.one;
            }
            else
            {
                effect.transform.position = screenPosition;
                effect.transform.localScale = Vector3.one;
            }

            return effect;
        }

        private RectTransform ResolveUiEffectParent()
        {
            if (uiEffectParent != null)
            {
                return uiEffectParent;
            }

            if (targetContainer != null)
            {
                Canvas canvas = targetContainer.GetComponentInParent<Canvas>();
                if (canvas != null && canvas.transform is RectTransform canvasRectTransform)
                {
                    return canvasRectTransform;
                }
            }

            return targetContainer;
        }

        private void SetVisible(bool visible)
        {
            if (root != null)
            {
                root.SetActive(visible);
            }

            if (!visible)
            {
                playerCounter?.Hide();
                botCounter?.Hide();
            }
        }

        private sealed class MiniGameTargetInteraction : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
        {
            private RectTransform rectTransform;
            private Vector3 normalScale = Vector3.one;
            private Vector3 hoverScale = Vector3.one;
            private float scaleDuration = 0.08f;
            private Coroutine scaleRoutine;
            private bool clicked;

            public event Action<RectTransform, Vector2> Clicked;

            private void Awake()
            {
                rectTransform = transform as RectTransform;
                normalScale = transform.localScale;
            }

            public void Initialize(float hoverMultiplier, float duration)
            {
                rectTransform = transform as RectTransform;
                normalScale = transform.localScale;
                hoverScale = normalScale * hoverMultiplier;
                scaleDuration = Mathf.Max(0.01f, duration);
                clicked = false;
            }

            public void OnPointerEnter(PointerEventData eventData)
            {
                if (!clicked)
                {
                    AnimateScale(hoverScale);
                }
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                if (!clicked)
                {
                    AnimateScale(normalScale);
                }
            }

            public void OnPointerClick(PointerEventData eventData)
            {
                if (clicked)
                {
                    return;
                }

                clicked = true;
                Clicked?.Invoke(rectTransform, eventData.position);
            }

            private void AnimateScale(Vector3 targetScale)
            {
                if (scaleRoutine != null)
                {
                    StopCoroutine(scaleRoutine);
                }

                scaleRoutine = StartCoroutine(ScaleTo(targetScale));
            }

            private IEnumerator ScaleTo(Vector3 targetScale)
            {
                Vector3 startScale = transform.localScale;
                float elapsed = 0f;

                while (elapsed < scaleDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / scaleDuration);
                    transform.localScale = Vector3.Lerp(startScale, targetScale, t);
                    yield return null;
                }

                transform.localScale = targetScale;
                scaleRoutine = null;
            }
        }
    }
}
