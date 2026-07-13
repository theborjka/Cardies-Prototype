# Satiety Game Setup

1. Create `GameConfig` from `Create > Satiety Game > Game Config`.
2. Create food cards from `Create > Satiety Game > Card` and assign title, sprite, and satiety value.
3. Create two `Player Profile` assets. Add up to 3 refused products for the player and up to 3 for the bot.
4. Put `GameController`, `BotController`, `SwipeInputController`, `BoosterController`, and optionally `TargetClickMiniGameController` in the scene.
5. Add `CardView` to the card prefab and connect root, icon, title text, and satiety text.
6. Add `SatietyBarView` to both progress bars. It supports either an Image fill or a Slider.
7. Connect all scene references on `GameController`.

Swipe mapping:
- Left: eat the card.
- Right: pass.
- Down: hold the card for one round.

If both player and bot want the same card by eating or holding, `TargetClickMiniGameController` starts.
The player must click 5 spawned targets before the bot's simulated finish time.

Held cards are stored in `PlayerState.HeldCard`.
Call `GameController.TryPlayHeldCard(PlayerSide.Player)` from a UI button or another input when you want the player to use the held card.
