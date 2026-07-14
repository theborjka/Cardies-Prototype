using UnityEngine;

namespace SatietyGame
{
    [CreateAssetMenu(menuName = "Satiety Game/Player Profile", fileName = "New Player Profile")]
    public sealed class PlayerProfileData : ScriptableObject
    {
        [SerializeField] private string displayName = "Player";
        [SerializeField, Min(0)] private int allergicFoodCount = 1;
        [SerializeField] private string allergyMessageFormat = "I'm allergic to {0}!";

        public string DisplayName => displayName;
        public int AllergicFoodCount => allergicFoodCount;
        public string AllergyMessageFormat => allergyMessageFormat;

        public string GetAllergyMessage(CardData card)
        {
            string foodName = card != null ? card.Title : "that food";
            return string.Format(allergyMessageFormat, foodName);
        }
    }
}
