using Assets;
using UnityEngine;
using TMPro;

public class RewardUI_agent : MonoBehaviour
{
    public RLBouncingAgent agent;
    public TMP_Text rewardText;

    void Update()
    {
        if (agent == null || rewardText == null) return;

        rewardText.text =
            "Agent\n" +
            $"Instant: {agent.CurrentReward:F4}\n" +
            $"Cumulative: {agent.CumulativeReward:F4}";
    }
}
