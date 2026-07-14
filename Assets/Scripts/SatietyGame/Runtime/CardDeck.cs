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

            int cardIndex = shuffle ? GetWeightedCardIndex() : 0;
            CardData card = drawPile[cardIndex];
            drawPile.RemoveAt(cardIndex);
            return card;
        }

        private int GetWeightedCardIndex()
        {
            float totalWeight = 0f;
            for (int i = 0; i < drawPile.Count; i++)
            {
                if (drawPile[i] != null)
                {
                    totalWeight += Mathf.Max(0f, drawPile[i].SpawnWeight);
                }
            }

            if (totalWeight <= 0f)
            {
                return Random.Range(0, drawPile.Count);
            }

            float roll = Random.value * totalWeight;
            int lastWeightedIndex = -1;
            for (int i = 0; i < drawPile.Count; i++)
            {
                CardData card = drawPile[i];
                if (card == null)
                {
                    continue;
                }

                float weight = Mathf.Max(0f, card.SpawnWeight);
                if (weight > 0f)
                {
                    lastWeightedIndex = i;
                }

                roll -= weight;
                if (roll <= 0f)
                {
                    return i;
                }
            }

            return lastWeightedIndex >= 0 ? lastWeightedIndex : drawPile.Count - 1;
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
