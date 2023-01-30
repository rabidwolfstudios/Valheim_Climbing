using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using ServerSync;

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
    private const string ModName = "Climbing";
    private const string ModVersion = "1.0.1";
    private const string ModGUID = "com.rabid-wolf.valheim.skills.climbing";

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

    public void Awake()
    {
        _ = new ClimbingSkill();
        SetConfiguration();

        ApplyHarmonyPatch();
    }
    private void SetConfiguration()
    {
        serverConfigLocked = SkillConfig("1 - General", "Lock Configuration", true, "If on, the configuration is locked and can be changed by server admins only.");
        configSync.AddLockingConfigEntry(serverConfigLocked);

        experienceGainedFactor = SkillConfig("2 - Climbing Adjustments", "Skill Experience Gain Factor", DEFAULT_EXPERIENCE_GAINED_FACTOR, new ConfigDescription($"Factor for experience gained for the climbing skill (Default ={DEFAULT_EXPERIENCE_GAINED_FACTOR})", new AcceptableValueRange<float>(0.01f, 1f)));
        experienceGainedFactor.SettingChanged += (_, _) => ClimbingSkill.SetSkillGainFactor(experienceGainedFactor.Value);

        experienceLoss = SkillConfig("2 - Climbing Adjustments", "Skill Experience Loss", DEFAULT_EXPERIENCE_LOSS, new ConfigDescription($"How much experience to lose in the climbing skill on death (Default = {DEFAULT_EXPERIENCE_LOSS})", new AcceptableValueRange<int>(0, 100)));
        experienceLoss.SettingChanged += (_, _) => ClimbingSkill.SetExperienceLossFactor(experienceLoss.Value);

        experienceGainedDistance = SkillConfig("2 - Climbing Adjustments", "Skill Experience Distance Gain", DEFAULT_EXPERIENCE_GAINED_DISTANCE, new ConfigDescription($"Distance player must run up slope to gain experience (Default = {DEFAULT_EXPERIENCE_GAINED_DISTANCE})", new AcceptableValueRange<float>(0.01f, 1f)));
        experienceGainedDistance.SettingChanged += (_, _) => ClimbingSkill.SetGainedDistance(experienceGainedDistance.Value);

        experienceResetDistance = SkillConfig("2 - Climbing Adjustments", "Skill Experience Distance Reset", DEFAULT_EXPERIENCE_RESET_DISTANCE, new ConfigDescription($"Distance player must come back down a slope from their most recent highest point that gained experience before they gain experience once more running up a slope (Default = {DEFAULT_EXPERIENCE_RESET_DISTANCE})", new AcceptableValueRange<float>(0.01f, 100.00f)));
        experienceResetDistance.SettingChanged += (_, _) => ClimbingSkill.SetResetDistance(experienceResetDistance.Value);

        staminaReductionPercentAtMax = SkillConfig("2 - Climbing Adjustments", "Stamina Reduction Factor At Max Level", DEFAULT_STAMINA_REDUCTION_PERCENT_AT_MAX, new ConfigDescription($"Maximum stamina reduction at Skill Level 100. The amount of stamina usage to reduce by when climbing. (Default = {DEFAULT_STAMINA_REDUCTION_PERCENT_AT_MAX}", new AcceptableValueRange<int>(0, 100)));
        staminaReductionPercentAtMax.SettingChanged += (_, _) => ClimbingSkill.SetStaminaReductionPercentage(staminaReductionPercentAtMax.Value);

        logDebugMessages = SkillConfig("3 - Other", "Log Debug Messages", false, "Log debug messages to BepInEx log? This can result in a lot of log spam");
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


