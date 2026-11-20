using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace PicoPark
{
    /// <summary>
    /// Bare-bones P/Invoke binding over the Steamworks flat API (SDK 1.65).
    /// Only the pieces a small co-op game needs: init/shutdown, manual callback dispatch,
    /// lobbies, P2P packets and a couple of identity helpers. No third-party wrapper.
    /// </summary>
    public static class SteamNative
    {
        public const string Dll = "steam_api64";

        public const int k_iSteamUserCallbacks = 100;
        public const int k_iSteamMatchmakingCallbacks = 500;
        public const int k_iSteamNetworkingCallbacks = 1200;

        // ---- callback ids ----
        public const int LobbyCreated = 513;
        public const int LobbyEnter = 504;
        public const int LobbyChatUpdate = 506;
        public const int P2PSessionRequest = 1202;
        public const int P2PSessionConnectFail = 1203;
        public const int GameLobbyJoinRequested = 333;
        public const int LobbyDataUpdate = 505;

        [StructLayout(LayoutKind.Sequential)]
        public struct CallbackMsg_t
        {
            public int m_hSteamUser;
            public int m_iCallback;
            public IntPtr m_pubParam;
            public int m_cubParam;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct LobbyCreated_t
        {
            public int m_eResult;
            public ulong m_ulSteamIDLobby;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct LobbyEnter_t
        {
            public ulong m_ulSteamIDLobby;
            public uint m_rgfChatPermissions;
            [MarshalAs(UnmanagedType.I1)]
            public bool m_bLocked;
            public uint m_EChatRoomEnterResponse;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct LobbyChatUpdate_t
        {
            public ulong m_ulSteamIDLobby;
            public ulong m_ulSteamIDUserChanged;
            public ulong m_ulSteamIDMakingChange;
            public uint m_rgfChatMemberStateChange;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct P2PSessionRequest_t
        {
            public ulong m_steamIDRemote;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct P2PSessionConnectFail_t
        {
            public ulong m_steamIDRemote;
            public byte m_eP2PSessionError;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct GameLobbyJoinRequested_t
        {
            public ulong m_steamIDLobby;
            public ulong m_steamIDFriend;
        }

        // ---- core ----
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool SteamAPI_InitSafe();

        /// <summary>带错误信息的初始化（SteamErrMsg = char[1024]）。返回 ESteamAPIInitResult，0=成功。</summary>
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SteamAPI_InitFlat(byte[] pOutErrMsg);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SteamAPI_Shutdown();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool SteamAPI_RestartAppIfNecessary(uint unOwnAppID);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SteamAPI_ManualDispatch_Init();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SteamAPI_ManualDispatch_RunFrame(int hSteamPipe);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool SteamAPI_ManualDispatch_GetNextCallback(int hSteamPipe, ref CallbackMsg_t msg);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SteamAPI_ManualDispatch_FreeLastCallback(int hSteamPipe);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool SteamAPI_ManualDispatch_GetAPICallResult(int hSteamPipe, ulong hSteamAPICall,
            IntPtr pCallback, int cubCallback, int iCallbackExpected, ref bool pbFailed);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SteamAPI_GetHSteamPipe();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SteamAPI_ReleaseCurrentThreadMemory();

        // ---- interface accessors ----
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr SteamAPI_SteamUser_v023();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr SteamAPI_SteamFriends_v018();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr SteamAPI_SteamMatchmaking_v009();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr SteamAPI_SteamNetworking_v006();

        // ---- 好友房间（"加入房间"列表用）----
        public const int k_EFriendFlagImmediate = 0x04;

        [StructLayout(LayoutKind.Sequential)]
        public struct FriendGameInfo_t
        {
            public ulong m_gameID;
            public uint m_unGameIP;
            public ushort m_usGamePort;
            public ushort m_usQueryPort;
            public ulong m_steamIDLobby;
        }

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SteamAPI_ISteamFriends_GetFriendCount(IntPtr self, int iFriendFlags);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern ulong SteamAPI_ISteamFriends_GetFriendByIndex(IntPtr self, int iFriend, int iFriendFlags);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool SteamAPI_ISteamFriends_GetFriendGamePlayed(IntPtr self, ulong steamIDFriend, ref FriendGameInfo_t pFriendGameInfo);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr SteamAPI_ISteamFriends_GetFriendPersonaName(IntPtr self, ulong steamIDFriend);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SteamAPI_ISteamFriends_GetFriendPersonaState(IntPtr self, ulong steamIDFriend);

        // ---- user / friends ----
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern ulong SteamAPI_ISteamUser_GetSteamID(IntPtr self);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool SteamAPI_ISteamUser_BLoggedOn(IntPtr self);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr SteamAPI_ISteamFriends_GetPersonaName(IntPtr self);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SteamAPI_ISteamFriends_ActivateGameOverlayInviteDialog(IntPtr self, ulong steamIDLobby);

        // ---- matchmaking ----
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern ulong SteamAPI_ISteamMatchmaking_CreateLobby(IntPtr self, int eLobbyType, int cMaxMembers);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern ulong SteamAPI_ISteamMatchmaking_JoinLobby(IntPtr self, ulong steamIDLobby);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SteamAPI_ISteamMatchmaking_LeaveLobby(IntPtr self, ulong steamIDLobby);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool SteamAPI_ISteamMatchmaking_InviteUserToLobby(IntPtr self, ulong lobby, ulong invitee);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SteamAPI_ISteamMatchmaking_GetNumLobbyMembers(IntPtr self, ulong lobby);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern ulong SteamAPI_ISteamMatchmaking_GetLobbyMemberByIndex(IntPtr self, ulong lobby, int iMember);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern ulong SteamAPI_ISteamMatchmaking_GetLobbyOwner(IntPtr self, ulong lobby);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool SteamAPI_ISteamMatchmaking_SetLobbyData(IntPtr self, ulong lobby, string key, string value);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr SteamAPI_ISteamMatchmaking_GetLobbyData(IntPtr self, ulong lobby, string key);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool SteamAPI_ISteamMatchmaking_SetLobbyJoinable(IntPtr self, ulong lobby, bool joinable);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool SteamAPI_ISteamMatchmaking_SetLobbyMemberLimit(IntPtr self, ulong lobby, int max);

        // ---- networking ----
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool SteamAPI_ISteamNetworking_SendP2PPacket(IntPtr self, ulong steamIDRemote,
            byte[] pubData, uint cubData, int eP2PSendType, int nChannel);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool SteamAPI_ISteamNetworking_IsP2PPacketAvailable(IntPtr self, ref uint pcubMsgSize, int nChannel);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool SteamAPI_ISteamNetworking_ReadP2PPacket(IntPtr self, byte[] pubDest, uint cubDest,
            ref uint pcubMsgSize, ref ulong psteamIDRemote, int nChannel);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool SteamAPI_ISteamNetworking_AcceptP2PSessionWithUser(IntPtr self, ulong steamIDRemote);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool SteamAPI_ISteamNetworking_CloseP2PSessionWithUser(IntPtr self, ulong steamIDRemote);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool SteamAPI_ISteamNetworking_AllowP2PPacketRelay(IntPtr self, bool bAllow);

        // ---- helpers ----
        public static string PtrToUtf8(IntPtr p)
        {
            if (p == IntPtr.Zero) return "";
            int len = 0;
            while (len < 4096 && Marshal.ReadByte(p, len) != 0) len++;
            if (len == 0) return "";
            byte[] buf = new byte[len];
            Marshal.Copy(p, buf, 0, len);
            return Encoding.UTF8.GetString(buf);
        }

        public static bool SendPacket(IntPtr iface, ulong remote, byte[] data, int len, bool reliable)
        {
            if (iface == IntPtr.Zero) return false;
            return SteamAPI_ISteamNetworking_SendP2PPacket(iface, remote, data, (uint)len,
                reliable ? 2 : 0, PPConfig.NetChannel);
        }
    }
}
