using UnityEngine;

namespace PicoPark
{
    public static class LevelDatabase
    {
        private static LevelDef[] levels;

        public static LevelDef[] Levels
        {
            get
            {
                if (levels == null) Build();
                return levels;
            }
        }

        public static int Count { get { return Levels.Length; } }

        private static LevelDef lobby;
        public static LevelDef Lobby
        {
            get
            {
                if (lobby == null) BuildLobby();
                return lobby;
            }
        }

        private static void BuildLobby()
        {
            lobby = ScriptableObject.CreateInstance<LevelDef>();
            lobby.worldIndex = 0;
            lobby.stageIndex = 0;
            lobby.levelName = "等待大厅";
            lobby.theme = "Lobby";
            lobby.hint = "等待玩家加入中… 可以自由活动练习跳跃，全员准备好后由房主开始游戏！";
            lobby.rule = LevelRule.None;
            lobby.gridW = PPConfig.GridW;
            lobby.gridH = PPConfig.GridH;
            lobby.hasDoor = true;
            lobby.rows = new string[]
            {
                "#1#2#3#4#5#6#7#8########D###",
                "#..........................#",
                "#..........................#",
                "#...====........====.......#",
                "#..........................#",
                "#..........................#",
                "#........==========........#",
                "#..........................#",
                "#..........................#",
                "#..........................#",
                "#..........................#",
                "#..........................#",
                "#..........................#",
                "#..........................#",
                "#..........................#",
                "############################"
            };
        }

        /// <summary>关卡解锁占位：当前全部开放，后续在此接入解锁进度。</summary>
        public static bool IsUnlocked(int index)
        {
            return true;
        }

        public static LevelDef Get(int i)
        {
            LevelDef[] all = Levels;
            if (all == null || all.Length == 0) return null;
            if (i < 0) i = 0;
            if (i >= all.Length) i = all.Length - 1;
            return all[i];
        }

        public static LevelDef GetByWorldStage(int world, int stage)
        {
            int idx = (world - 1) * 4 + (stage - 1);
            return Get(idx);
        }

        private static void Build()
        {
            LevelDef[] w1_3 = LevelContent_W1_W3.Levels();
            LevelDef[] w4_6 = LevelContent_W4_W6.Levels();
            LevelDef[] w7_9 = LevelContent_W7_W9.Levels();
            LevelDef[] w10_12 = LevelContent_W10_W12.Levels();

            int total = w1_3.Length + w4_6.Length + w7_9.Length + w10_12.Length;
            levels = new LevelDef[total];

            int offset = 0;
            for (int i = 0; i < w1_3.Length; i++) levels[offset++] = w1_3[i];
            for (int i = 0; i < w4_6.Length; i++) levels[offset++] = w4_6[i];
            for (int i = 0; i < w7_9.Length; i++) levels[offset++] = w7_9[i];
            for (int i = 0; i < w10_12.Length; i++) levels[offset++] = w10_12[i];
        }
    }
}
