using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using EFT.Interactive;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace UniversalCoopExfil
{
    [BepInPlugin("com.minesettimi.coopexfil", "UniversalCoopExfil", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> StickyAccess = null!;
        
        private void Awake()
        {
            new ScavCooperationPatch().Enable();
            new ScavCooperationMetPatch().Enable();

            StickyAccess = Config.Bind("Coop Exfil Settings", "Sticky Access", true,
                "If enabled, the exfil point will stay open after the conditions are first met.");
        }
    }

    public class ScavCooperationPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ScavCooperationRequirement),
                nameof(ScavCooperationRequirement.UpdateStatus));
        }
        
        [PatchPrefix]
        public static bool Prefix(ExfiltrationPoint point, ScavCooperationRequirement __instance)
        {
            if (!Plugin.StickyAccess.Value)
            {
                point.SetStatusLogged(point.Entered.Count > 1 ? EExfiltrationStatus.Countdown : EExfiltrationStatus.UncompleteRequirements, "CooperationRequirement");
                return false;
            }
            
            if (point._status == EExfiltrationStatus.RegularMode)
                return false;
            
            if (point.Entered.Count > 1)
            {
                point.SetStatusLogged(EExfiltrationStatus.RegularMode, "CooperationRequirement");
                __instance._unbind.Invoke();
            }
            else
            {
                point.SetStatusLogged(EExfiltrationStatus.UncompleteRequirements, "CooperationRequirement");
            }
            
            return false;
        }
    }
    
    public class ScavCooperationMetPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ScavCooperationRequirement), nameof(ScavCooperationRequirement.Met));
        }

        [PatchPrefix]
        public static bool Prefix(ExfiltrationPoint point, ref bool __result)
        {
            if (!Plugin.StickyAccess.Value)
                return true;
            
            __result = point.Status == EExfiltrationStatus.RegularMode;
            return false;
        }
    }
}