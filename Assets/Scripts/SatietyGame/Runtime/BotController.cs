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
                return CardAction.Pass;
            }

            if (botState.Refuses(card))
            {
                return CardAction.Pass;
            }

            bool wouldOvereat = botState.CurrentSatiety + card.SatietyValue > botState.MaxSatiety;
            if (wouldOvereat)
            {
                return Random.value < holdChance ? CardAction.Hold : CardAction.Pass;
            }

            bool canWin = botState.CurrentSatiety + card.SatietyValue >= botState.MaxSatiety;
            if (canWin)
            {
                return CardAction.Eat;
            }

            if (Random.value < holdChance)
            {
                return CardAction.Hold;
            }

            return Random.value < passChanceWhenSafe ? CardAction.Pass : CardAction.Eat;
        }
    }
}
