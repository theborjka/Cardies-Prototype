using System;
using System.Collections.Generic;
using UnityEngine;

namespace SatietyGame
{
    public sealed class PlayerState
    {
        private readonly HashSet<BoosterData> usedBoosters = new HashSet<BoosterData>();
        private readonly HashSet<CardData> allergicFoods = new HashSet<CardData>();
        private readonly List<CardData> allergicFoodList = new List<CardData>();

        public PlayerState(PlayerSide side, int maxSatiety, PlayerProfileData profile)
        {
            Side = side;
            MaxSatiety = maxSatiety;
            Profile = profile;
        }

        public PlayerSide Side { get; }
        public PlayerProfileData Profile { get; }
        public int MaxSatiety { get; }
        public int CurrentSatiety { get; private set; }
        public CardData HeldCard { get; private set; }
        public int HeldCardRoundsLeft { get; private set; }
        public bool ProtectFromNextOvereatPenalty { get; set; }

        public bool HasWon => CurrentSatiety >= MaxSatiety;
        public IReadOnlyList<CardData> AllergicFoods => allergicFoodList;

        public bool Refuses(CardData card)
        {
            return card != null && allergicFoods.Contains(card);
        }

        public void RollAllergies(IReadOnlyList<CardData> availableCards)
        {
            allergicFoods.Clear();
            allergicFoodList.Clear();

            if (Profile == null || availableCards == null || Profile.AllergicFoodCount <= 0)
            {
                return;
            }

            List<CardData> candidates = new List<CardData>();
            for (int i = 0; i < availableCards.Count; i++)
            {
                CardData card = availableCards[i];
                if (card != null && !card.BadFood && !candidates.Contains(card))
                {
                    candidates.Add(card);
                }
            }

            int allergyCount = Mathf.Min(Profile.AllergicFoodCount, candidates.Count);
            for (int i = 0; i < allergyCount; i++)
            {
                int index = UnityEngine.Random.Range(i, candidates.Count);
                (candidates[i], candidates[index]) = (candidates[index], candidates[i]);
                allergicFoods.Add(candidates[i]);
                allergicFoodList.Add(candidates[i]);
            }
        }

        public bool TryAddSatiety(int amount, float overeatPenaltyPercent, out bool overate, out bool penaltyApplied)
        {
            overate = CurrentSatiety + amount > MaxSatiety;
            penaltyApplied = false;

            if (!overate)
            {
                CurrentSatiety += amount;
                return true;
            }

            if (ProtectFromNextOvereatPenalty)
            {
                ProtectFromNextOvereatPenalty = false;
                return false;
            }

            CurrentSatiety -= (int)(CurrentSatiety * overeatPenaltyPercent);
            penaltyApplied = true;
            return false;
        }

        public int RemoveSatiety(int amount)
        {
            int removableAmount = Math.Max(0, amount);
            int removedAmount = Math.Min(CurrentSatiety, removableAmount);
            CurrentSatiety -= removedAmount;
            return removedAmount;
        }

        public void HoldCard(CardData card, int rounds)
        {
            HeldCard = card;
            HeldCardRoundsLeft = rounds;
        }

        public void ClearHeldCard()
        {
            HeldCard = null;
            HeldCardRoundsLeft = 0;
        }

        public void TickHeldCardLifetime()
        {
            if (HeldCard == null)
            {
                return;
            }

            HeldCardRoundsLeft--;
            if (HeldCardRoundsLeft <= 0)
            {
                ClearHeldCard();
            }
        }

        public bool IsBoosterUsed(BoosterData booster)
        {
            return booster != null && usedBoosters.Contains(booster);
        }

        public bool MarkBoosterUsed(BoosterData booster)
        {
            return booster != null && usedBoosters.Add(booster);
        }
    }
}
