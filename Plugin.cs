using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Comfort.Common;
using CommonAssets.Scripts.Game;
using EFT.Interactive;
using Fika.Core.Main.Components;
using Fika.Core.Main.GameMode;
using Fika.Core.Main.Players;
using Fika.Core.Main.Utils;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking;
using SPT.Reflection.Patching;

namespace UniversalCoopExfil
{
    [BepInPlugin("com.minesettimi.coopexfil", "UniversalCoopExfil", "1.0.0")]
    [BepInDependency("com.fika.core")]
    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> StickyAccess = null!;
        public static ManualLogSource PluginLogger = null!;
        public static PatchManager PatchManager = null!;
        
        private void Awake()
        {
            PatchManager = new PatchManager(this, true);
            PatchManager.EnablePatches();

            PluginLogger = Logger;
            
            StickyAccess = Config.Bind("Coop Exfil Settings", "Sticky Access", true,
                "If enabled, the exfil point will stay open after the conditions are first met.");
            
            FikaEventDispatcher.SubscribeEvent<FikaNetworkManagerCreatedEvent>(OnNetworkManagerCreated);
        }

        private void OnNetworkManagerCreated(FikaNetworkManagerCreatedEvent @event)
        {
            switch (@event.Manager)
            {
                case FikaClient client:
                    client.RegisterPacket<ExfilEnteredPacket>(HandleExfilEntered);
                    break;
                case FikaServer server:
                    server.RegisterPacket<ExfilEnteredPacket>(HandleExfilEntered);
                    break;
            }
        }

        private void HandleExfilEntered(ExfilEnteredPacket packet)
        {
            CoopHandler coopHandler = FikaBackendUtils.IsClient
                ? Singleton<FikaClient>.Instance.CoopHandler
                : Singleton<FikaServer>.Instance.CoopHandler;
                
            if (!coopHandler.Players.TryGetValue(packet.NetId, out FikaPlayer? player))
            {
                return;
            }

            foreach (ExfiltrationPoint exfilPoint in ExfiltrationController.Instance.ExfiltrationPoints)
            {
                if (exfilPoint.Settings.Name != packet.Name)
                    return;

                if (packet.Entered)
                {
                    exfilPoint.Entered.Add(player);
                }
                else
                {
                    exfilPoint.Entered.Remove(player);
                }

                break;
            }

            
        }
    }
}