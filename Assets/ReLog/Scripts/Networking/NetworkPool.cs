using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace ReLog.Networking
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class NetworkPool : UdonSharpBehaviour
    {
        public VRCPlayerApi[] Players = new VRCPlayerApi[0];
        public VRCPlayerApi LocalPlayer;
        public VRCPlayerApi Master;
        public bool IsReady { get; private set; }
        
        internal NetworkedLogger[] Loggers = new NetworkedLogger[0];
        private int lastPlayerCount;

        public NetworkedLogger Get(VRCPlayerApi player)
        {
            VRCPlayerApi localPlayer = VRC.SDKBase.Networking.LocalPlayer;
            if (localPlayer.isMaster)
            {
                foreach (NetworkedLogger networkedLogger in Loggers)
                {
                    VRCPlayerApi loggerOwner = networkedLogger.GetOwner();
                    if (localPlayer == player && localPlayer == loggerOwner && (networkedLogger.logs.Length > 0 || LoggersWithData(player) <= 0)) return networkedLogger;
                    // Player has already been assigned an object
                    if (loggerOwner == player && !player.isMaster) return networkedLogger;
                }
                // Player needs to be assigned a network logger
                foreach (NetworkedLogger networkedLogger in Loggers)
                {
                    VRCPlayerApi loggerOwner = networkedLogger.GetOwner();
                    if (loggerOwner == localPlayer && networkedLogger.logs.Length <= 0)
                    {
                        networkedLogger.logs = new string[0];
                        networkedLogger.logDates = new long[0];
                        networkedLogger.levels = new int[0];
                        networkedLogger.lastJson = "[]";
                        VRC.SDKBase.Networking.SetOwner(player, networkedLogger.gameObject);
                        Debug.Log($"Assigned {networkedLogger.gameObject.name} to {player.displayName} ({player.playerId})");
                        return networkedLogger;
                    }
                }
            }
            else
            {
                // Can't reassign, just find whatever matches
                foreach (NetworkedLogger networkedLogger in Loggers)
                {
                    if(networkedLogger.GetOwner() != player) continue;
                    if(networkedLogger.logs.Length > 0 || LoggersWithData(player) <= 0)
                        return networkedLogger;
                }
            }
            Debug.LogError("Could not rent NetworkedLogger!");
            return null;
        }

        public int LoggersWithData(VRCPlayerApi player)
        {
            int i = 0;
            foreach (NetworkedLogger networkedLogger in Loggers)
            {
                VRCPlayerApi loggerOwner = networkedLogger.GetOwner();
                if (player == loggerOwner && networkedLogger.logs.Length > 0) i++;
            }
            return i;
        }

        internal void Initialize()
        {
            LocalPlayer = VRC.SDKBase.Networking.LocalPlayer;
            Master = VRC.SDKBase.Networking.Master;
            int childCount = transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                GameObject g = transform.GetChild(i).gameObject;
                NetworkedLogger logger = g.GetComponent<NetworkedLogger>();
                if (logger == null) continue;
                logger.Initialize();
                Utils.Push(ref Loggers, logger);
            }
            Debug.Log($"Initialized {Loggers.Length} NetworkedLoggers!");
            IsReady = true;
        }

        internal bool _Update()
        {
            int playerCount = VRCPlayerApi.GetPlayerCount();
            bool updateBehaviours = false;
            if (lastPlayerCount != playerCount)
            {
                Players = new VRCPlayerApi[playerCount];
                VRCPlayerApi.GetPlayers(Players);
                Master = VRC.SDKBase.Networking.Master;
                updateBehaviours = true;
            }
            lastPlayerCount = playerCount;
            VRCPlayerApi local = VRC.SDKBase.Networking.LocalPlayer;
            if (local.isLocal && local.isMaster && updateBehaviours)
            {
                foreach (VRCPlayerApi player in Players)
                    Get(player);
            }
            return updateBehaviours;
        }
    }
}
