using System;
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
            point.SetStatusLogged(point.Entered.Count > 1 ? EExfiltrationStatus.Countdown : EExfiltrationStatus.UncompleteRequirements, "CooperationRequirement");
            return false;
        }
    }
}