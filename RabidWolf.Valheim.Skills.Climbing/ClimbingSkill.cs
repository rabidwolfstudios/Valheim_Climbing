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
        _skill.Name.Alias("$rw_climbing_skill_name");
        _skill.Description.Alias("$rw_climbing_skill_description");
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

    [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyRunStaminaDrain))]
    [HarmonyPriority(Priority.First)]
    private static class ChangeStamina
    {
        private static float _usage = 0f;
        private static Vector3 _previousPosition = Vector3.zero;

        private static bool Prefix(SEMan __instance, float baseDrain, ref float drain)
        {
            if (!__instance.m_character.IsPlayer() || !__instance.m_character.IsOnGround())
            {
                return true;
            }
            
            var isRiding = __instance.m_character.IsRiding();
            var slopeAngle = __instance.m_character.GetSlopeAngle();
            var slideAngle = __instance.m_character.GetSlideAngle();
            var currentPosition = __instance.m_character.GetTransform().localPosition;
            var previousY = _previousPosition.y;
            var currentY = currentPosition.y;
            var targetForSkillRaise = previousY + _experienceGainedDistance.Value; // prevents raising skill by running in place against a rock
            var targetForReset = previousY - _experienceResetDistance.Value;

            if (slopeAngle > slideAngle
                && !isRiding)
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
                var newUse = drain * reduceBy;

                if (_logDebugMessages)
                {
                    Debug.Log(string.Format(Localization.instance.Localize("$rw_climbing_debug_message"), currentSkillFactor, (1 - reduceBy) * 100, drain, newUse, _usage));
                }

                drain = newUse;
            }

            return true;
        }
    }
}
