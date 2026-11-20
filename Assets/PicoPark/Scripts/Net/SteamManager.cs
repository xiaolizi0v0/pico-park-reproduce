using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace PicoPark
{
    /// <summary>
    /// Owns the Steam API lifetime and pumps callbacks manually, which is the only way a
    /// C# P/Invoke binding can receive them. Everything else talks to Steam through here.
    /// </summary>
    public class SteamManager : MonoBehaviour
    {
        public static SteamManager Instance { get; private set; }

        public bool Initialised { get; private set; }
        public string FailReason { get; private set; }

        public IntPtr User { get; private set; }
        public IntPtr Friends { get; private set; }
        public IntPtr Matchmaking { get; private set; }
        public IntPtr Networking { get; private set; }

        public ulong SteamId { get; private set; }
        public string PersonaName { get; private set; } = "玩家";

        public event Action<ulong, int> OnLobbyCreated;
        public event Action<ulong> OnLobbyEntered;
        public event Action<ulong> OnLobbyChatUpdate;
        public event Action<ulong> OnP2PSessionRequest;
        public event Action<ulong, ulong> OnGameLobbyJoinRequested;

        private int pipe;
        private readonly List<PendingCall> pending = new List<PendingCall>();

        private struct PendingCall
        {
            public ulong handle;
            public int callbackId;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Init();
        }

        private void Init()
        {
            try
            {
                // 打包环境自愈：确保 steam_appid.txt 在 exe 旁边（防止用户只拷了部分文件）
                string exeDir = System.IO.Path.GetDirectoryName(UnityEngine.Application.dataPath);
                string appIdPath = System.IO.Path.Combine(exeDir, "steam_appid.txt");
                if (!System.IO.File.Exists(appIdPath))
                {
                    System.IO.File.WriteAllText(appIdPath, "480");
                    Debug.LogWarning("[Steam] steam_appid.txt 缺失，已自动补写: " + appIdPath);
                }
                // steam_appid.txt 是按"当前工作目录"查找的：快捷方式/启动器可能改了 CWD，
                // 所以 CWD 不在 exe 目录时再补一份
                string cwdFile = System.IO.Path.Combine(System.Environment.CurrentDirectory, "steam_appid.txt");
                if (string.CompareOrdinal(System.Environment.CurrentDirectory, exeDir) != 0 && !System.IO.File.Exists(cwdFile))
                {
                    try { System.IO.File.WriteAllText(cwdFile, "480"); Debug.LogWarning("[Steam] 已补写 CWD 下的 steam_appid.txt: " + cwdFile); } catch { }
                }

                // 先用 InitFlat 拿到详细失败原因，再决定初始化成功与否
                byte[] errMsg = new byte[1024];
                int result = SteamNative.SteamAPI_InitFlat(errMsg);
                int len = 0;
                while (len < 1023 && errMsg[len] != 0) len++;
                string msg = System.Text.Encoding.UTF8.GetString(errMsg, 0, len);
                bool ok = result == 0;
                if (!ok)
                {
                    // 重试：Steam 客户端偶发未就绪
                    for (int retry = 0; retry < 3 && !ok; retry++)
                    {
                        System.Threading.Thread.Sleep(1000);
                        result = SteamNative.SteamAPI_InitFlat(errMsg);
                        ok = result == 0;
                    }
                }
                if (!ok)
                {
                    FailReason = "Steam 初始化失败 code=" + result + (string.IsNullOrEmpty(msg) ? "" : " (" + msg + ")");
                    Initialised = false;
                    Debug.LogWarning("[Steam] " + FailReason);
                    // 落一个调试文件到 exe 目录，方便排查打包环境
                    try
                    {
                        string dbg = "cwd=" + System.Environment.CurrentDirectory + "\n"
                            + "appidFile=" + System.IO.File.Exists(appIdPath) + "\n"
                            + "code=" + result + "\nmsg=" + msg + "\n";
                        System.IO.File.WriteAllText(System.IO.Path.Combine(exeDir, "steam_debug.txt"), dbg);
                    }
                    catch { }
                    return;
                }
                SteamNative.SteamAPI_ManualDispatch_Init();
                pipe = SteamNative.SteamAPI_GetHSteamPipe();
                User = SteamNative.SteamAPI_SteamUser_v023();
                Friends = SteamNative.SteamAPI_SteamFriends_v018();
                Matchmaking = SteamNative.SteamAPI_SteamMatchmaking_v009();
                Networking = SteamNative.SteamAPI_SteamNetworking_v006();
                if (Networking != IntPtr.Zero)
                    SteamNative.SteamAPI_ISteamNetworking_AllowP2PPacketRelay(Networking, true);
                if (User != IntPtr.Zero)
                    SteamId = SteamNative.SteamAPI_ISteamUser_GetSteamID(User);
                if (Friends != IntPtr.Zero)
                {
                    string n = SteamNative.PtrToUtf8(SteamNative.SteamAPI_ISteamFriends_GetPersonaName(Friends));
                    if (!string.IsNullOrEmpty(n)) PersonaName = n;
                }
                Initialised = true;
                Debug.Log("[Steam] 初始化成功 " + SteamId + " " + PersonaName);
            }
            catch (Exception e)
            {
                Initialised = false;
                FailReason = e.Message;
                Debug.LogWarning("[Steam] 初始化失败: " + e.Message);
            }
        }

        private void Update()
        {
            if (!Initialised) return;
            SteamNative.SteamAPI_ManualDispatch_RunFrame(pipe);
            SteamNative.CallbackMsg_t msg = new SteamNative.CallbackMsg_t();
            while (SteamNative.SteamAPI_ManualDispatch_GetNextCallback(pipe, ref msg))
            {
                try
                {
                    Dispatch(msg);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[Steam] callback error: " + e.Message);
                }
                finally
                {
                    SteamNative.SteamAPI_ManualDispatch_FreeLastCallback(pipe);
                }
            }
        }

        private void Dispatch(SteamNative.CallbackMsg_t msg)
        {
            switch (msg.m_iCallback)
            {
                case SteamNative.LobbyCreated:
                {
                    var data = Marshal.PtrToStructure<SteamNative.LobbyCreated_t>(msg.m_pubParam);
                    if (data.m_eResult == 1 && OnLobbyCreated != null) OnLobbyCreated(data.m_ulSteamIDLobby, data.m_eResult);
                    else if (data.m_eResult != 1 && OnLobbyCreated != null) OnLobbyCreated(0ul, data.m_eResult);
                    break;
                }
                case SteamNative.LobbyEnter:
                {
                    var data = Marshal.PtrToStructure<SteamNative.LobbyEnter_t>(msg.m_pubParam);
                    if (OnLobbyEntered != null) OnLobbyEntered(data.m_ulSteamIDLobby);
                    break;
                }
                case SteamNative.LobbyChatUpdate:
                {
                    var data = Marshal.PtrToStructure<SteamNative.LobbyChatUpdate_t>(msg.m_pubParam);
                    if (OnLobbyChatUpdate != null) OnLobbyChatUpdate(data.m_ulSteamIDLobby);
                    break;
                }
                case SteamNative.P2PSessionRequest:
                {
                    var data = Marshal.PtrToStructure<SteamNative.P2PSessionRequest_t>(msg.m_pubParam);
                    if (Networking != IntPtr.Zero)
                        SteamNative.SteamAPI_ISteamNetworking_AcceptP2PSessionWithUser(Networking, data.m_steamIDRemote);
                    if (OnP2PSessionRequest != null) OnP2PSessionRequest(data.m_steamIDRemote);
                    break;
                }
                case SteamNative.GameLobbyJoinRequested:
                {
                    var data = Marshal.PtrToStructure<SteamNative.GameLobbyJoinRequested_t>(msg.m_pubParam);
                    if (OnGameLobbyJoinRequested != null) OnGameLobbyJoinRequested(data.m_steamIDLobby, data.m_steamIDFriend);
                    break;
                }
            }
        }

        private void LateUpdate()
        {
            if (!Initialised || pending.Count == 0) return;
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                PendingCall pc = pending[i];
                bool failed = false;
                int size = pc.callbackId == SteamNative.LobbyCreated
                    ? Marshal.SizeOf<SteamNative.LobbyCreated_t>()
                    : Marshal.SizeOf<SteamNative.LobbyEnter_t>();
                IntPtr buf = Marshal.AllocHGlobal(size);
                try
                {
                    bool got = SteamNative.SteamAPI_ManualDispatch_GetAPICallResult(pipe, pc.handle, buf, size, pc.callbackId, ref failed);
                    if (!got) continue;
                    if (pc.callbackId == SteamNative.LobbyCreated)
                    {
                        var data = Marshal.PtrToStructure<SteamNative.LobbyCreated_t>(buf);
                        pending.RemoveAt(i);
                        if (OnLobbyCreated != null) OnLobbyCreated(data.m_eResult == 1 ? data.m_ulSteamIDLobby : 0ul, data.m_eResult);
                    }
                    else
                    {
                        var data = Marshal.PtrToStructure<SteamNative.LobbyEnter_t>(buf);
                        pending.RemoveAt(i);
                        if (OnLobbyEntered != null) OnLobbyEntered(data.m_ulSteamIDLobby);
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(buf);
                }
            }
        }

        private void OnApplicationQuit()
        {
            if (Initialised) SteamNative.SteamAPI_Shutdown();
            Initialised = false;
        }

        // ------------------------------------------------------------- wrappers

        public void CreateLobby(int maxMembers)
        {
            if (!Initialised || Matchmaking == IntPtr.Zero) return;
            ulong call = SteamNative.SteamAPI_ISteamMatchmaking_CreateLobby(Matchmaking, 1, maxMembers);
            pending.Add(new PendingCall { handle = call, callbackId = SteamNative.LobbyCreated });
        }

        public void JoinLobby(ulong id)
        {
            if (!Initialised || Matchmaking == IntPtr.Zero) return;
            ulong call = SteamNative.SteamAPI_ISteamMatchmaking_JoinLobby(Matchmaking, id);
            pending.Add(new PendingCall { handle = call, callbackId = SteamNative.LobbyEnter });
        }

        public void LeaveLobby(ulong id)
        {
            if (!Initialised || Matchmaking == IntPtr.Zero || id == 0) return;
            SteamNative.SteamAPI_ISteamMatchmaking_LeaveLobby(Matchmaking, id);
        }

        public void OpenInviteDialog(ulong lobbyId)
        {
            if (!Initialised || Friends == IntPtr.Zero) return;
            SteamNative.SteamAPI_ISteamFriends_ActivateGameOverlayInviteDialog(Friends, lobbyId);
        }

        public ulong LobbyOwner(ulong lobby)
        {
            if (!Initialised || Matchmaking == IntPtr.Zero) return 0;
            return SteamNative.SteamAPI_ISteamMatchmaking_GetLobbyOwner(Matchmaking, lobby);
        }

        public int LobbyMemberCount(ulong lobby)
        {
            if (!Initialised || Matchmaking == IntPtr.Zero || lobby == 0) return 0;
            return SteamNative.SteamAPI_ISteamMatchmaking_GetNumLobbyMembers(Matchmaking, lobby);
        }

        public ulong LobbyMemberByIndex(ulong lobby, int i)
        {
            if (!Initialised || Matchmaking == IntPtr.Zero || lobby == 0) return 0;
            return SteamNative.SteamAPI_ISteamMatchmaking_GetLobbyMemberByIndex(Matchmaking, lobby, i);
        }

        public void SetLobbyData(ulong lobby, string key, string value)
        {
            if (!Initialised || Matchmaking == IntPtr.Zero || lobby == 0) return;
            SteamNative.SteamAPI_ISteamMatchmaking_SetLobbyData(Matchmaking, lobby, key, value);
        }

        public string GetLobbyData(ulong lobby, string key)
        {
            if (!Initialised || Matchmaking == IntPtr.Zero || lobby == 0) return "";
            return SteamNative.PtrToUtf8(SteamNative.SteamAPI_ISteamMatchmaking_GetLobbyData(Matchmaking, lobby, key));
        }

        public void SetLobbyJoinable(ulong lobby, bool joinable)
        {
            if (!Initialised || Matchmaking == IntPtr.Zero || lobby == 0) return;
            SteamNative.SteamAPI_ISteamMatchmaking_SetLobbyJoinable(Matchmaking, lobby, joinable);
        }

        public bool Send(ulong remote, byte[] data, int len, bool reliable)
        {
            if (!Initialised || Networking == IntPtr.Zero || remote == 0) return false;
            return SteamNative.SendPacket(Networking, remote, data, len, reliable);
        }

        public bool Receive(byte[] buffer, out int len, out ulong from)
        {
            len = 0;
            from = 0;
            if (!Initialised || Networking == IntPtr.Zero) return false;
            uint size = 0;
            if (!SteamNative.SteamAPI_ISteamNetworking_IsP2PPacketAvailable(Networking, ref size, PPConfig.NetChannel))
                return false;
            if (size == 0 || size > buffer.Length) return false;
            uint read = 0;
            ulong sender = 0;
            bool ok = SteamNative.SteamAPI_ISteamNetworking_ReadP2PPacket(Networking, buffer, (uint)buffer.Length, ref read, ref sender, PPConfig.NetChannel);
            if (!ok) return false;
            len = (int)read;
            from = sender;
            return len > 0;
        }

        public void CloseSession(ulong remote)
        {
            if (!Initialised || Networking == IntPtr.Zero || remote == 0) return;
            SteamNative.SteamAPI_ISteamNetworking_CloseP2PSessionWithUser(Networking, remote);
        }

        // ------------------------------------------------------------- 好友房间列表

        public struct FriendRoom
        {
            public ulong steamId;
            public string name;
            public ulong lobbyId;
        }

        /// <summary>
        /// 扫描好友列表，返回正在本游戏房间里（可加入）的好友。
        /// FriendsOnly 大厅不出现在公共列表，只能通过好友的游戏信息拿 lobbyId。
        /// </summary>
        public List<FriendRoom> GetFriendRooms()
        {
            var rooms = new List<FriendRoom>();
            if (!Initialised || Friends == IntPtr.Zero) return rooms;
            const int flags = SteamNative.k_EFriendFlagImmediate;
            int n = SteamNative.SteamAPI_ISteamFriends_GetFriendCount(Friends, flags);
            for (int i = 0; i < n; i++)
            {
                ulong fid = SteamNative.SteamAPI_ISteamFriends_GetFriendByIndex(Friends, i, flags);
                if (fid == 0) continue;
                var info = new SteamNative.FriendGameInfo_t();
                if (!SteamNative.SteamAPI_ISteamFriends_GetFriendGamePlayed(Friends, fid, ref info)) continue;
                if (info.m_steamIDLobby == 0) continue;
                string name = SteamNative.PtrToUtf8(SteamNative.SteamAPI_ISteamFriends_GetFriendPersonaName(Friends, fid));
                rooms.Add(new FriendRoom { steamId = fid, name = string.IsNullOrEmpty(name) ? "好友" : name, lobbyId = info.m_steamIDLobby });
            }
            return rooms;
        }
        // ------------------------------------------------------------- 局内好友与邀请
        public struct FriendInfo
        {
            public ulong steamId;
            public string name;
            public int personaState;   // 0:离线, 1:在线, 2:忙碌, 3:离开, 4:打盹, 5:交易, 6:玩游戏
            public bool inOurLobby;    // 是否已在当前房间中
            public bool inOtherGame;   // 是否在玩其他游戏
        }

        public List<FriendInfo> GetFriendList(ulong currentLobbyId)
        {
            var list = new List<FriendInfo>();
            if (!Initialised || Friends == IntPtr.Zero) return list;
            const int flags = SteamNative.k_EFriendFlagImmediate;
            int n = SteamNative.SteamAPI_ISteamFriends_GetFriendCount(Friends, flags);
            for (int i = 0; i < n; i++)
            {
                ulong fid = SteamNative.SteamAPI_ISteamFriends_GetFriendByIndex(Friends, i, flags);
                if (fid == 0) continue;
                string name = SteamNative.PtrToUtf8(SteamNative.SteamAPI_ISteamFriends_GetFriendPersonaName(Friends, fid));
                int state = SteamNative.SteamAPI_ISteamFriends_GetFriendPersonaState(Friends, fid);
                var info = new SteamNative.FriendGameInfo_t();
                bool inGame = SteamNative.SteamAPI_ISteamFriends_GetFriendGamePlayed(Friends, fid, ref info);
                bool inOur = currentLobbyId > 0 && info.m_steamIDLobby == currentLobbyId;
                list.Add(new FriendInfo
                {
                    steamId = fid,
                    name = string.IsNullOrEmpty(name) ? "好友 " + fid : name,
                    personaState = state,
                    inOurLobby = inOur,
                    inOtherGame = inGame && !inOur
                });
            }
            list.Sort((a, b) =>
            {
                int rankA = a.inOurLobby ? 0 : (a.personaState > 0 ? 1 : 2);
                int rankB = b.inOurLobby ? 0 : (b.personaState > 0 ? 1 : 2);
                if (rankA != rankB) return rankA.CompareTo(rankB);
                return string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase);
            });
            return list;
        }

        public bool InviteFriendToLobby(ulong lobbyId, ulong friendSteamId)
        {
            if (!Initialised || Matchmaking == IntPtr.Zero || lobbyId == 0 || friendSteamId == 0) return false;
            return SteamNative.SteamAPI_ISteamMatchmaking_InviteUserToLobby(Matchmaking, lobbyId, friendSteamId);
        }
    }
}
