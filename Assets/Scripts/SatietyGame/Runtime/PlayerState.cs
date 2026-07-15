using System.Collections.Generic;
using UnityEngine;

namespace SatietyGame
{
    public sealed class PlayerState
    {
        private readonly HashSet<BoosterData> usedBoosters = new HashSet<BoosterData>();
        private readonly HashSet<CardData> allergicFoods = new HashSet<CardData>();
        private readonly List<CardData> allergicFoodList = new List<CardData>();

        public PlayerState(PlayerSide side, int maxVomit, PlayerProfileData profile)
        {
            Side = side;
            MaxVomit = maxVomit;
            Profile = profile;
        }

        public PlayerSide Side { get; }
        public PlayerProfileData Profile { get; }
        public int MaxVomit { get; }
        public int CurrentVomit { get; private set; }
        public bool ProtectFromNextOvereatPenalty { get; set; }

        public bool VomitMeterFull => CurrentVomit >= MaxVomit;
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

        public int ChangeVomit(int amount)
        {
            int previous = CurrentVomit;
            CurrentVomit = Mathf.Clamp(CurrentVomit + amount, 0, MaxVomit);
            return CurrentVomit - previous;
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
