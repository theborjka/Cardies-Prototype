using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace SatietyGame
{
    [Serializable] public sealed class CardEvent : UnityEvent<CardData> { }
    [Serializable] public sealed class PlayerEvent : UnityEvent<PlayerSide> { }
    [Serializable] public sealed class SatietyEvent : UnityEvent<PlayerSide, int, int> { }
    [Serializable] public sealed class ActionEvent : UnityEvent<PlayerSide, CardAction> { }

    public sealed class GameController : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private GameConfig config;
        [SerializeField] private PlayerProfileData playerProfile;
        [SerializeField] private PlayerProfileData botProfile;
        [SerializeField, Min(0f)] private float miniGameResultDelaySeconds = 1.15f;

        [Header("Scene References")]
        [SerializeField] private CardView cardView;
        [SerializeField] private RectTransform cardSpawnParent;
        [SerializeField] private SwipeInputController swipeInput;
        [SerializeField] private BotController botController;
        [SerializeField] private TargetClickMiniGameController miniGame;
        [SerializeField] private SatietyBarView playerSatietyBar;
        [SerializeField] private SatietyBarView botSatietyBar;
        [SerializeField] private DecisionTimerView timerView;
        [SerializeField] private BoosterController boosterController;
        [SerializeField] private PlayerFaceView playerFace;
        [SerializeField] private PlayerFaceView botFace;
        [SerializeField] private GameEndScreenView victoryScreen;
        [SerializeField] private GameEndScreenView loseScreen;
        [SerializeField] private ChatBubbleView chatBubble;
        [SerializeField] private AllergyIconsView playerAllergyIcons;
        [SerializeField] private AllergyIconsView botAllergyIcons;
        [SerializeField] private SatietyChangeAnnouncerView satietyAnnouncer;
        [SerializeField] private TMP_Text roundCounterText;
        [SerializeField] private CanvasGroup decisionHint;
        [SerializeField] private string roundCounterFormat = "ROUND {0}";
        [SerializeField, Min(0.01f)] private float decisionHintFadeDuration = 0.24f;
        [SerializeField, Range(1f, 1.08f)] private float decisionHintBreathScale = 1.018f;
        [SerializeField, Min(0.1f)] private float decisionHintBreathDuration = 1.35f;

        [Header("Events")]
        [SerializeField] private CardEvent cardShown;
        [SerializeField] private ActionEvent actionChosen;
        [SerializeField] private ActionEvent actionApplied;
        [SerializeField] private SatietyEvent satietyChanged;
        [SerializeField] private PlayerEvent miniGameStarted;
        [SerializeField] private PlayerEvent gameEnded;

        private readonly CardDeck deck = new CardDeck();
        private PlayerState playerState;
        private PlayerState botState;
        private CardAction pendingPlayerAction = CardAction.None;
        private bool receivedPlayerAction;
        private CardData currentCard;
        private int currentCardSatiety;
        private bool botActionBlocked;
        private Coroutine gameLoop;
        private CardView cardViewPrefab;
        private CardAction resolvedCardAction;
        private PlayerSide? resolvedCardReceiver;
        private bool resolvedEatApplied;
        private bool resolvedEatOverate;
        private int roundNumber;
        private Tween decisionHintTween;
        private Vector3 decisionHintHomeScale = Vector3.one;
        private bool decisionHintScaleCached;

        public PlayerState PlayerState => playerState;
        public PlayerState BotState => botState;
        public CardData CurrentCard => currentCard;

        private void Awake()
        {
            if (swipeInput != null)
            {
                swipeInput.ActionSelected += OnPlayerActionSelected;
                swipeInput.DragUpdated += OnPlayerDragUpdated;
                swipeInput.DragCanceled += OnPlayerDragCanceled;
            }
        }

        private void OnDestroy()
        {
            if (swipeInput != null)
            {
                swipeInput.ActionSelected -= OnPlayerActionSelected;
                swipeInput.DragUpdated -= OnPlayerDragUpdated;
                swipeInput.DragCanceled -= OnPlayerDragCanceled;
            }
        }

        private void Start()
        {
            StartGame();
        }

        public void StartGame()
        {
            if (config == null)
            {
                Debug.LogError("GameConfig is missing.", this);
                return;
            }

            ResolveSceneReferences();
            playerState = new PlayerState(PlayerSide.Player, config.MaxSatiety, playerProfile);
            botState = new PlayerState(PlayerSide.Bot, config.MaxSatiety, botProfile);
            playerState.RollAllergies(config.Cards);
            botState.RollAllergies(config.Cards);
            playerAllergyIcons?.SetAllergies(playerState.AllergicFoods);
            botAllergyIcons?.SetAllergies(botState.AllergicFoods);
            deck.Initialize(config.Cards, config.ShuffleDeck);
            roundNumber = 0;
            UpdateRoundCounter();
            HideDecisionHint(true);
            timerView?.Hide(true);
            chatBubble?.Hide(true);
            victoryScreen?.Hide(true);
            loseScreen?.Hide(true);
            RefreshSatietyViews();
            playerFace?.PlayIdle();
            botFace?.PlayIdle();

            if (gameLoop != null)
            {
                StopCoroutine(gameLoop);
            }

            gameLoop = StartCoroutine(GameLoop());
        }

        public bool TryUsePlayerBooster(BoosterData booster)
        {
            if (boosterController == null)
            {
                return false;
            }

            return boosterController.TryUseBooster(booster, playerState, botState, ref currentCardSatiety, ref botActionBlocked);
        }

        public bool TryPlayHeldCard(PlayerSide side)
        {
            PlayerState state = GetState(side);
            if (state?.HeldCard == null)
            {
                return false;
            }

            CardData heldCard = state.HeldCard;
            state.ClearHeldCard();
            ApplyEat(state, heldCard, heldCard.SatietyValue);
            return true;
        }

        private IEnumerator GameLoop()
        {
            while (!playerState.HasWon && !botState.HasWon)
            {
                yield return PlayRound();
                playerState.TickHeldCardLifetime();
                botState.TickHeldCardLifetime();
                yield return new WaitForSeconds(0.35f);
            }

            PlayerSide winner = playerState.HasWon ? PlayerSide.Player : PlayerSide.Bot;
            gameEnded?.Invoke(winner);
            ShowGameEndScreen(winner);
            if (swipeInput != null)
            {
                swipeInput.DisableInput();
            }
        }

        private IEnumerator PlayRound()
        {
            currentCard = deck.Draw();
            currentCardSatiety = currentCard != null ? currentCard.SatietyValue : 0;
            botActionBlocked = false;
            receivedPlayerAction = false;
            pendingPlayerAction = CardAction.None;
            resolvedCardAction = CardAction.Pass;
            resolvedCardReceiver = null;
            resolvedEatApplied = false;
            resolvedEatOverate = false;

            if (currentCard == null)
            {
                Debug.LogWarning("Deck has no cards to draw. Add CardData assets to GameConfig.", this);
                yield break;
            }

            roundNumber++;
            UpdateRoundCounter();
            chatBubble?.Hide();

            if (cardView != null)
            {
                cardView.Show(currentCard);
                Tween showTween = cardView.PlayShow();
                if (showTween != null)
                {
                    yield return showTween.WaitForCompletion();
                }
            }
            else
            {
                Debug.LogWarning($"Drew card '{currentCard.Title}', but GameController has no CardView assigned/found in the scene.", this);
            }

            cardShown?.Invoke(currentCard);
            playerFace?.LookAtFood();
            botFace?.LookAtFood();
            ShowDecisionHint();
            timerView?.Show();

            if (swipeInput != null)
            {
                swipeInput.EnableInput();
            }

            float remaining = config.DecisionTimeSeconds;
            while (remaining > 0f && !receivedPlayerAction)
            {
                remaining -= Time.deltaTime;
                timerView?.SetTime(remaining, config.DecisionTimeSeconds);
                yield return null;
            }

            if (swipeInput != null)
            {
                swipeInput.DisableInput();
            }

            HideDecisionHint();
            timerView?.Hide();

            CardAction playerAction = receivedPlayerAction ? pendingPlayerAction : CardAction.Pass;
            CardAction botAction = botActionBlocked ? CardAction.Pass : botController != null
                ? botController.ChooseAction(currentCard, botState, playerState)
                : CardAction.Pass;

            playerAction = ValidateAction(playerState, currentCard, playerAction);
            botAction = ValidateAction(botState, currentCard, botAction);

            if (cardView != null)
            {
                Tween choiceTween = cardView.PlayChoiceFeedback(playerAction);
                if (choiceTween != null)
                {
                    yield return choiceTween.WaitForCompletion();
                }
            }

            actionChosen?.Invoke(PlayerSide.Player, playerAction);
            actionChosen?.Invoke(PlayerSide.Bot, botAction);

            yield return ResolveActions(playerAction, botAction);
            yield return PlayResolvedCardVisuals();
            yield return ApplyResolvedAction();

            cardView?.Hide();
        }

        private IEnumerator ResolveActions(CardAction playerAction, CardAction botAction)
        {
            bool playerWantsCard = playerAction == CardAction.Eat || playerAction == CardAction.Hold;
            bool botWantsCard = botAction == CardAction.Eat || botAction == CardAction.Hold;

            if (playerWantsCard && botWantsCard)
            {
                PlayerSide winner = PlayerSide.Bot;
                miniGameStarted?.Invoke(PlayerSide.Player);

                if (miniGame != null)
                {
                    bool completed = false;
                    if (cardView != null)
                    {
                        miniGame.SetSpawnFocusArea(cardView.TargetSpawnArea);
                    }

                    yield return miniGame.Play(result =>
                    {
                        winner = result;
                        completed = true;
                    });

                    while (!completed)
                    {
                        yield return null;
                    }
                }
                else
                {
                    winner = UnityEngine.Random.value < 0.5f ? PlayerSide.Player : PlayerSide.Bot;
                }

                if (winner == PlayerSide.Player)
                {
                    yield return PlayMiniGameResultFaces(PlayerSide.Player);
                    resolvedCardAction = playerAction;
                    resolvedCardReceiver = PlayerSide.Player;
                }
                else
                {
                    yield return PlayMiniGameResultFaces(PlayerSide.Bot);
                    resolvedCardAction = botAction;
                    resolvedCardReceiver = PlayerSide.Bot;
                }

                yield break;
            }

            if (playerWantsCard)
            {
                resolvedCardAction = playerAction;
                resolvedCardReceiver = PlayerSide.Player;
            }
            else if (botWantsCard)
            {
                resolvedCardAction = botAction;
                resolvedCardReceiver = PlayerSide.Bot;
            }
            else
            {
                resolvedCardAction = CardAction.Pass;
                resolvedCardReceiver = null;
            }
        }

        private IEnumerator PlayResolvedCardVisuals()
        {
            if (cardView == null)
            {
                yield break;
            }

            if (resolvedCardReceiver != null && resolvedCardAction == CardAction.Eat)
            {
                PlayerFaceView receiverFace = GetFace(resolvedCardReceiver.Value);
                bool badFood = currentCard != null && currentCard.BadFood;

                Tween mouthTween = receiverFace != null ? receiverFace.OpenMouthForIncomingFood() : null;
                if (mouthTween != null)
                {
                    yield return mouthTween.WaitForCompletion();
                }

                bool mouthReached = false;
                Tween foodTween = receiverFace != null
                    ? cardView.PlayFoodFlyToMouth(
                        receiverFace.MouthTarget,
                        () => mouthReached = true,
                        () => receiverFace.PlayFoodArrivalPop())
                    : null;

                while (foodTween != null && !mouthReached)
                {
                    yield return null;
                }

                // The game result lands exactly when the food reaches the mouth.
                yield return ApplyResolvedAction();

                if (foodTween != null)
                {
                    yield return foodTween.WaitForCompletion();
                }

                Tween chewTween = receiverFace != null && !resolvedEatOverate
                    ? receiverFace.ChewAfterFoodArrives(badFood)
                    : null;
                if (chewTween != null)
                {
                    yield return chewTween.WaitForCompletion();
                }

                Tween disappearTween = cardView.PlayDisappearScaleDown();
                if (disappearTween != null)
                {
                    yield return disappearTween.WaitForCompletion();
                }

                yield break;
            }

            Tween actionTween = cardView.PlayResolution(resolvedCardReceiver, resolvedCardAction);
            if (actionTween != null)
            {
                yield return actionTween.WaitForCompletion();
            }
        }

        private IEnumerator ApplyResolvedAction()
        {
            if (resolvedCardReceiver == null || resolvedEatApplied)
            {
                yield break;
            }

            resolvedEatApplied = true;
            yield return ApplyAction(GetState(resolvedCardReceiver.Value), currentCard, resolvedCardAction);
        }

        private CardAction ValidateAction(PlayerState state, CardData card, CardAction action)
        {
            return action;
        }

        private IEnumerator ApplyAction(PlayerState state, CardData card, CardAction action)
        {
            actionApplied?.Invoke(state.Side, action);

            switch (action)
            {
                case CardAction.Eat:
                    yield return ApplyEat(state, card, currentCardSatiety);
                    break;
                case CardAction.Hold:
                    state.HoldCard(card, config.HeldCardLifetimeRounds + 1);
                    break;
            }
        }

        private IEnumerator ApplyEat(PlayerState state, CardData card, int satietyAmount)
        {
            if (state == null || card == null)
            {
                yield break;
            }

            int previousSatiety = state.CurrentSatiety;

            if (state.Refuses(card))
            {
                int vomitAmount = UnityEngine.Random.Range(1, Mathf.Max(1, satietyAmount) + 1);
                state.RemoveSatiety(vomitAmount);
                resolvedEatOverate = true;
                AnnounceSatietyChange(state, previousSatiety);
                yield return PlayAllergyReaction(state, card, previousSatiety);
                satietyChanged?.Invoke(state.Side, state.CurrentSatiety, state.MaxSatiety);
                yield break;
            }

            if (card.BadFood)
            {
                int vomitAmount = UnityEngine.Random.Range(1, Mathf.Max(1, satietyAmount) + 1);
                state.RemoveSatiety(vomitAmount);
                resolvedEatOverate = true;
                AnnounceSatietyChange(state, previousSatiety);
                yield return PlayBadFoodReaction(state, previousSatiety);
                satietyChanged?.Invoke(state.Side, state.CurrentSatiety, state.MaxSatiety);
                yield break;
            }

            state.TryAddSatiety(satietyAmount, config.OvereatPenaltyPercent, out _, out bool penaltyApplied);
            resolvedEatOverate = penaltyApplied;
            AnnounceSatietyChange(state, previousSatiety);

            if (penaltyApplied)
            {
                yield return PlayOvereatReaction(state, previousSatiety, previousSatiety + satietyAmount);
            }
            else
            {
                Tween barTween = PlaySatietyChange(state, previousSatiety);
                if (barTween != null)
                {
                    yield return barTween.WaitForCompletion();
                }
            }

            satietyChanged?.Invoke(state.Side, state.CurrentSatiety, state.MaxSatiety);
        }

        private void AnnounceSatietyChange(PlayerState state, int previousSatiety)
        {
            int delta = state.CurrentSatiety - previousSatiety;
            if (state.Side == PlayerSide.Player)
            {
                satietyAnnouncer?.ShowDelta(delta, playerSatietyBar != null ? playerSatietyBar.transform as RectTransform : null);
            }
            else
            {
                satietyAnnouncer?.ShowDelta(delta, botSatietyBar != null ? botSatietyBar.transform as RectTransform : null);
            }
        }

        private IEnumerator PlayAllergyReaction(PlayerState state, CardData card, int previousSatiety)
        {
            PlayerFaceView face = GetFace(state.Side);
            SatietyBarView bar = GetSatietyBar(state.Side);

            chatBubble?.ShowMessage(state.Profile != null
                ? state.Profile.GetAllergyMessage(card)
                : "I'm allergic to this!", GetReactionTarget(state.Side));
            Tween faceTween = face != null ? face.PlayOvereatReaction() : null;
            Tween barTween = bar != null
                ? bar.SetValueAnimated(previousSatiety, state.CurrentSatiety, state.MaxSatiety, 1.05f)
                : null;

            if (barTween != null)
            {
                yield return barTween.WaitForCompletion();
            }

            if (faceTween != null && faceTween.IsActive() && !faceTween.IsComplete())
            {
                yield return faceTween.WaitForCompletion();
            }
        }

        private IEnumerator PlayBadFoodReaction(PlayerState state, int previousSatiety)
        {
            PlayerFaceView face = GetFace(state.Side);
            SatietyBarView bar = GetSatietyBar(state.Side);

            chatBubble?.ShowMessage("Yuck...", GetReactionTarget(state.Side));
            Tween faceTween = face != null ? face.PlayOvereatReaction() : null;
            Tween barTween = bar != null
                ? bar.SetValueAnimated(previousSatiety, state.CurrentSatiety, state.MaxSatiety, 1.05f)
                : null;

            if (barTween != null)
            {
                yield return barTween.WaitForCompletion();
            }

            if (faceTween != null && faceTween.IsActive() && !faceTween.IsComplete())
            {
                yield return faceTween.WaitForCompletion();
            }
        }

        private IEnumerator PlayOvereatReaction(PlayerState state, int previousSatiety, int attemptedSatiety)
        {
            PlayerFaceView face = GetFace(state.Side);
            SatietyBarView bar = GetSatietyBar(state.Side);

            chatBubble?.ShowMessage("Too much...", GetReactionTarget(state.Side));
            Tween faceTween = face != null ? face.PlayOvereatReaction() : null;
            Tween barTween = bar != null
                ? bar.PlayOverfillAndDrain(previousSatiety, attemptedSatiety, state.CurrentSatiety, state.MaxSatiety)
                : null;

            if (barTween != null)
            {
                yield return barTween.WaitForCompletion();
            }

            if (faceTween != null && faceTween.IsActive() && !faceTween.IsComplete())
            {
                yield return faceTween.WaitForCompletion();
            }
        }

        private Tween PlaySatietyChange(PlayerState state, int previousSatiety)
        {
            SatietyBarView bar = GetSatietyBar(state.Side);
            if (bar == null)
            {
                RefreshSatietyViews();
                return null;
            }

            return bar.SetValueAnimated(previousSatiety, state.CurrentSatiety, state.MaxSatiety);
        }

        private void RefreshSatietyViews()
        {
            playerSatietyBar?.SetValue(playerState.CurrentSatiety, playerState.MaxSatiety);
            botSatietyBar?.SetValue(botState.CurrentSatiety, botState.MaxSatiety);
        }

        private PlayerState GetState(PlayerSide side)
        {
            return side == PlayerSide.Player ? playerState : botState;
        }

        private PlayerFaceView GetFace(PlayerSide side)
        {
            return side == PlayerSide.Player ? playerFace : botFace;
        }

        private RectTransform GetReactionTarget(PlayerSide side)
        {
            PlayerFaceView face = GetFace(side);
            if (face != null && face.transform is RectTransform faceRect)
            {
                return faceRect;
            }

            SatietyBarView bar = GetSatietyBar(side);
            return bar != null ? bar.transform as RectTransform : null;
        }

        private SatietyBarView GetSatietyBar(PlayerSide side)
        {
            return side == PlayerSide.Player ? playerSatietyBar : botSatietyBar;
        }

        private void ResolveSceneReferences()
        {
            if (cardView == null)
            {
                cardView = FindAnyObjectByType<CardView>(FindObjectsInactive.Include);
                if (cardView == null)
                {
                    Debug.LogWarning("No CardView found. Add your card prefab to the UI canvas and assign it to GameController.", this);
                }
            }

            EnsureSceneCardView();

            if (swipeInput == null)
            {
                swipeInput = FindAnyObjectByType<SwipeInputController>();
                if (swipeInput != null)
                {
                    swipeInput.ActionSelected -= OnPlayerActionSelected;
                    swipeInput.ActionSelected += OnPlayerActionSelected;
                    swipeInput.DragUpdated -= OnPlayerDragUpdated;
                    swipeInput.DragUpdated += OnPlayerDragUpdated;
                    swipeInput.DragCanceled -= OnPlayerDragCanceled;
                    swipeInput.DragCanceled += OnPlayerDragCanceled;
                }
            }

            if (botController == null)
            {
                botController = FindAnyObjectByType<BotController>();
            }

            if (miniGame == null)
            {
                miniGame = FindAnyObjectByType<TargetClickMiniGameController>(FindObjectsInactive.Include);
            }

            if (boosterController == null)
            {
                boosterController = FindAnyObjectByType<BoosterController>();
            }

            if (playerFace == null || botFace == null)
            {
                PlayerFaceView[] faces = FindObjectsByType<PlayerFaceView>(FindObjectsInactive.Include);
                for (int i = 0; i < faces.Length; i++)
                {
                    if (faces[i].Side == PlayerSide.Player && playerFace == null)
                    {
                        playerFace = faces[i];
                    }
                    else if (faces[i].Side == PlayerSide.Bot && botFace == null)
                    {
                        botFace = faces[i];
                    }
                }
            }
        }

        private void EnsureSceneCardView()
        {
            if (cardView == null || cardView.gameObject.scene.isLoaded)
            {
                return;
            }

            cardViewPrefab = cardView;

            if (cardSpawnParent == null)
            {
                Canvas canvas = FindAnyObjectByType<Canvas>();
                cardSpawnParent = canvas != null ? canvas.transform as RectTransform : null;
            }

            if (cardSpawnParent == null)
            {
                Debug.LogWarning("CardView is a prefab asset, but no UI Canvas was found to spawn it under.", this);
                return;
            }

            cardView = Instantiate(cardViewPrefab, cardSpawnParent);
            cardView.name = cardViewPrefab.name;

            RectTransform rectTransform = cardView.transform as RectTransform;
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.localScale = Vector3.one;
            }

            cardView.Hide();
        }

        private void OnPlayerActionSelected(CardAction action)
        {
            pendingPlayerAction = action;
            receivedPlayerAction = true;
            HideDecisionHint();
            timerView?.Hide();
        }

        private void ShowGameEndScreen(PlayerSide winner)
        {
            if (winner == PlayerSide.Player)
            {
                playerFace?.ShowFinalVictory();
                botFace?.ShowFinalDefeat();
                loseScreen?.Hide();
                victoryScreen?.Show(StartGame);
            }
            else
            {
                botFace?.ShowFinalVictory();
                playerFace?.ShowFinalDefeat();
                victoryScreen?.Hide();
                loseScreen?.Show(StartGame);
            }
        }

        private void UpdateRoundCounter()
        {
            if (roundCounterText != null)
            {
                roundCounterText.text = string.Format(roundCounterFormat, roundNumber);
            }
        }

        private void ShowDecisionHint()
        {
            if (decisionHint == null)
            {
                return;
            }

            decisionHintTween?.Kill();
            decisionHint.gameObject.SetActive(true);
            decisionHint.alpha = 0f;
            RectTransform hintTransform = decisionHint.transform as RectTransform;
            if (hintTransform != null && !decisionHintScaleCached)
            {
                decisionHintHomeScale = hintTransform.localScale;
                decisionHintScaleCached = true;
            }

            Sequence hintSequence = DOTween.Sequence()
                .SetTarget(this)
                .SetUpdate(true)
                .Append(decisionHint.DOFade(1f, decisionHintFadeDuration).SetEase(Ease.OutCubic));

            if (hintTransform != null)
            {
                hintTransform.localScale = decisionHintHomeScale;
                hintSequence.Append(hintTransform.DOScale(decisionHintHomeScale * decisionHintBreathScale, decisionHintBreathDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo));
            }

            decisionHintTween = hintSequence
                .SetEase(Ease.OutCubic)
                .SetUpdate(true);
        }

        private void HideDecisionHint(bool immediate = false)
        {
            if (decisionHint == null)
            {
                return;
            }

            decisionHintTween?.Kill();

            if (immediate)
            {
                decisionHint.alpha = 0f;
                decisionHint.gameObject.SetActive(false);
                ResetDecisionHintScale();
                return;
            }

            decisionHintTween = decisionHint.DOFade(0f, decisionHintFadeDuration)
                .SetEase(Ease.InCubic)
                .SetUpdate(true)
                .SetTarget(this)
                .OnComplete(() =>
                {
                    decisionHint.gameObject.SetActive(false);
                    ResetDecisionHintScale();
                });
        }

        private void ResetDecisionHintScale()
        {
            RectTransform hintTransform = decisionHint != null ? decisionHint.transform as RectTransform : null;
            if (hintTransform != null && decisionHintScaleCached)
            {
                hintTransform.localScale = decisionHintHomeScale;
            }
        }

        private void OnPlayerDragUpdated(Vector2 delta)
        {
            if (cardView != null && swipeInput != null)
            {
                cardView.PreviewSwipe(delta, swipeInput.MinSwipeDistance);
            }
        }

        private void OnPlayerDragCanceled()
        {
            cardView?.CancelSwipePreview();
        }

        private void PlayFaceResolution(PlayerSide? receiver, CardAction action, CardData card)
        {
            if (receiver == null || action != CardAction.Eat)
            {
                return;
            }

            bool badFood = card != null && card.BadFood;
            if (receiver == PlayerSide.Player)
            {
                playerFace?.PlayEatSequence(badFood);
            }
            else if (receiver == PlayerSide.Bot)
            {
                botFace?.PlayEatSequence(badFood);
            }
        }

        private IEnumerator PlayMiniGameResultFaces(PlayerSide winner)
        {
            if (winner == PlayerSide.Player)
            {
                playerFace?.PlayMiniGameWon();
                botFace?.PlayMiniGameLost();
            }
            else
            {
                botFace?.PlayMiniGameWon();
                playerFace?.PlayMiniGameLost();
            }

            if (miniGameResultDelaySeconds > 0f)
            {
                yield return new WaitForSeconds(miniGameResultDelaySeconds);
            }
        }
    }
}
