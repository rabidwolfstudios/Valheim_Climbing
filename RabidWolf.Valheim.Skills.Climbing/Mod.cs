using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using ServerSync;
using LocalizationManager;

namespace RabidWolf.Valheim.Skills;

/// <summary>
/// Skill inspired and suggested by admins and players at the Kazuals discord server
/// https://discord.gg/aWSJbq2hZz
/// Rabid Wolf Studios is my LLC which one day hopes to develop a game you might like to play
/// For now, I'm supporting a community and game I enjoy playing
/// 
/// Thanks to Smoothbrain for SkillManager and his example skills and source code.
/// </summary>
[BepInPlugin(ModGUID, ModName, ModVersion)]
public class Mod : BaseUnityPlugin
{
    /* Mod definition */
    public const string ModName = "Climbing";
    public const string ModVersion = "1.0.5";
    public const string ModGUID = "com.rabid-wolf.valheim.skills.climbing";

    /* Configuration */
    private static ConfigEntry<bool> serverConfigLocked = null!;
    private static ConfigEntry<float> experienceGainedFactor = null!;
    private static ConfigEntry<int> experienceLoss = null!;
    private static ConfigEntry<float> experienceGainedDistance = null!;
    private static ConfigEntry<float> experienceResetDistance = null!;
    private static ConfigEntry<int> staminaReductionPercentAtMax = null;
    private static ConfigEntry<bool> logDebugMessages = null!;

    private const float DEFAULT_EXPERIENCE_GAINED_FACTOR = 0.075f;
    private const int DEFAULT_EXPERIENCE_LOSS = 5;
    private const float DEFAULT_EXPERIENCE_GAINED_DISTANCE = 0.2f;
    private const float DEFAULT_EXPERIENCE_RESET_DISTANCE = 1.00f;
    private const int DEFAULT_STAMINA_REDUCTION_PERCENT_AT_MAX = 34;

    private static readonly ConfigSync configSync = new(ModGUID) { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };

    private ConfigEntry<T> SkillConfig<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
    {
        ConfigEntry<T> configEntry = Config.Bind(group, name, value, description);

        SyncedConfigEntry<T> syncedConfigEntry = configSync.AddConfigEntry(configEntry);
        syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

        return configEntry;
    }
    private ConfigEntry<T> SkillConfig<T>(string group, string name, T value, string description, bool synchronizedSetting = true) => SkillConfig(group, name, value, new ConfigDescription(description), synchronizedSetting);

    private delegate string LocalizeFunc(string text);
    private LocalizeFunc Localize;

    public void Awake()
    {
        Localizer.Load();
        Localize = Localization.instance.Localize;

        _ = new ClimbingSkill();
        SetConfiguration();

        ApplyHarmonyPatch();
    }
    private void SetConfiguration()
    {
        serverConfigLocked = SkillConfig(Localize("$rw_climbing_config_category_general"), Localize("$rw_climbing_config_lock_configuration_name"), true,Localize("$rw_climbing_config_lock_configuration_description"));
        configSync.AddLockingConfigEntry(serverConfigLocked);

        experienceGainedFactor = SkillConfig(Localize("$rw_climbing_config_category_adjustments"), Localize("$rw_climbing_config_skill_experience_gain_factor_name"), DEFAULT_EXPERIENCE_GAINED_FACTOR, new ConfigDescription(string.Format(Localize($"rw_climbing_config_skill_experience_gain_factor_description"), DEFAULT_EXPERIENCE_GAINED_FACTOR), new AcceptableValueRange<float>(0.01f, 1f)));
        experienceGainedFactor.SettingChanged += (_, _) => ClimbingSkill.SetSkillGainFactor(experienceGainedFactor.Value);

        experienceLoss = SkillConfig(Localize("$rw_climbing_config_category_adjustments"), Localize("$rw_climbing_config_skill_experience_loss_name"), DEFAULT_EXPERIENCE_LOSS, new ConfigDescription(string.Format(Localize("$rw_climbing_config_skill_experience_loss_description"), DEFAULT_EXPERIENCE_LOSS), new AcceptableValueRange<int>(0, 100)));
        experienceLoss.SettingChanged += (_, _) => ClimbingSkill.SetExperienceLossFactor(experienceLoss.Value);

        experienceGainedDistance = SkillConfig(Localize("$rw_climbing_config_category_adjustments"), Localize("$rw_climbing_config_skill_experience_distance_gain_name"), DEFAULT_EXPERIENCE_GAINED_DISTANCE, new ConfigDescription(string.Format(Localize("$rw_climbing_config_skill_experience_distance_gain_description"), DEFAULT_EXPERIENCE_GAINED_DISTANCE), new AcceptableValueRange<float>(0.01f, 1f)));
        experienceGainedDistance.SettingChanged += (_, _) => ClimbingSkill.SetGainedDistance(experienceGainedDistance.Value);

        experienceResetDistance = SkillConfig(Localize("$rw_climbing_config_category_adjustments"), Localize("$rw_climbing_config_skill_experience_distance_reset_name"), DEFAULT_EXPERIENCE_RESET_DISTANCE, new ConfigDescription(string.Format(Localize($"rw_climbing_config_skill_experience_distance_reset_description"), DEFAULT_EXPERIENCE_RESET_DISTANCE), new AcceptableValueRange<float>(0.01f, 100.00f)));
        experienceResetDistance.SettingChanged += (_, _) => ClimbingSkill.SetResetDistance(experienceResetDistance.Value);

        staminaReductionPercentAtMax = SkillConfig(Localize("$rw_climbing_config_category_adjustments"), Localize("$rw_climbing_config_stamina_reduction_factor_name"), DEFAULT_STAMINA_REDUCTION_PERCENT_AT_MAX, new ConfigDescription(string.Format(Localize("$rw_climbing_config_stamina_reduction_factor_description"), DEFAULT_STAMINA_REDUCTION_PERCENT_AT_MAX), new AcceptableValueRange<int>(0, 100)));
        staminaReductionPercentAtMax.SettingChanged += (_, _) => ClimbingSkill.SetStaminaReductionPercentage(staminaReductionPercentAtMax.Value);

        logDebugMessages = SkillConfig(Localize("$rw_climbing_config_category_other"), Localize("$rw_climbing_config_log_debug_messages_name"), false, Localize("$rw_climbing_config_log_debug_messages_description"));
        logDebugMessages.SettingChanged += (_, _) => ClimbingSkill.SetLogDebugMessages(logDebugMessages.Value);

        ClimbingSkill.SetSkillGainFactor(experienceGainedFactor.Value);
        ClimbingSkill.SetExperienceLossFactor(experienceLoss.Value);
        ClimbingSkill.SetGainedDistance(experienceGainedDistance.Value);
        ClimbingSkill.SetResetDistance(experienceResetDistance.Value);
        ClimbingSkill.SetStaminaReductionPercentage(staminaReductionPercentAtMax.Value);
        ClimbingSkill.SetLogDebugMessages(logDebugMessages.Value);
    }

    private void ApplyHarmonyPatch()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        Harmony harmony = new(ModGUID);
        harmony.PatchAll(assembly);
    }
}


