using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;

public class RewardSignal : MonoBehaviour
{
    [SerializeField]
    private List<WeightedRewardSource> weightedRewards;
    private List<WeightedRewardSource> nonEmptyRewards;

    [SerializeField]
    [DefaultValue(RewardMixing.MixType.Linear)]
    private RewardMixing.MixType MixingMethod;

    public float Reward { get => nonEmptyRewards.MixRewards(MixingMethod); }

    public void OnAgentInitialize()
    {
        nonEmptyRewards = weightedRewards.Where(r => !r.IsEmpty()).ToList();
        foreach (var wr in nonEmptyRewards)
        {
            wr.OnAgentInitialize();
        }
    }

}

public static class RewardMixing
{
    public enum MixType
    {
        Linear,
        Unweighted,
        Multiplicative
    }

    /// <summary>Weighted sum of a collection reward sources</summary>
    public static float LinearMix(this IEnumerable<WeightedRewardSource> rewardList)
    {
        return rewardList.Select(x => x.Weight * x.Reward).Sum();
    }

    /// <summary>Sum of a collection reward sources, ignoring the weight field in the WeightedRewardSource</summary>
    public static float UnweightedMix(this IEnumerable<WeightedRewardSource> rewardList)
    {
        return rewardList.Select(x => x.Reward).Sum();
    }

    public static float MultiplicativeMix(this IEnumerable<WeightedRewardSource> rewardList)
    {
        return rewardList.Select(x => x.Reward).Product();
    }

    public static float Product(this IEnumerable<float> nums)
    {
        return nums.Aggregate(1f, (acc, val) => acc * val);
    }

    ///<summary>Mix rewards with method selected by enum</summary>
    public static float MixRewards(this IEnumerable<WeightedRewardSource> rewardsToMix, MixType mixType)
    {
        switch (mixType)
        {
            case MixType.Linear:
                return rewardsToMix.LinearMix();

            case MixType.Unweighted:
                return rewardsToMix.UnweightedMix();

            case MixType.Multiplicative:
                return rewardsToMix.MultiplicativeMix();

            default:
                return rewardsToMix.LinearMix();
        }
    }
}
