using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SatietyGame
{
    public sealed class AllergyIconsView : MonoBehaviour
    {
        [SerializeField] private Image[] slots;

        public void SetAllergies(IReadOnlyList<CardData> allergies)
        {
            if (slots == null)
            {
                return;
            }

            for (int i = 0; i < slots.Length; i++)
            {
                Image slot = slots[i];
                if (slot == null)
                {
                    continue;
                }

                bool hasAllergy = allergies != null && i < allergies.Count && allergies[i] != null;
                slot.gameObject.SetActive(hasAllergy);
                if (hasAllergy)
                {
                    slot.sprite = allergies[i].Icon;
                    slot.preserveAspect = true;
                }
            }
        }
    }
}
