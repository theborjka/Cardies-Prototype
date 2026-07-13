using System.Collections.Generic;

namespace SatietyGame
{
    public sealed class PlayerState
    {
        private readonly HashSet<BoosterData> usedBoosters = new HashSet<BoosterData>();

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

        public bool Refuses(CardData card)
        {
            return Profile != null && Profile.Refuses(card);
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
