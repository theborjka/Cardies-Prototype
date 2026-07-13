using System.Collections.Generic;
using UnityEngine;

namespace SatietyGame
{
    [CreateAssetMenu(menuName = "Satiety Game/Game Config", fileName = "Satiety Game Config")]
    public sealed class GameConfig : ScriptableObject
    {
        [Header("Rules")]
        [SerializeField, Min(1)] private int maxSatiety = 20;
        [SerializeField, Min(1f)] private float decisionTimeSeconds = 10f;
        [SerializeField, Range(0f, 1f)] private float overeatPenaltyPercent = 0.5f;
        [SerializeField, Min(1)] private int heldCardLifetimeRounds = 1;

        [Header("Content")]
        [SerializeField] private List<CardData> cards = new List<CardData>();
        [SerializeField] private List<BoosterData> boosters = new List<BoosterData>(3);
        [SerializeField] private bool shuffleDeck = true;

        public int MaxSatiety => maxSatiety;
        public float DecisionTimeSeconds => decisionTimeSeconds;
        public float OvereatPenaltyPercent => overeatPenaltyPercent;
        public int HeldCardLifetimeRounds => heldCardLifetimeRounds;
        public IReadOnlyList<CardData> Cards => cards;
        public IReadOnlyList<BoosterData> Boosters => boosters;
        public bool ShuffleDeck => shuffleDeck;

        private void OnValidate()
        {
            while (boosters.Count > 3)
            {
                boosters.RemoveAt(boosters.Count - 1);
            }
        }
    }
}
