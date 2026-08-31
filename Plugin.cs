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
    [BepInPlugin("com.minesettimi.coopexfil", "UniversalCoopExfil", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> StickyAccess = null!;
        public static PatchManager PatchManager = null!;
        public static ManualLogSource PluginLogger = null!;
        
        private void Awake()
        {
            PatchManager = new PatchManager(this, true);
            PatchManager.EnablePatches();

            PluginLogger = Logger;

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

    public class ExfiltrationPointEnterPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ExfiltrationPoint), "IPhysicsTrigger.OnTriggerEnter");
        }

        [PatchPrefix]
        public static bool Prefix(Collider col, ExfiltrationPoint __instance)
        {
            GameWorld instance = Singleton<GameWorld>.Instance;
            Player? playerByCollider = instance.GetPlayerByCollider(col);
            if (playerByCollider == null)
            {
                Plugin.PluginLogger.LogError("Failed to get player by collider.");
                return false;
            }

            if ((instance.BtrController != null && instance.BtrController.BtrVehicle != null && instance.BtrController.BtrVehicle.IsPassenger(playerByCollider, out BTRPassenger _)) || playerByCollider.BtrState == EPlayerBtrState.Inside)
            {
                Plugin.PluginLogger.LogError("Player is in BTR.");
                return false;
            }
            
            if (ExfiltrationController.Instance.BannedPlayers.Contains(playerByCollider.Id))
            {
                Plugin.PluginLogger.LogError("Player is banned.");
                return false;
            }
            if (!__instance.InfiltrationMatch(playerByCollider))
            {
                Plugin.PluginLogger.LogError("Player doesn't have infiltration match.");
                return false;
            }
            if (__instance.Entered.Contains(playerByCollider))
            {
                Plugin.PluginLogger.LogError("Player has already entered point.");
                return false;
            }
            __instance.Entered.Add(playerByCollider);
            __instance.Proceed(playerByCollider, false);

            return false;
        }
    }
}