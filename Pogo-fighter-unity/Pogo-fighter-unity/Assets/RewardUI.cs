using Assets;
using UnityEngine;
using TMPro;

public class RewardUI : MonoBehaviour
{
    public RLBouncingAgent agent;
    public TMP_Text rewardText;

    void Update()
    {
        if (agent == null || rewardText == null) return;

        rewardText.text =
            $"Instant: {agent.CurrentReward:F4}\n" +
            $"Cumulative: {agent.GetCumulativeReward():F4}";
    }
}
