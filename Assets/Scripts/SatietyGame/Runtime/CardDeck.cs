using System.Collections.Generic;
using UnityEngine;

namespace SatietyGame
{
    public sealed class CardDeck
    {
        private readonly List<CardData> drawPile = new List<CardData>();
        private readonly List<CardData> sourceCards = new List<CardData>();
        private bool shuffle;

        public void Initialize(IEnumerable<CardData> cards, bool shouldShuffle)
        {
            sourceCards.Clear();
            sourceCards.AddRange(cards);
            shuffle = shouldShuffle;
            Refill();
        }

        public CardData Draw()
        {
            if (drawPile.Count == 0)
            {
                Refill();
            }

            if (drawPile.Count == 0)
            {
                return null;
            }

            CardData card = drawPile[0];
            drawPile.RemoveAt(0);
            return card;
        }

        private void Refill()
        {
            drawPile.Clear();
            drawPile.AddRange(sourceCards);

            if (!shuffle)
            {
                return;
            }

            for (int i = 0; i < drawPile.Count; i++)
            {
                int randomIndex = Random.Range(i, drawPile.Count);
                (drawPile[i], drawPile[randomIndex]) = (drawPile[randomIndex], drawPile[i]);
            }
        }
    }
}
