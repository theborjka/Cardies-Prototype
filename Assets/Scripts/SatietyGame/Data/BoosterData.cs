using UnityEngine;

namespace SatietyGame
{
    public enum BoosterEffectType
    {
        DoubleCurrentCardSatiety = 0,
        BlockBotAction = 1,
        ProtectFromOvereatPenalty = 2
    }

    [CreateAssetMenu(menuName = "Satiety Game/Booster", fileName = "New Booster")]
    public sealed class BoosterData : ScriptableObject
    {
        [SerializeField] private string title;
        [SerializeField] private Sprite icon;
        [SerializeField, TextArea] private string description;
        [SerializeField] private BoosterEffectType effectType;

        public string Title => title;
        public Sprite Icon => icon;
        public string Description => description;
        public BoosterEffectType EffectType => effectType;
    }
}
