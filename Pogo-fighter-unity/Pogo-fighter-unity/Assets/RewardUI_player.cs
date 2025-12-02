using Assets;
using UnityEngine;
using TMPro;

public class RewardUI_player : MonoBehaviour
{
    public PlayerController player;
    public TMP_Text rewardText;

    void Update()
    {
        if (player == null || rewardText == null) return;

        rewardText.text =
            "Player\n" +
            $"Instant: {player.CurrentReward:F4}\n" +
            $"Cumulative: {player.CumulativeReward:F4}";
    }
}
