using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SatietyGame
{
    public sealed class MiniGameProgressBarView : MonoBehaviour
    {
        [Header("Manual Segments")]
        [SerializeField] private Image[] playerSegments;
        [SerializeField] private Image[] botSegments;

        [Header("Generated Segments")]
        [SerializeField] private RectTransform playerSegmentParent;
        [SerializeField] private RectTransform botSegmentParent;
        [SerializeField] private Image segmentPrefab;

        [Header("Visuals")]
        [SerializeField] private Color playerColor = new Color(0.43f, 0.9f, 0.24f, 1f);
        [SerializeField] private Color botColor = new Color(0.95f, 0.22f, 0.18f, 1f);
        [SerializeField] private Color inactiveColor = new Color(0.18f, 0.15f, 0.11f, 0.75f);
        [SerializeField] private TMP_Text playerScoreText;
        [SerializeField] private TMP_Text botScoreText;

        private readonly List<Image> generatedPlayerSegments = new List<Image>();
        private readonly List<Image> generatedBotSegments = new List<Image>();
        private int configuredMaxScore = -1;

        public void Initialize(int maxScore)
        {
            configuredMaxScore = Mathf.Max(1, maxScore);
            EnsureGeneratedSegments(generatedPlayerSegments, playerSegmentParent, configuredMaxScore);
            EnsureGeneratedSegments(generatedBotSegments, botSegmentParent, configuredMaxScore);
            SetValue(0, 0, configuredMaxScore);
        }

        public void SetValue(int playerScore, int botScore, int maxScore)
        {
            if (configuredMaxScore != maxScore)
            {
                Initialize(maxScore);
            }

            IReadOnlyList<Image> player = GetPlayerSegments();
            IReadOnlyList<Image> bot = GetBotSegments();

            ApplySegments(player, playerScore, playerColor);
            ApplySegments(bot, botScore, botColor);

            if (playerScoreText != null)
            {
                playerScoreText.text = playerScore.ToString();
            }

            if (botScoreText != null)
            {
                botScoreText.text = botScore.ToString();
            }
        }

        private IReadOnlyList<Image> GetPlayerSegments()
        {
            return generatedPlayerSegments.Count > 0 ? generatedPlayerSegments : playerSegments;
        }

        private IReadOnlyList<Image> GetBotSegments()
        {
            return generatedBotSegments.Count > 0 ? generatedBotSegments : botSegments;
        }

        private void ApplySegments(IReadOnlyList<Image> segments, int score, Color activeColor)
        {
            if (segments == null)
            {
                return;
            }

            for (int i = 0; i < segments.Count; i++)
            {
                Image segment = segments[i];
                if (segment == null)
                {
                    continue;
                }

                bool active = i < score;
                segment.color = active ? activeColor : inactiveColor;
                segment.enabled = true;
            }
        }

        private void EnsureGeneratedSegments(List<Image> generated, RectTransform parent, int count)
        {
            if (parent == null || segmentPrefab == null)
            {
                return;
            }

            while (generated.Count < count)
            {
                Image segment = Instantiate(segmentPrefab, parent);
                segment.gameObject.SetActive(true);
                generated.Add(segment);
            }

            for (int i = 0; i < generated.Count; i++)
            {
                generated[i].gameObject.SetActive(i < count);
            }
        }
    }
}
