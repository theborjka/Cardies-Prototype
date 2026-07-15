using UnityEngine;

namespace SatietyGame
{
    [CreateAssetMenu(menuName = "Satiety Game/Card", fileName = "New Card")]
    public sealed class CardData : ScriptableObject
    {
        [SerializeField] private string title;
        [SerializeField] private Sprite icon;
        [SerializeField, Min(0)] private int satietyValue = 1;
        [SerializeField] private bool badFood;
        [SerializeField, Min(0f)] private float spawnWeight = 1f;

        public string Title => title;
        public Sprite Icon => icon;
        public int SatietyValue => satietyValue;
        public bool BadFood => badFood;
        public float SpawnWeight => spawnWeight;
        public string SatietyDescription => badFood
            ? $"+{satietyValue} до ригачіни"
            : $"+{satietyValue} детокс";
    }
}
