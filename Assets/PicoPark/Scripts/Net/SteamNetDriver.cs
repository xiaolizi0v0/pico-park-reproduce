using System.Collections.Generic;
using UnityEngine;

namespace PicoPark
{
    public enum NetMode { Offline, Host, Client }

    /// <summary>Who the game loop asks for input each tick.</summary>
    public interface INetDriver
    {
        NetMode Mode { get; }
        InputCmd RequestInput(int playerIndex, int tick);
        void Advance(SimWorld world);
        bool IsLocalPlayer(int playerIndex);
        int LocalPlayerIndex { get; }
        int RemotePlayerCount { get; }
    }

    /// <summary>
    /// Host-authoritative netcode over Steam lobbies + Steam P2P.
    /// Host runs the only simulation and broadcasts full 60Hz snapshots.
    /// Clients never step the world - they render snapshots directly and
    /// upload their keyboard input each frame. Deterministic and simple;
    /// remote inputs arrive with one RTT of latency.
    /// </summary>
    public class SteamNetDriver : MonoBehaviour, INetDriver
    {
        public NetMode Mode { get; set; } = NetMode.Offline;
        public int LocalPlayerIndex { get; private set; }
        public int RemotePlayerCount { get; private set; }

        public ulong LobbyId { get; private set; }
        public string Status { get; private set; } = "离线";
        public float RttTicks { get { return 0f; } } // 快照模式下不再估算，UI 显示为空

        public System.Action<int, int> OnLevelFromHost;
        public System.Action OnLobbyChanged;

        private InputCmd[] lastRemoteInput = new InputCmd[PPConfig.MaxPlayers];
        private List<ulong> peers = new List<ulong>();
        private byte[] recvBuf = new byte[4096];
        private ByteWriter writer = new ByteWriter(4096);
        private ByteReader reader;

        private int pendingRemoteLevel = -999;
        private int pendingRemotePlayers = -1;
        private bool hostLevelDirty = true;
        private int hostLevelIndex = -1;
        private int hostPlayerCount = 1;
        private SimWorld activeWorld;

        private LocalInputManager localInput;

        public void Bind(LocalInputManager input) { localInput = input; }

        public bool SteamReady
        {
            get { return SteamManager.Instance != null && SteamManager.Instance.Initialised; }
        }

        // ------------------------------------------------------------------ lobby

        public void CreateLobby(int maxPlayers)
        {
            if (!SteamReady) { Status = "Steam 不可用，转为本地同屏"; Mode = NetMode.Offline; return; }
            Mode = NetMode.Host;
            LocalPlayerIndex = 0;
            Status = "正在创建房间…";
            SteamManager.Instance.OnLobbyCreated -= OnLobbyCreated;
            SteamManager.Instance.OnLobbyCreated += OnLobbyCreated;
            SteamManager.Instance.CreateLobby(maxPlayers);
        }

        public void JoinLobby(ulong id)
        {
            if (!SteamReady) { Status = "Steam 不可用"; return; }
            Mode = NetMode.Client;
            Status = "正在加入…";
            SteamManager.Instance.OnLobbyEntered -= OnLobbyEntered;
            SteamManager.Instance.OnLobbyEntered += OnLobbyEntered;
            SteamManager.Instance.JoinLobby(id);
        }

        public void LeaveLobby()
        {
            if (SteamManager.Instance != null && LobbyId != 0) SteamManager.Instance.LeaveLobby(LobbyId);
            foreach (ulong p in peers)
                if (SteamManager.Instance != null) SteamManager.Instance.CloseSession(p);
            peers.Clear();
            LobbyId = 0;
            Mode = NetMode.Offline;
            LocalPlayerIndex = 0;
            Status = "离线";
            pendingRemoteLevel = -999;
            pendingRemotePlayers = -1;
        }

        public void OpenInvite()
        {
            if (SteamManager.Instance != null && LobbyId != 0 && Mode == NetMode.Host)
                SteamManager.Instance.OpenInviteDialog(LobbyId);
        }

        public bool InviteUser(ulong friendSteamId)
        {
            if (SteamManager.Instance != null && LobbyId != 0)
                return SteamManager.Instance.InviteFriendToLobby(LobbyId, friendSteamId);
            return false;
        }

        private void OnLobbyCreated(ulong id, int result)
        {
            if (id != 0)
            {
                LobbyId = id;
                Mode = NetMode.Host;
                LocalPlayerIndex = 0;
                Status = "房间已创建";
                hostLevelDirty = true; // 房间就绪后补发关卡，之前会丢
                if (SteamManager.Instance != null)
                {
                    SteamManager.Instance.SetLobbyData(id, "game", "picopark");
                    SteamManager.Instance.SetLobbyJoinable(id, true);
                }
                if (OnLobbyChanged != null) OnLobbyChanged();
            }
            else
            {
                Status = "创建房间失败 (" + result + ")";
                Mode = NetMode.Offline;
            }
        }

        private void OnLobbyEntered(ulong id)
        {
            LobbyId = id;
            Mode = NetMode.Client;
            Status = "已加入房间";
            RefreshMembers();
            pendingRemoteLevel = PicoLoop.LobbyIndex;
            pendingRemotePlayers = Mathf.Max(1, 1 + peers.Count);
            if (OnLobbyChanged != null) OnLobbyChanged();
        }

        /// <summary>
        /// 座位分配（全员一致）：房主固定 0 号，其余成员按 steamId 升序排 1..n。
        /// 每台机器用同一规则算出的座位必然唯一，不会出现两人都控制同一只猫。
        /// </summary>
        private void RefreshMembers()
        {
            if (!SteamReady || LobbyId == 0) return;
            ulong me = SteamManager.Instance.SteamId;
            ulong owner = SteamManager.Instance.LobbyOwner(LobbyId);
            int count = SteamManager.Instance.LobbyMemberCount(LobbyId);
            List<ulong> all = new List<ulong>(count);
            for (int i = 0; i < count; i++)
            {
                ulong m = SteamManager.Instance.LobbyMemberByIndex(LobbyId, i);
                if (m != 0) all.Add(m);
            }
            all.Sort((a, b) =>
            {
                if (a == owner) return -1;
                if (b == owner) return 1;
                return a.CompareTo(b);
            });
            peers.Clear();
            int myIndex = 0;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] == me) myIndex = i;
                else peers.Add(all[i]);
            }
            LocalPlayerIndex = myIndex;
            RemotePlayerCount = peers.Count;
        }

        public List<ulong> Peers { get { return peers; } }

        // ------------------------------------------------------------------ driver

        public bool IsLocalPlayer(int idx)
        {
            if (Mode == NetMode.Offline) return true;
            return idx == LocalPlayerIndex;
        }

        /// <summary>
        /// 主机每个 tick 对全部座位调用：本地座位读本机键盘（联机时本机只有一个键盘 = 0 号位），
        /// 远端座位用最近一次收到的输入。
        /// </summary>
        public InputCmd RequestInput(int playerIndex, int tick)
        {
            if (Mode == NetMode.Offline || playerIndex == LocalPlayerIndex)
                return localInput != null ? localInput.Read(0) : InputCmd.Empty;
            if (playerIndex >= 0 && playerIndex < PPConfig.MaxPlayers) return lastRemoteInput[playerIndex];
            return InputCmd.Empty;
        }

        public void Advance(SimWorld world)
        {
            activeWorld = world;
            if (Mode == NetMode.Host) HostAdvance();
            else if (Mode == NetMode.Client) ClientAdvance(world);
        }

        private void HostAdvance()
        {
            if (SteamReady && LobbyId != 0)
            {
                RefreshMembers();
                int count = 1 + peers.Count;
                if (count != hostPlayerCount)
                {
                    hostPlayerCount = count;
                    hostLevelDirty = true; // 有人进出房间 → 重发关卡（并按新人数重开本关）
                    if (OnLevelFromHost != null) OnLevelFromHost(hostLevelIndex, hostPlayerCount);
                }
            }
            if (hostLevelDirty)
            {
                hostLevelDirty = false;
                BroadcastLevel(hostLevelIndex, hostPlayerCount);
            }
            Broadcast(activeWorld);
        }

        /// <summary>客户端不步进世界：只处理主机下发的关卡切换，并每帧上报自己的键盘输入。</summary>
        public void ClientAdvance(SimWorld world)
        {
            if (pendingRemoteLevel != -999)
            {
                int lv = pendingRemoteLevel;
                int pc = pendingRemotePlayers;
                pendingRemoteLevel = -999;
                pendingRemotePlayers = -1;
                if (OnLevelFromHost != null) OnLevelFromHost(lv, pc);
                return;
            }
            if (!SteamReady || world == null) return;
            ulong host = HostSteamId();
            if (host == 0 || host == SteamManager.Instance.SteamId) return;
            InputCmd cmd = localInput != null ? localInput.Read(0) : InputCmd.Empty;
            int len = NetSerial.WriteInput(writer, LocalPlayerIndex, world.tick + 1, cmd.buttons);
            SteamManager.Instance.Send(host, writer.buf, len, false);
        }

        // ------------------------------------------------------------------ level

        public void HostSetLevel(int levelIndex, int playerCount)
        {
            hostLevelIndex = levelIndex;
            hostPlayerCount = playerCount;
            hostLevelDirty = true;
        }

        private void BroadcastLevel(int levelIndex, int playerCount)
        {
            if (!SteamReady) return;
            int len = NetSerial.WriteLevel(writer, levelIndex, playerCount);
            foreach (ulong p in peers) SteamManager.Instance.Send(p, writer.buf, len, true);
        }

        private void Broadcast(SimWorld world)
        {
            if (!SteamReady || peers.Count == 0) return;
            NetSerial.WriteSnapshot(writer, world);
            foreach (ulong p in peers) SteamManager.Instance.Send(p, writer.buf, writer.len, false);
        }

        private ulong HostSteamId()
        {
            if (!SteamReady || LobbyId == 0) return 0;
            return SteamManager.Instance.LobbyOwner(LobbyId);
        }

        // ------------------------------------------------------------------ pump

        private void Update()
        {
            if (!SteamReady || Mode == NetMode.Offline) return;
            int len;
            ulong from;
            int guard = 0;
            while (SteamManager.Instance.Receive(recvBuf, out len, out from) && guard++ < 32)
            {
                reader = new ByteReader(recvBuf, len);
                byte type = reader.U8();
                if (type == NetMsg.Input && Mode == NetMode.Host)
                {
                    int pi = reader.U8();
                    reader.I32(); // tick：仅供调试，主机统一按"最近输入"应用
                    InputCmd cmd = new InputCmd();
                    cmd.buttons = (Btn)reader.U8();
                    if (pi >= 0 && pi < PPConfig.MaxPlayers) lastRemoteInput[pi] = cmd;
                }
                else if (type == NetMsg.Snapshot && Mode == NetMode.Client)
                {
                    if (activeWorld != null) NetSerial.ApplySnapshot(reader, activeWorld);
                }
                else if (type == NetMsg.Level && Mode == NetMode.Client)
                {
                    pendingRemoteLevel = (short)reader.I16();
                    pendingRemotePlayers = reader.U8();
                }
            }
        }

        public void RegisterWorld(SimWorld world) { activeWorld = world; }
    }
}
