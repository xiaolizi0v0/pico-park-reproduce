using UnityEngine;

namespace PicoPark
{
    /// <summary>
    /// 挂载于场景中的 [Levels] 根物体，作为关卡总管理器。
    /// 可以通过它方便地切换当前编辑/游玩的活跃关卡！
    /// </summary>
    [SelectionBase]
    public class LevelsManager : MonoBehaviour
    {
        private static LevelsManager _instance;
        public static LevelsManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Object.FindAnyObjectByType<LevelsManager>(FindObjectsInactive.Include);
                }
                return _instance;
            }
            set => _instance = value;
        }

        public LevelSceneRoot[] allLevels;
        public int currentLevelIndex = 0;

        private void Awake()
        {
            _instance = this;
            RefreshLevelsList();
        }

        public void RefreshLevelsList()
        {
            allLevels = GetComponentsInChildren<LevelSceneRoot>(true);
        }

        public LevelSceneRoot GetActiveLevel()
        {
            RefreshLevelsList();
            if (allLevels == null || allLevels.Length == 0) return null;
            for (int i = 0; i < allLevels.Length; i++)
                if (allLevels[i] != null && allLevels[i].gameObject.activeInHierarchy)
                    return allLevels[i];
            return allLevels[0];
        }

        public LevelSceneRoot FindLevelRoot(int levelIndex, int playerCount = 2)
        {
            RefreshLevelsList();
            if (allLevels == null || allLevels.Length == 0) return null;
            if (levelIndex == PicoLoop.LobbyIndex)
            {
                foreach (var l in allLevels)
                {
                    if (l != null && (l.worldIndex == 0 || l.gameObject.name.ToLower().Contains("lobby")))
                        return l;
                }
                return null;
            }

            int targetWorld = (levelIndex / 4) + 1;
            int targetStage = (levelIndex % 4) + 1;
            // 缺少同人数的变体时，沿用旧版 2P / 4P / 8P 分档。
            int tier = (playerCount >= 8) ? 8 : (playerCount >= 4 ? 4 : 2);

            // 1. 匹配同一关卡的实际人数。
            foreach (var l in allLevels)
            {
                if (l != null && l.worldIndex == targetWorld && l.stageIndex == targetStage && l.targetPlayers == playerCount)
                    return l;
            }

            // 2. 匹配旧版分档。
            if (tier != playerCount)
            {
                foreach (var l in allLevels)
                {
                    if (l != null && l.worldIndex == targetWorld && l.stageIndex == targetStage && l.targetPlayers == tier)
                        return l;
                }
            }

            // 3. 回退匹配该关卡的任意变体
            foreach (var l in allLevels)
            {
                if (l != null && l.worldIndex == targetWorld && l.stageIndex == targetStage)
                    return l;
            }

            return null;
        }

        public LevelSceneRoot ActivateLevel(int index, int playerCount = 2)
        {
            // 若 [Levels] 管理器根节点在编辑期被隐藏，激活它以保证子关卡能够正常激活显示
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            LevelSceneRoot target = FindLevelRoot(index, playerCount);
            if (allLevels != null)
            {
                for (int i = 0; i < allLevels.Length; i++)
                {
                    if (allLevels[i] != null)
                    {
                        bool isTarget = (allLevels[i] == target);
                        allLevels[i].gameObject.SetActive(isTarget);

                        if (isTarget)
                        {
                            // 沿父节点链向上逐级激活，确保所有隐藏的中间父物体（如 Level_7_1）被完整唤醒
                            Transform curr = allLevels[i].transform.parent;
                            while (curr != null)
                            {
                                curr.gameObject.SetActive(true);
                                if (curr == transform) break;
                                curr = curr.parent;
                            }
                        }
                    }
                }
            }
            return target;
        }
    }
}
