using UnityEngine;

namespace SatietyGame
{
    public sealed class BotController : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float holdChance = 0.18f;
        [SerializeField, Range(0f, 1f)] private float passChanceWhenSafe = 0.12f;

        public CardAction ChooseAction(CardData card, PlayerState botState, PlayerState playerState)
        {
            if (card == null || botState == null)
            {
                return CardAction.FeedOpponent;
            }

            if (botState.Refuses(card))
            {
                return CardAction.FeedOpponent;
            }

            if (card.BadFood || playerState.Refuses(card))
            {
                return CardAction.FeedOpponent;
            }

            if (Random.value < holdChance)
            {
                return CardAction.FeedOpponent;
            }

            return Random.value < passChanceWhenSafe ? CardAction.FeedOpponent : CardAction.EatSelf;
        }
    }
}
