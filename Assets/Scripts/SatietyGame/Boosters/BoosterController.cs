using System;
using UnityEngine;

namespace SatietyGame
{
    public sealed class BoosterController : MonoBehaviour
    {
        public event Action<BoosterData> BoosterUsed;

        public bool TryUseBooster(BoosterData booster, PlayerState playerState, PlayerState botState, ref int currentCardSatiety, ref bool botActionBlocked)
        {
            if (booster == null || playerState == null || !playerState.MarkBoosterUsed(booster))
            {
                return false;
            }

            switch (booster.EffectType)
            {
                case BoosterEffectType.DoubleCurrentCardSatiety:
                    currentCardSatiety *= 2;
                    break;
                case BoosterEffectType.BlockBotAction:
                    botActionBlocked = true;
                    break;
                case BoosterEffectType.ProtectFromOvereatPenalty:
                    playerState.ProtectFromNextOvereatPenalty = true;
                    break;
            }

            BoosterUsed?.Invoke(booster);
            return true;
        }
    }
}
