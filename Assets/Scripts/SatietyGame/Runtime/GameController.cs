using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

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

        [Header("Scene References")]
        [SerializeField] private CardView cardView;
        [SerializeField] private RectTransform cardSpawnParent;
        [SerializeField] private SwipeInputController swipeInput;
        [SerializeField] private BotController botController;
        [FormerlySerializedAs("playerSatietyBar")]
        [SerializeField] private SatietyBarView playerVomitBar;
        [FormerlySerializedAs("botSatietyBar")]
        [SerializeField] private SatietyBarView botVomitBar;
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
        [SerializeField] private SatietyChangeAnnouncerView playerSatietyAnnouncer;
        [SerializeField] private SatietyChangeAnnouncerView botSatietyAnnouncer;
        [SerializeField] private UIClickFeedbackView clickFeedback;
        [SerializeField] private GameObject playerTurnIndicator;
        [SerializeField] private GameObject botTurnIndicator;
        [Tooltip("Optional additional element shown while it is the player's turn.")]
        [SerializeField] private GameObject playerActiveTurnElement;
        [Tooltip("Optional additional element shown while it is the bot's turn.")]
        [SerializeField] private GameObject botActiveTurnElement;
        [SerializeField] private TMP_Text turnStatusText;
        [SerializeField] private TMP_Text notYourTurnText;
        [SerializeField] private TurnStatusView turnStatusView;
        [SerializeField] private NotYourTurnAnnouncerView notYourTurnAnnouncer;
        [SerializeField] private string playerTurnText = "Your turn!";
        [SerializeField] private string botTurnText = "Opponent turn";
        [SerializeField] private Button revealBoosterButton;
        [SerializeField] private Button reduceVomitBoosterButton;
        [SerializeField] private Button skipTurnBoosterButton;
        [SerializeField] private TMP_Text revealBoosterCounterText;
        [SerializeField] private TMP_Text reduceVomitBoosterCounterText;
        [SerializeField] private TMP_Text skipTurnBoosterCounterText;
        [SerializeField] private BoosterData revealBooster;
        [SerializeField] private BoosterData reduceVomitBooster;
        [SerializeField] private BoosterData skipTurnBooster;
        [SerializeField] private string boosterCounterFormat = "{remaining}/{max}";
        [SerializeField, Min(1)] private int boosterCounterMaxUses = 1;
        [SerializeField] private TMP_Text roundCounterText;
        [SerializeField] private CanvasGroup decisionHint;
        [SerializeField] private string roundCounterFormat = "ROUND {0}";
        [SerializeField, Min(0.01f)] private float decisionHintFadeDuration = 0.24f;
        [SerializeField, Range(1f, 1.08f)] private float decisionHintBreathScale = 1.018f;
        [SerializeField, Min(0.1f)] private float decisionHintBreathDuration = 1.35f;
        [Header("Bot Turn Timing")]
        [SerializeField, Min(0f)] private float botDecisionDelayMin = 0.4f;
        [SerializeField, Min(0f)] private float botDecisionDelayMax = 0.95f;

        [Header("Events")]
        [SerializeField] private CardEvent cardShown;
        [SerializeField] private ActionEvent actionChosen;
        [SerializeField] private ActionEvent actionApplied;
        [SerializeField] private SatietyEvent satietyChanged;
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
        private bool playerChoiceActive;
        private bool roundSkipped;
        private PlayerSide activeTurnSide = PlayerSide.Player;
        private int roundNumber;
        private Tween decisionHintTween;
        private Tween turnStatusTween;
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
                swipeInput.DragStarted += OnPlayerDragStarted;
                swipeInput.DragUpdated += OnPlayerDragUpdated;
                swipeInput.DragCanceled += OnPlayerDragCanceled;
            }

            if (cardView != null)
            {
                cardView.Clicked += OnCardClicked;
            }

            if (clickFeedback != null)
            {
                clickFeedback.ClickCompleted += OnScreenClickCompleted;
            }

            ResolveTurnMessageViews();

            revealBoosterButton?.onClick.AddListener(UseRevealBooster);
            reduceVomitBoosterButton?.onClick.AddListener(UseReduceVomitBooster);
            skipTurnBoosterButton?.onClick.AddListener(UseSkipTurnBooster);
        }

        private void OnDestroy()
        {
            if (swipeInput != null)
            {
                swipeInput.ActionSelected -= OnPlayerActionSelected;
                swipeInput.DragStarted -= OnPlayerDragStarted;
                swipeInput.DragUpdated -= OnPlayerDragUpdated;
                swipeInput.DragCanceled -= OnPlayerDragCanceled;
            }

            if (cardView != null)
            {
                cardView.Clicked -= OnCardClicked;
            }

            if (clickFeedback != null)
            {
                clickFeedback.ClickCompleted -= OnScreenClickCompleted;
            }

            revealBoosterButton?.onClick.RemoveListener(UseRevealBooster);
            reduceVomitBoosterButton?.onClick.RemoveListener(UseReduceVomitBooster);
            skipTurnBoosterButton?.onClick.RemoveListener(UseSkipTurnBooster);
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
            playerState = new PlayerState(PlayerSide.Player, config.MaxVomit, playerProfile);
            botState = new PlayerState(PlayerSide.Bot, config.MaxVomit, botProfile);
            playerState.RollAllergies(config.Cards);
            botState.RollAllergies(config.Cards);
            playerAllergyIcons?.SetAllergies(playerState.AllergicFoods);
            botAllergyIcons?.SetAllergies(botState.AllergicFoods);
            deck.Initialize(config.Cards, config.ShuffleDeck);
            roundNumber = 0;
            activeTurnSide = PlayerSide.Player;
            playerChoiceActive = false;
            roundSkipped = false;
            SetTurnIndicator(activeTurnSide);
            SetBoosterButtonsInteractable(true);
            UpdateBoosterCounters();
            UpdateRoundCounter();
            HideDecisionHint(true);
            timerView?.Hide(true);
            chatBubble?.Hide(true);
            victoryScreen?.Hide(true);
            loseScreen?.Hide(true);
            RefreshVomitViews();
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

        private IEnumerator GameLoop()
        {
            while (!playerState.VomitMeterFull && !botState.VomitMeterFull)
            {
                yield return PlayRound();
                yield return new WaitForSeconds(0.35f);
            }

            PlayerSide winner = botState.VomitMeterFull ? PlayerSide.Player : PlayerSide.Bot;
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
            resolvedCardAction = CardAction.None;
            resolvedCardReceiver = null;
            resolvedEatApplied = false;
            resolvedEatOverate = false;
            roundSkipped = false;

            if (currentCard == null)
            {
                Debug.LogWarning("Deck has no cards to draw. Add CardData assets to GameConfig.", this);
                yield break;
            }

            roundNumber++;
            UpdateRoundCounter();
            SetTurnIndicator(activeTurnSide);
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
            CardAction selectedAction;
            if (activeTurnSide == PlayerSide.Player)
            {
                yield return PlayerChooseAction();
            }
            else
            {
                yield return BotChooseAction();
            }

            selectedAction = pendingPlayerAction;

            if (roundSkipped)
            {
                Tween skippedCardTween = cardView != null ? cardView.PlayDisappearScaleDown() : null;
                if (skippedCardTween != null)
                {
                    yield return skippedCardTween.WaitForCompletion();
                }

                swipeInput?.ResetSwipeHint();
                cardView?.Hide();
                activeTurnSide = GetOpponentSide(activeTurnSide);
                SetTurnIndicator(activeTurnSide);
                yield break;
            }

            resolvedCardAction = selectedAction;
            resolvedCardReceiver = selectedAction == CardAction.EatSelf
                ? activeTurnSide
                : GetOpponentSide(activeTurnSide);
            actionChosen?.Invoke(activeTurnSide, selectedAction);

            if (cardView != null)
            {
                Tween choiceTween = cardView.PlayChoiceFeedback(selectedAction, activeTurnSide);
                if (choiceTween != null)
                {
                    yield return choiceTween.WaitForCompletion();
                }

                Tween revealTween = cardView.PlayReveal();
                if (revealTween != null)
                {
                    yield return revealTween.WaitForCompletion();
                }
            }

            yield return PlayResolvedCardVisuals();
            cardView?.Hide();
            activeTurnSide = GetOpponentSide(activeTurnSide);
            SetTurnIndicator(activeTurnSide);
        }

        private IEnumerator PlayerChooseAction()
        {
            yield return WaitForTurnStatusAnimation();
            playerChoiceActive = true;
            SetBoosterButtonsInteractable(true);
            ShowDecisionHint();
            timerView?.Show();
            swipeInput?.EnableInput();

            float remaining = config.DecisionTimeSeconds;
            while (remaining > 0f && !receivedPlayerAction)
            {
                remaining -= Time.deltaTime;
                timerView?.SetTime(remaining, config.DecisionTimeSeconds);
                yield return null;
            }

            swipeInput?.DisableInput();
            HideDecisionHint();
            timerView?.Hide();
            ClearTurnStateVisuals(activeTurnSide);
            playerChoiceActive = false;
            SetBoosterButtonsInteractable(false);
            pendingPlayerAction = receivedPlayerAction ? pendingPlayerAction : CardAction.FeedOpponent;
        }

        private IEnumerator BotChooseAction()
        {
            yield return WaitForTurnStatusAnimation();
            playerChoiceActive = false;
            SetBoosterButtonsInteractable(false);
            HideDecisionHint(true);
            timerView?.Show();
            timerView?.SetTime(config.DecisionTimeSeconds, config.DecisionTimeSeconds);
            float delayMin = Mathf.Max(0f, botDecisionDelayMin);
            float delayMax = Mathf.Max(delayMin, botDecisionDelayMax);
            float remaining = config.DecisionTimeSeconds;
            float botDelay = UnityEngine.Random.Range(delayMin, delayMax);
            while (remaining > 0f && botDelay > 0f)
            {
                float deltaTime = Mathf.Min(Time.deltaTime, botDelay);
                botDelay -= deltaTime;
                remaining -= deltaTime;
                timerView?.SetTime(remaining, config.DecisionTimeSeconds);
                yield return null;
            }
            pendingPlayerAction = botActionBlocked || botController == null
                ? CardAction.FeedOpponent
                : botController.ChooseAction(currentCard, botState, playerState);
            swipeInput?.ShowActionHint(pendingPlayerAction, PlayerSide.Bot);
            timerView?.Hide();
            ClearTurnStateVisuals(activeTurnSide);
        }

        private IEnumerator WaitForTurnStatusAnimation()
        {
            if (turnStatusTween != null && turnStatusTween.IsActive())
            {
                yield return turnStatusTween.WaitForCompletion();
            }
        }

        private PlayerSide GetOpponentSide(PlayerSide side)
        {
            return side == PlayerSide.Player ? PlayerSide.Bot : PlayerSide.Player;
        }

        private IEnumerator PlayResolvedCardVisuals()
        {
            if (cardView == null || resolvedCardReceiver == null)
            {
                yield break;
            }

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

            if (foodTween != null)
            {
                while (!mouthReached)
                {
                    yield return null;
                }

            }

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

            swipeInput?.ResetSwipeHint();
            Tween disappearTween = cardView.PlayDisappearScaleDown();
            if (disappearTween != null)
            {
                yield return disappearTween.WaitForCompletion();
            }
        }

        private IEnumerator ApplyResolvedAction()
        {
            if (resolvedCardReceiver == null || resolvedEatApplied)
            {
                yield break;
            }

            resolvedEatApplied = true;
            yield return ApplyAction(GetState(resolvedCardReceiver.Value), currentCard);
        }

        private IEnumerator ApplyAction(PlayerState state, CardData card)
        {
            actionApplied?.Invoke(activeTurnSide, resolvedCardAction);
            yield return ApplyEat(state, card, currentCardSatiety);
        }

        private IEnumerator ApplyEat(PlayerState state, CardData card, int foodValue)
        {
            if (state == null || card == null)
            {
                yield break;
            }

            int previousVomit = state.CurrentVomit;
            bool harmfulFood = card.BadFood || state.Refuses(card);
            int vomitDelta = harmfulFood ? Mathf.Max(1, foodValue) : -Mathf.Max(1, foodValue);
            state.ChangeVomit(vomitDelta);
            resolvedEatOverate = harmfulFood;
            AnnounceVomitChange(state, previousVomit);

            Tween reactionTween = harmfulFood
                ? PlayHarmfulFoodReaction(state, card, previousVomit)
                : PlayVomitChange(state, previousVomit);
            if (reactionTween != null)
            {
                yield return reactionTween.WaitForCompletion();
            }

            satietyChanged?.Invoke(state.Side, state.CurrentVomit, state.MaxVomit);
        }

        private void AnnounceVomitChange(PlayerState state, int previousVomit)
        {
            int delta = state.CurrentVomit - previousVomit;
            SatietyChangeAnnouncerView announcer = state.Side == PlayerSide.Player
                ? playerSatietyAnnouncer
                : botSatietyAnnouncer;
            if (announcer == null)
            {
                announcer = satietyAnnouncer;
            }

            announcer?.ShowVomitChangeAtStart(delta);
        }

        private Tween PlayHarmfulFoodReaction(PlayerState state, CardData card, int previousVomit)
        {
            PlayerFaceView face = GetFace(state.Side);
            PlayerFaceView opponentFace = GetFace(GetOpponentSide(state.Side));
            SatietyBarView bar = GetSatietyBar(state.Side);

            if (state.Refuses(card))
            {
                ShowPlayerReactionMessage(state, state.Profile != null
                    ? state.Profile.GetAllergyMessage(card)
                    : "I'm allergic to this!");
            }
            else
            {
                ShowPlayerReactionMessage(state, "Yuck...");
            }

            face?.PlayOvereatReaction();
            opponentFace?.PlayLaughReaction();
            return bar != null
                ? bar.SetValueAnimated(previousVomit, state.CurrentVomit, state.MaxVomit, 1.05f)
                : null;
        }

        private Tween PlayVomitChange(PlayerState state, int previousVomit)
        {
            SatietyBarView bar = GetSatietyBar(state.Side);
            if (bar == null)
            {
                RefreshVomitViews();
                return null;
            }

            return bar.SetValueAnimated(previousVomit, state.CurrentVomit, state.MaxVomit);
        }

        private void RefreshVomitViews()
        {
            playerVomitBar?.SetValue(playerState.CurrentVomit, playerState.MaxVomit);
            botVomitBar?.SetValue(botState.CurrentVomit, botState.MaxVomit);
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

        private void ShowPlayerReactionMessage(PlayerState state, string message)
        {
            if (state == null || state.Side != PlayerSide.Player)
            {
                chatBubble?.Hide();
                return;
            }

            chatBubble?.ShowMessage(message);
        }

        private SatietyBarView GetSatietyBar(PlayerSide side)
        {
            return side == PlayerSide.Player ? playerVomitBar : botVomitBar;
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

            if (cardView != null)
            {
                cardView.Clicked -= OnCardClicked;
                cardView.Clicked += OnCardClicked;
            }

            if (clickFeedback == null)
            {
                clickFeedback = FindAnyObjectByType<UIClickFeedbackView>();
            }

            if (clickFeedback != null)
            {
                clickFeedback.ClickCompleted -= OnScreenClickCompleted;
                clickFeedback.ClickCompleted += OnScreenClickCompleted;
            }

            EnsureSceneCardView();

            if (swipeInput == null)
            {
                swipeInput = FindAnyObjectByType<SwipeInputController>();
                if (swipeInput != null)
                {
                    swipeInput.ActionSelected -= OnPlayerActionSelected;
                    swipeInput.ActionSelected += OnPlayerActionSelected;
                    swipeInput.DragStarted -= OnPlayerDragStarted;
                    swipeInput.DragStarted += OnPlayerDragStarted;
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

        private void SetTurnIndicator(PlayerSide side)
        {
            swipeInput?.ResetSwipeHint();

            ShowTurnStatus(side == PlayerSide.Player ? playerTurnText : botTurnText, side);

            if (playerTurnIndicator != null)
            {
                playerTurnIndicator.SetActive(side == PlayerSide.Player);
            }

            if (botTurnIndicator != null)
            {
                botTurnIndicator.SetActive(side == PlayerSide.Bot);
            }

            if (playerActiveTurnElement != null)
            {
                playerActiveTurnElement.SetActive(side == PlayerSide.Player);
            }

            if (botActiveTurnElement != null)
            {
                botActiveTurnElement.SetActive(side == PlayerSide.Bot);
            }

            GetFace(side)?.SetTurnEffect(true);
            GetFace(GetOpponentSide(side))?.SetTurnEffect(false);
        }

        private void ClearTurnStateVisuals(PlayerSide side)
        {
            if (side == PlayerSide.Player)
            {
                playerTurnIndicator?.SetActive(false);
            }
            else
            {
                botTurnIndicator?.SetActive(false);
            }

            if (side == PlayerSide.Player)
            {
                playerActiveTurnElement?.SetActive(false);
            }
            else
            {
                botActiveTurnElement?.SetActive(false);
            }

            GetFace(side)?.SetTurnEffect(false);
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
            ClearTurnStateVisuals(activeTurnSide);
            SetBoosterButtonsInteractable(false);
        }

        private void OnPlayerDragStarted()
        {
            HideDecisionHint();
        }

        private void OnCardClicked()
        {
            if (activeTurnSide != PlayerSide.Player)
            {
                ShowNotYourTurn();
            }
        }

        private void OnScreenClickCompleted(Vector2 screenPosition)
        {
            if (activeTurnSide == PlayerSide.Player || EventSystem.current == null)
            {
                return;
            }

            PointerEventData pointer = new PointerEventData(EventSystem.current)
            {
                position = screenPosition
            };
            System.Collections.Generic.List<RaycastResult> results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, results);
            for (int i = 0; i < results.Count; i++)
            {
                if (results[i].gameObject.GetComponentInParent<CardView>() != null
                    || results[i].gameObject == revealBoosterButton?.gameObject
                    || results[i].gameObject == reduceVomitBoosterButton?.gameObject
                    || results[i].gameObject == skipTurnBoosterButton?.gameObject)
                {
                    notYourTurnAnnouncer?.ShowAtScreenPosition(screenPosition);
                    return;
                }
            }

            Camera eventCamera = pointer.pressEventCamera;
            RectTransform cardRect = cardView != null ? cardView.transform as RectTransform : null;
            if (cardRect != null && RectTransformUtility.RectangleContainsScreenPoint(cardRect, screenPosition, eventCamera))
            {
                notYourTurnAnnouncer?.ShowAtScreenPosition(screenPosition);
            }
        }

        private void ShowNotYourTurn()
        {
            ResolveTurnMessageViews();
            if (notYourTurnAnnouncer != null)
            {
                notYourTurnAnnouncer.Show();
            }
        }

        private Tween ShowTurnStatus(string message, PlayerSide side)
        {
            ResolveTurnMessageViews();
            if (turnStatusView == null)
            {
                return null;
            }

            turnStatusTween = turnStatusView.Show(message, side);
            return turnStatusTween;
        }

        private void ResolveTurnMessageViews()
        {
            if (turnStatusText != null && turnStatusView == null)
            {
                turnStatusView = turnStatusText.GetComponent<TurnStatusView>();
                if (turnStatusView == null)
                {
                    turnStatusView = turnStatusText.gameObject.AddComponent<TurnStatusView>();
                }
            }

            if (notYourTurnAnnouncer == null)
            {
                notYourTurnAnnouncer = GetComponent<NotYourTurnAnnouncerView>();
                if (notYourTurnAnnouncer == null)
                {
                    notYourTurnAnnouncer = gameObject.AddComponent<NotYourTurnAnnouncerView>();
                }
            }

            notYourTurnAnnouncer.Configure(notYourTurnText);
        }

        private void UseRevealBooster()
        {
            if (activeTurnSide != PlayerSide.Player)
            {
                ShowNotYourTurn();
                return;
            }

            if (!CanUsePlayerBooster(revealBooster, revealBoosterButton) || cardView == null
                || !playerState.MarkBoosterUsed(revealBooster))
            {
                return;
            }

            revealBoosterButton.interactable = false;
            SetBoosterCounter(revealBoosterCounterText, revealBooster);
            cardView.RevealImmediately();
        }

        private void UseReduceVomitBooster()
        {
            if (activeTurnSide != PlayerSide.Player)
            {
                ShowNotYourTurn();
                return;
            }

            if (!CanUsePlayerBooster(reduceVomitBooster, reduceVomitBoosterButton)
                || !playerState.MarkBoosterUsed(reduceVomitBooster))
            {
                return;
            }

            reduceVomitBoosterButton.interactable = false;
            SetBoosterCounter(reduceVomitBoosterCounterText, reduceVomitBooster);
            int previousVomit = playerState.CurrentVomit;
            playerState.ChangeVomit(-4);
            AnnounceVomitChange(playerState, previousVomit);
            StartCoroutine(AnimatePlayerVomitChange(previousVomit));
        }

        private void UseSkipTurnBooster()
        {
            if (activeTurnSide != PlayerSide.Player)
            {
                ShowNotYourTurn();
                return;
            }

            if (!CanUsePlayerBooster(skipTurnBooster, skipTurnBoosterButton)
                || !playerState.MarkBoosterUsed(skipTurnBooster))
            {
                return;
            }

            skipTurnBoosterButton.interactable = false;
            SetBoosterCounter(skipTurnBoosterCounterText, skipTurnBooster);
            playerState.RandomizeAllergies(config.Cards);
            botState.RandomizeAllergies(config.Cards);
            playerAllergyIcons?.SetAllergies(playerState.AllergicFoods);
            botAllergyIcons?.SetAllergies(botState.AllergicFoods);
        }

        private bool CanUsePlayerBooster(BoosterData booster, Button button)
        {
            return activeTurnSide == PlayerSide.Player && playerChoiceActive && currentCard != null
                && playerState != null && booster != null && button != null && button.interactable
                && !playerState.IsBoosterUsed(booster);
        }

        private IEnumerator AnimatePlayerVomitChange(int previousVomit)
        {
            Tween barTween = PlayVomitChange(playerState, previousVomit);
            if (barTween != null)
            {
                yield return barTween.WaitForCompletion();
            }

            satietyChanged?.Invoke(PlayerSide.Player, playerState.CurrentVomit, playerState.MaxVomit);
        }

        private void SetBoosterButtonsInteractable(bool interactable)
        {
            if (revealBoosterButton != null)
            {
                revealBoosterButton.interactable = interactable && revealBooster != null
                    && (playerState == null || !playerState.IsBoosterUsed(revealBooster));
            }

            if (reduceVomitBoosterButton != null)
            {
                reduceVomitBoosterButton.interactable = interactable && reduceVomitBooster != null
                    && (playerState == null || !playerState.IsBoosterUsed(reduceVomitBooster));
            }

            if (skipTurnBoosterButton != null)
            {
                skipTurnBoosterButton.interactable = interactable && skipTurnBooster != null
                    && (playerState == null || !playerState.IsBoosterUsed(skipTurnBooster));
            }
        }

        private void UpdateBoosterCounters()
        {
            SetBoosterCounter(revealBoosterCounterText, revealBooster);
            SetBoosterCounter(reduceVomitBoosterCounterText, reduceVomitBooster);
            SetBoosterCounter(skipTurnBoosterCounterText, skipTurnBooster);
        }

        private void SetBoosterCounter(TMP_Text counterText, BoosterData booster)
        {
            if (counterText == null)
            {
                return;
            }

            int maxUses = Mathf.Max(1, boosterCounterMaxUses);
            int used = playerState != null && booster != null && playerState.IsBoosterUsed(booster) ? maxUses : 0;
            int remaining = Mathf.Max(0, maxUses - used);
            counterText.text = FormatBoosterCounter(remaining, maxUses, used);
        }

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(boosterCounterFormat))
            {
                boosterCounterFormat = "{remaining}/{max}";
            }

            boosterCounterMaxUses = Mathf.Max(1, boosterCounterMaxUses);
        }

        private string FormatBoosterCounter(int remaining, int maxUses, int used)
        {
            string format = string.IsNullOrWhiteSpace(boosterCounterFormat) ? "{remaining}/{max}" : boosterCounterFormat;
            string formatted = format
                .Replace("{x/x}", $"{remaining}/{maxUses}", StringComparison.OrdinalIgnoreCase)
                .Replace("{remaining}", remaining.ToString(), StringComparison.OrdinalIgnoreCase)
                .Replace("{left}", remaining.ToString(), StringComparison.OrdinalIgnoreCase)
                .Replace("{current}", remaining.ToString(), StringComparison.OrdinalIgnoreCase)
                .Replace("{max}", maxUses.ToString(), StringComparison.OrdinalIgnoreCase)
                .Replace("{total}", maxUses.ToString(), StringComparison.OrdinalIgnoreCase)
                .Replace("{used}", used.ToString(), StringComparison.OrdinalIgnoreCase);

            try
            {
                return string.Format(formatted, remaining, maxUses, used);
            }
            catch (FormatException)
            {
                return $"{remaining}/{maxUses}";
            }
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

    }
}
