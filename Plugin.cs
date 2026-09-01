using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Comfort.Common;
using CommonAssets.Scripts.Game;
using EFT;
using EFT.Interactive;
using EFT.Vehicle;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace UniversalCoopExfil
{
    [BepInPlugin("com.minesettimi.coopexfil", "UniversalCoopExfil", "1.0.1")]
    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> StickyAccess = null!;
        public static PatchManager PatchManager = null!;
        
        private void Awake()
        {
            PatchManager = new PatchManager(this, true);
            PatchManager.EnablePatches();

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
            
            if (point.Status != EExfiltrationStatus.UncompleteRequirements)
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
            __result = point.Status != EExfiltrationStatus.UncompleteRequirements;
            return false;
        }
    }

    public class ExfiltrationPointInfilMatchPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(SharedExfiltrationPoint), nameof(SharedExfiltrationPoint.InfiltrationMatch));
        }

        [PatchPrefix]
        public static bool Prefix(Player player, ref bool __result)
        {
            GameWorld? instance = Singleton<GameWorld>.Instance;
            if (instance == null)
            {
                return true;
            }
            
            if (player.IsYourPlayer)
            {
                return true;
            }
            
            __result = true;
            return false;
        }
    }
}