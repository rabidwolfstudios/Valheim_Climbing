using HarmonyLib;
using SkillManager;
using UnityEngine;

namespace RabidWolf.Valheim.Skills;

public class ClimbingSkill
{
    private static Skill _skill;

    /* Configuration */
    private static float? _experienceGainedFactor = null!;
    private static float? _experienceGainedDistance = null!;
    private static float? _experienceResetDistance = null!;
    private static int? _staminaReductionPercentage = null!;
    private static bool _logDebugMessages = false;

    public ClimbingSkill()
    {
        _skill = new Skill("Climbing", "climbing-icon.png");
        _skill.Description.English("Allows you to lose less stamina when running up sloped terrain");
        _skill.Configurable = false;
    }

    public static void SetSkillGainFactor(float? experienceGainedFactor)
    {
        _experienceGainedFactor = experienceGainedFactor;
    }

    public static void SetGainedDistance(float? experienceGainedDistance)
    {
        _experienceGainedDistance = experienceGainedDistance;
    }

    public static void SetResetDistance(float? experienceResetDistance)
    {
        _experienceResetDistance = experienceResetDistance;
    }

    public static void SetLogDebugMessages(bool showDebugMessages)
    {
        _logDebugMessages = showDebugMessages;
    }

    public static void SetExperienceLossFactor(int experienceLossFactor = 0)
    {
        _skill.SkillLoss = experienceLossFactor;
    }

    public static void SetStaminaReductionPercentage(int staminaReductionPercentage)
    {
        _staminaReductionPercentage = staminaReductionPercentage;
    }


    [HarmonyPatch(typeof(Player), nameof(Player.UseStamina))]
    private static class ChangeStaminaUsageResetByCounter
    {
        private static float _usage = 0f;
        private static Vector3 _previousPosition = Vector3.zero;

        private static void Prefix(Player __instance, ref float v)
        {
            var runSpeedFactor = __instance.GetRunSpeedFactor();
            var slopeAngle = __instance.GetSlopeAngle();
            var slideAngle = __instance.GetSlideAngle();
            var currentPosition = __instance.GetTransform().localPosition;
            var previousY = _previousPosition.y;
            var currentY = currentPosition.y;
            var targetForSkillRaise = previousY + _experienceGainedDistance.Value; // prevents raising skill by running in place against a rock
            var targetForReset = previousY - _experienceResetDistance.Value;

            if (slopeAngle > slideAngle
                && runSpeedFactor > 0)
            {
                var currentSkillFactor = Player.m_localPlayer.GetSkillFactor("Climbing");
                if (currentY >= targetForSkillRaise)
                {
                    _previousPosition = currentPosition;
                    _usage += _experienceGainedFactor.Value;

                    if (_usage > 1f)
                    {
                        Player.m_localPlayer.RaiseSkill("Climbing");
                        _usage = 0f;
                    }

                }
                else if (currentY <= targetForReset)
                {
                    _previousPosition = currentPosition;
                }
                var reduceBy = Mathf.Lerp(1f, (float)( (100 - _staminaReductionPercentage.Value) * 0.01), currentSkillFactor);
                var newV = v * reduceBy;

                if (_logDebugMessages)
                {
                    Debug.Log($"Climbing skill factor is: {currentSkillFactor} - Reducing stamina use by: {(1 - reduceBy) * 100}% - From {v} to {newV} - Percent towards next experience increase {_usage}");
                }

                v = newV;
            }
        }
    }
}
