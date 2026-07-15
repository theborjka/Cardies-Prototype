# Satiety Game Setup

1. Create `GameConfig` from `Create > Satiety Game > Game Config`.
2. Create food cards from `Create > Satiety Game > Card` and assign title, sprite, and satiety value.
3. Create two `Player Profile` assets. Allergic products are rolled from the good-food list at game start.
4. Put `GameController`, `BotController`, `SwipeInputController`, and `BoosterController` in the scene.
5. Add `CardView` to the card prefab and connect root, icon, title text, satiety text, and the `Reveal Overlay`.
6. Add `SatietyBarView` to both vomit meters. The existing bar component is reused for the vomit value and supports either an Image fill or a Slider.
7. Connect all scene references on `GameController`.

Swipe mapping for the active side:
- Left: eat the card yourself.
- Right: feed the opponent.

The card starts hidden. After the active side chooses, `CardView.PlayReveal()` fades the overlay and animates the configured shader properties before the food flies to the receiver.

Each round belongs to one side. The active side chooses either `EatSelf` or `FeedOpponent`; the card is revealed and flies to the selected receiver before the turn changes.

Good food reduces the receiver's vomit meter. Bad or allergic food increases it. When a player's meter reaches `GameConfig > Max Vomit`, the opponent wins.

Assign `Player Vomit Bar` and `Bot Vomit Bar` on `GameController`. `CardView > Reveal / Blur` contains the overlay material, blur/reveal property names, and a manual list of additional float shader parameters. Each parameter has a hidden value and a revealed value.

Create three `BoosterData` assets with effect types `Reveal Card`, `Reduce Vomit`, and `Skip Turn`. Assign them to the matching `GameController` fields and assign the three designer-created buttons to `Reveal Booster Button`, `Reduce Vomit Booster Button`, and `Skip Turn Booster Button`. Each booster is usable once per game and resets when `StartGame()` creates a new player state.

Assign the three `Booster Counter Text` fields to the labels below the buttons. They display `1/1` at game start and `0/1` after use. Reveal also applies the configured revealed values to both the reveal overlay material and the card product material, including custom shader parameters such as `_HologramFade`.
