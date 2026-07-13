using System.Collections.Generic;
using UnityEngine;

namespace SatietyGame
{
    [CreateAssetMenu(menuName = "Satiety Game/Player Profile", fileName = "New Player Profile")]
    public sealed class PlayerProfileData : ScriptableObject
    {
        [SerializeField] private string displayName = "Player";
        [SerializeField] private List<CardData> refusedProducts = new List<CardData>(3);

        public string DisplayName => displayName;
        public IReadOnlyList<CardData> RefusedProducts => refusedProducts;

        public bool Refuses(CardData card)
        {
            return card != null && refusedProducts.Contains(card);
        }

        private void OnValidate()
        {
            while (refusedProducts.Count > 3)
            {
                refusedProducts.RemoveAt(refusedProducts.Count - 1);
            }
        }
    }
}
