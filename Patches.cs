using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using Fika.Core.Main.Components;
using Fika.Core.Main.GameMode;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;
using Fika.Core.Networking.LiteNetLib;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace UniversalCoopExfil;

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
            
            if (Singleton<IFikaGame>.Instance is BaseLocalGame<EftGamePlayerOwner> game)
            {
                game.UpdateExfiltrationUi(point, point.Entered.Contains(Singleton<IFikaNetworkManager>.Instance.CoopHandler.MyPlayer));
            }
        }
        else
        {
            point.SetStatusLogged(EExfiltrationStatus.UncompleteRequirements, "CooperationRequirement");
        }
        
        Plugin.PluginLogger.LogInfo($"Point update, entered count: {point.Entered.Count}, status: {point.Status}");

        return false;
    }
}

public class ScavCooperationEnterPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(ScavCooperationRequirement), nameof(ScavCooperationRequirement.Enter));
    }

    [PatchPrefix]
    public static void Prefix(Player player, ExfiltrationPoint point)
    {
        if (FikaBackendUtils.IsHeadless ||
            player != Singleton<IFikaNetworkManager>.Instance.CoopHandler.MyPlayer) return;
        
        IFikaNetworkManager networkManager = Singleton<IFikaNetworkManager>.Instance;
        ExfilEnteredPacket packet = new()
        {
            NetId = networkManager.NetId,
            Entered = true,
            Name = point.Settings.Name
        };
            
        networkManager.SendData(ref packet, DeliveryMethod.ReliableOrdered, true);
    }
}

public class ScavCooperationExitPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(ScavCooperationRequirement), nameof(ScavCooperationRequirement.Exit));
    }

    [PatchPrefix]
    public static void Prefix(Player player, ExfiltrationPoint point)
    {
        if (FikaBackendUtils.IsHeadless ||
            player != Singleton<IFikaNetworkManager>.Instance.CoopHandler.MyPlayer) return;
        
        IFikaNetworkManager networkManager = Singleton<IFikaNetworkManager>.Instance;
        ExfilEnteredPacket packet = new()
        {
            NetId = networkManager.NetId,
            Entered = false,
            Name = point.Settings.Name
        };
            
        networkManager.SendData(ref packet, DeliveryMethod.ReliableOrdered, true);
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