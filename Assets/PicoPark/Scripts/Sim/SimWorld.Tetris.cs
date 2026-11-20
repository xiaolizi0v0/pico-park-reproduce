using UnityEngine;

namespace PicoPark
{
    /// <summary>一个正在俄罗斯方块窗口中下落的四格方块。</summary>
    public class SimTetrisPiece
    {
        public int id;
        public int ownerPlayerIndex;
        public int shapeIndex;
        public int rotation;
        public int gridX;
        public int gridY;
        public float fallTimer;
        public float moveRepeatTimer;
        public bool active = true;
    }

    public partial class SimWorld
    {
        private const float TetrisDefaultFallSpeed = 2f;
        private const float TetrisSoftDropInterval = 0.07f;
        private const float TetrisHorizontalRepeat = 0.11f;
        private const int TetrisMaxWindowSize = 32;

        // 坐标原点在方块左下角；每个数组项是相对锚点的格坐标。
        private static readonly Vector2Int[][] TetrisShapes =
        {
            new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) }, // I
            new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) },   // O
            new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1) }, // T
            new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1) }, // L
            new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(-1, 1) }, // J
            new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(-1, 1), new Vector2Int(0, 1) },  // S
            new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) },  // Z
        };

        public bool IsTetrisRuleActive
        {
            get { return def != null && def.rule == LevelRule.Tetris; }
        }

        /// <summary>初始化俄罗斯方块窗口、棋盘和出砖随机序列。</summary>
        public void ResetTetrisRule()
        {
            tetrisPieces.Clear();
            tetrisRowsCleared = 0;
            tetrisKeyRevealed = !IsTetrisRuleActive;
            tetrisWindowWidth = 0;
            tetrisWindowHeight = 0;
            tetrisBoardColumns = 0;
            tetrisBoardRows = 0;
            tetrisBoard = new int[0];
            for (int i = 0; i < tetrisSpawnTimers.Length; i++)
                tetrisSpawnTimers[i] = 0f;
            for (int i = 0; i < tetrisPreviousButtons.Length; i++)
                tetrisPreviousButtons[i] = Btn.None;

            if (!IsTetrisRuleActive) return;

            tetrisWindowWidth = Mathf.Clamp(def.tetrisWindowWidth, 4, TetrisMaxWindowSize);
            tetrisWindowHeight = Mathf.Clamp(def.tetrisWindowHeight, 4, TetrisMaxWindowSize);
            tetrisBoardColumns = Mathf.Clamp(def.tetrisBoardColumns, 4, TetrisMaxWindowSize);
            tetrisBoardRows = Mathf.Clamp(def.tetrisBoardRows, 4, TetrisMaxWindowSize);
            tetrisBoard = new int[tetrisBoardColumns * tetrisBoardRows];
            for (int i = 0; i < tetrisBoard.Length; i++) tetrisBoard[i] = -1;

            unchecked
            {
                tetrisRandomState = (uint)(levelIndex + 1) * 747796405u + 2891336453u;
                tetrisRandomState ^= (uint)(playerCount + 1) * 277803737u;
                if (tetrisRandomState == 0u) tetrisRandomState = 1u;
            }
        }

        /// <summary>供 GameRenderer 读取窗口内某格的玩家颜色编号；-1 表示空格。</summary>
        public int GetTetrisCellOwner(int x, int y)
        {
            if (!IsTetrisRuleActive || x < 0 || y < 0 || x >= tetrisBoardColumns || y >= tetrisBoardRows)
                return -1;

            int owner = tetrisBoard[y * tetrisBoardColumns + x];
            if (owner >= 0) return owner;

            for (int i = 0; i < tetrisPieces.Count; i++)
            {
                SimTetrisPiece piece = tetrisPieces[i];
                if (piece == null || !piece.active) continue;
                Vector2Int[] cells = TetrisShapes[Mathf.Clamp(piece.shapeIndex, 0, TetrisShapes.Length - 1)];
                for (int c = 0; c < cells.Length; c++)
                {
                    Vector2Int cell = RotateCell(cells[c], piece.rotation);
                    if (piece.gridX + cell.x == x && piece.gridY + cell.y == y)
                        return Mathf.Clamp(piece.ownerPlayerIndex, 0, PPConfig.MaxPlayers - 1);
                }
            }
            return -1;
        }

        public void TickTetris()
        {
            if (!IsTetrisRuleActive || phase != Phase.Playing) return;
            if (tetrisRowsCleared >= Mathf.Max(1, def.tetrisRowsToClear))
            {
                if (!tetrisKeyRevealed) RevealTetrisKeys();
                return;
            }

            for (int i = 0; i < tetrisPieces.Count; i++)
            {
                SimTetrisPiece piece = tetrisPieces[i];
                if (piece == null || !piece.active) continue;

                InputCmd cmd = GetInput(Mathf.Clamp(piece.ownerPlayerIndex, 0, PPConfig.MaxPlayers - 1));
                Btn previous = tetrisPreviousButtons[Mathf.Clamp(piece.ownerPlayerIndex, 0, PPConfig.MaxPlayers - 1)];
                bool jumpPressed = cmd.Has(Btn.Jump) && (previous & Btn.Jump) == 0;
                bool actionPressed = cmd.Has(Btn.Action) && (previous & Btn.Action) == 0;

                int moveX = cmd.MoveX;
                if (moveX != 0)
                {
                    piece.moveRepeatTimer -= PPConfig.Dt;
                    bool directionPressed = (moveX < 0 && (previous & Btn.Left) == 0) ||
                                             (moveX > 0 && (previous & Btn.Right) == 0);
                    if (directionPressed || piece.moveRepeatTimer <= 0f)
                    {
                        TryMoveTetrisPiece(piece, moveX, 0);
                        piece.moveRepeatTimer = TetrisHorizontalRepeat;
                    }
                }
                else
                {
                    piece.moveRepeatTimer = 0f;
                }

                if (jumpPressed)
                {
                    int nextRotation = (piece.rotation + 1) & 3;
                    if (CanPlaceTetrisPiece(piece, piece.gridX, piece.gridY, nextRotation))
                        piece.rotation = nextRotation;
                }

                if (actionPressed)
                {
                    while (CanPlaceTetrisPiece(piece, piece.gridX, piece.gridY - 1, piece.rotation))
                        piece.gridY--;
                    // Another still-falling piece is only a temporary obstacle.
                    // Do not convert that airborne contact into a locked board
                    // cell; wait until the obstacle itself settles first.
                    if (IsBlockedByActiveTetrisPiece(piece, piece.gridX, piece.gridY - 1, piece.rotation))
                    {
                        piece.fallTimer = 0f;
                        continue;
                    }
                    LockTetrisPiece(piece);
                    continue;
                }

                float fallSpeed = def != null && def.tetrisFallSpeed > 0.05f
                    ? def.tetrisFallSpeed
                    : TetrisDefaultFallSpeed;
                float interval = cmd.Has(Btn.Down) ? TetrisSoftDropInterval : 1f / fallSpeed;
                piece.fallTimer += PPConfig.Dt;
                if (piece.fallTimer >= interval)
                {
                    piece.fallTimer = 0f;
                    int nextY = piece.gridY - 1;
                    if (CanPlaceTetrisPiece(piece, piece.gridX, nextY, piece.rotation))
                        piece.gridY--;
                    else if (IsBlockedByActiveTetrisPiece(piece, piece.gridX, nextY, piece.rotation))
                    {
                        // Contact with another active falling piece must not
                        // lock this piece in mid-air. It will try again after
                        // the lower piece moves or becomes part of the board.
                    }
                    else
                        LockTetrisPiece(piece);
                }
            }

            for (int i = tetrisPieces.Count - 1; i >= 0; i--)
                if (tetrisPieces[i] == null || !tetrisPieces[i].active) tetrisPieces.RemoveAt(i);

            for (int i = 0; i < tetrisPreviousButtons.Length; i++)
                tetrisPreviousButtons[i] = GetInput(i).buttons;

            if (tetrisRowsCleared >= Mathf.Max(1, def.tetrisRowsToClear))
            {
                if (!tetrisKeyRevealed) RevealTetrisKeys();
                return;
            }

            // 独立掉落：每位玩家只为自己生成下一块；已有活动方块的玩家不会
            // 再拿到第二块，其他玩家的计时器不受其影响。
            int maxPlayers = Mathf.Clamp(playerCount, 1, PPConfig.MaxPlayers);
            float spawnInterval = Mathf.Max(0.05f, def.tetrisSpawnInterval);
            for (int owner = 0; owner < maxPlayers; owner++)
            {
                if (HasActiveTetrisPieceForOwner(owner)) continue;
                tetrisSpawnTimers[owner] -= PPConfig.Dt;
                if (tetrisSpawnTimers[owner] > 0f) continue;
                if (SpawnTetrisPiece(owner))
                    tetrisSpawnTimers[owner] = spawnInterval;
                if (phase != Phase.Playing) return;
            }
        }

        private bool SpawnTetrisPiece(int owner)
        {
            SimTetrisPiece piece = new SimTetrisPiece();
            piece.id = tetrisPieces.Count;
            piece.ownerPlayerIndex = owner;
            piece.shapeIndex = NextTetrisInt(TetrisShapes.Length);
            piece.rotation = 0;
            piece.gridY = tetrisBoardRows - 2;
            int startX = NextTetrisInt(tetrisBoardColumns);
            bool foundSpawn = false;
            for (int offset = 0; offset < tetrisBoardColumns; offset++)
            {
                piece.gridX = (startX + offset) % tetrisBoardColumns;
                if (CanPlaceTetrisPiece(piece, piece.gridX, piece.gridY, piece.rotation))
                {
                    foundSpawn = true;
                    break;
                }
            }
            if (!foundSpawn)
            {
                phase = Phase.Failed;
                failTimer = 0f;
                Debug.Log("[Tetris] 俄罗斯方块窗口已堆满，关卡失败。");
                return false;
            }
            tetrisPieces.Add(piece);
            return true;
        }

        private bool HasActiveTetrisPieceForOwner(int ownerPlayerIndex)
        {
            for (int i = 0; i < tetrisPieces.Count; i++)
            {
                SimTetrisPiece piece = tetrisPieces[i];
                if (piece != null && piece.active && piece.ownerPlayerIndex == ownerPlayerIndex)
                    return true;
            }
            return false;
        }

        private bool TryMoveTetrisPiece(SimTetrisPiece piece, int dx, int dy)
        {
            if (!CanPlaceTetrisPiece(piece, piece.gridX + dx, piece.gridY + dy, piece.rotation)) return false;
            piece.gridX += dx;
            piece.gridY += dy;
            return true;
        }

        private bool CanPlaceTetrisPiece(SimTetrisPiece piece, int x, int y, int rotation)
        {
            if (piece == null || tetrisBoard == null || tetrisBoardColumns <= 0 || tetrisBoardRows <= 0) return false;
            Vector2Int[] cells = TetrisShapes[Mathf.Clamp(piece.shapeIndex, 0, TetrisShapes.Length - 1)];
            for (int c = 0; c < cells.Length; c++)
            {
                Vector2Int cell = RotateCell(cells[c], rotation);
                int gx = x + cell.x;
                int gy = y + cell.y;
                if (gx < 0 || gx >= tetrisBoardColumns || gy < 0 || gy >= tetrisBoardRows) return false;
                if (tetrisBoard[gy * tetrisBoardColumns + gx] >= 0) return false;
            }
            return !IsBlockedByActiveTetrisPiece(piece, x, y, rotation);
        }

        private bool IsBlockedByActiveTetrisPiece(SimTetrisPiece piece, int x, int y, int rotation)
        {
            if (piece == null) return false;
            Vector2Int[] cells = TetrisShapes[Mathf.Clamp(piece.shapeIndex, 0, TetrisShapes.Length - 1)];
            for (int c = 0; c < cells.Length; c++)
            {
                Vector2Int cell = RotateCell(cells[c], rotation);
                int gx = x + cell.x;
                int gy = y + cell.y;
                for (int i = 0; i < tetrisPieces.Count; i++)
                {
                    SimTetrisPiece other = tetrisPieces[i];
                    if (other == null || other == piece || !other.active) continue;
                    Vector2Int[] otherCells = TetrisShapes[Mathf.Clamp(other.shapeIndex, 0, TetrisShapes.Length - 1)];
                    for (int oc = 0; oc < otherCells.Length; oc++)
                    {
                        Vector2Int otherCell = RotateCell(otherCells[oc], other.rotation);
                        if (other.gridX + otherCell.x == gx && other.gridY + otherCell.y == gy)
                            return true;
                    }
                }
            }
            return false;
        }

        private void LockTetrisPiece(SimTetrisPiece piece)
        {
            if (piece == null || !piece.active) return;
            Vector2Int[] cells = TetrisShapes[Mathf.Clamp(piece.shapeIndex, 0, TetrisShapes.Length - 1)];
            for (int c = 0; c < cells.Length; c++)
            {
                Vector2Int cell = RotateCell(cells[c], piece.rotation);
                int gx = piece.gridX + cell.x;
                int gy = piece.gridY + cell.y;
                if (gx >= 0 && gx < tetrisBoardColumns && gy >= 0 && gy < tetrisBoardRows)
                    tetrisBoard[gy * tetrisBoardColumns + gx] = Mathf.Clamp(piece.ownerPlayerIndex, 0, PPConfig.MaxPlayers - 1);
            }
            piece.active = false;
            int cleared = ClearTetrisRows();
            if (cleared > 0)
            {
                tetrisRowsCleared += cleared;
                Debug.Log("[Tetris] 消除 " + cleared + " 行，累计 " + tetrisRowsCleared + " 行。");
            }
        }

        private int ClearTetrisRows()
        {
            if (tetrisBoard == null || tetrisBoardColumns <= 0 || tetrisBoardRows <= 0) return 0;
            bool[] clearedRows = new bool[tetrisBoardRows];
            for (int y = 0; y < tetrisBoardRows; y++)
            {
                bool full = true;
                for (int x = 0; x < tetrisBoardColumns; x++)
                {
                    if (tetrisBoard[y * tetrisBoardColumns + x] < 0) { full = false; break; }
                }
                clearedRows[y] = full;
            }

            int writeY = 0;
            int cleared = 0;
            for (int y = 0; y < tetrisBoardRows; y++)
            {
                if (clearedRows[y]) { cleared++; continue; }
                if (writeY != y)
                {
                    for (int x = 0; x < tetrisBoardColumns; x++)
                        tetrisBoard[writeY * tetrisBoardColumns + x] = tetrisBoard[y * tetrisBoardColumns + x];
                }
                writeY++;
            }
            for (int y = writeY; y < tetrisBoardRows; y++)
                for (int x = 0; x < tetrisBoardColumns; x++)
                    tetrisBoard[y * tetrisBoardColumns + x] = -1;

            // 同时下落的方块也要跟随其下方被消除的行下移，避免清行后悬空或与棋盘重叠。
            for (int i = 0; i < tetrisPieces.Count; i++)
            {
                SimTetrisPiece piece = tetrisPieces[i];
                if (piece == null || !piece.active) continue;
                Vector2Int[] cells = TetrisShapes[Mathf.Clamp(piece.shapeIndex, 0, TetrisShapes.Length - 1)];
                int minY = tetrisBoardRows;
                for (int c = 0; c < cells.Length; c++)
                    minY = Mathf.Min(minY, piece.gridY + RotateCell(cells[c], piece.rotation).y);
                int drop = 0;
                for (int y = 0; y < minY && y < clearedRows.Length; y++)
                    if (clearedRows[y]) drop++;
                piece.gridY = Mathf.Max(0, piece.gridY - drop);
            }
            return cleared;
        }

        private void RevealTetrisKeys()
        {
            tetrisKeyRevealed = true;
            for (int i = 0; i < keyActorIds.Count; i++)
            {
                SimActor key = GetActor(keyActorIds[i]);
                if (key == null || key.isKeyJarKey) continue;
                key.active = true;
                key.delivered = false;
                key.carriedBy = -1;
                key.pos = key.spawnPos;
            }
            Debug.Log("[Tetris] 达到目标消除行数，钥匙出现。");
        }

        private static Vector2Int RotateCell(Vector2Int cell, int rotation)
        {
            int turns = ((rotation % 4) + 4) % 4;
            for (int i = 0; i < turns; i++)
                cell = new Vector2Int(-cell.y, cell.x);
            return cell;
        }

        private uint NextTetrisRandom()
        {
            unchecked
            {
                uint x = tetrisRandomState;
                x ^= x >> 16;
                x *= 2246822519u;
                x ^= x >> 13;
                x *= 3266489917u;
                x ^= x >> 16;
                tetrisRandomState = x == 0u ? 1u : x;
                return tetrisRandomState;
            }
        }

        private int NextTetrisInt(int maxExclusive)
        {
            if (maxExclusive <= 1) return 0;
            return (int)(NextTetrisRandom() % (uint)maxExclusive);
        }
    }
}
