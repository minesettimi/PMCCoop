using System.Reflection;
using BepInEx;
using EFT.Interactive;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace PMCCoop
{
    [BepInPlugin("com.minesettimi.pmccoop", "PMCCoop", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            new ScavCooperationPatch().Enable();
            new ScavCooperationMetPatch().Enable();
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
        public static bool Prefix(ExfiltrationPoint point)
        {
            //vanilla scav spot absolutely sucks and can flip on and off seemingly at random, if they start it, let them keep it, this isn't PVP
            if (point._status == EExfiltrationStatus.RegularMode)
                return false;
            
            point.SetStatusLogged(point.Entered.Count > 1 ? EExfiltrationStatus.RegularMode : EExfiltrationStatus.UncompleteRequirements, "CooperationRequirement");
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
            __result = point.Status == EExfiltrationStatus.RegularMode;
            return false;
        }
    }
}