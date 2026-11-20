using UnityEngine;

namespace PicoPark
{
    /// <summary>
    /// Static level geometry on a 1-unit grid. Tile (x,y) occupies world box
    /// [x, x+1] x [y, y+1] so the level's bottom-left corner sits at the origin.
    /// </summary>
    public class TileGrid
    {
        public int W;
        public int H;
        public TileKind[] Tiles;
        /// <summary>飞机模式允许角色从关卡左右边缘飞出，镜头继续向右推进。</summary>
        public bool openHorizontalBounds;

        public TileGrid(int w, int h)
        {
            W = w; H = h;
            Tiles = new TileKind[w * h];
        }

        public TileKind Get(int x, int y)
        {
            if (x < 0 || x >= W) return openHorizontalBounds ? TileKind.Empty : TileKind.Solid;
            if (y >= H) return TileKind.Solid; // ceiling remains closed
            if (y < 0) return TileKind.Empty; // bottom is an open pit
            return Tiles[y * W + x];
        }

        public void Set(int x, int y, TileKind k)
        {
            if (x < 0 || y < 0 || x >= W || y >= H) return;
            Tiles[y * W + x] = k;
        }

        public bool IsBlocking(int x, int y)
        {
            TileKind k = Get(x, y);
            return k == TileKind.Solid || k == TileKind.Spike;
        }

        public bool IsOneWay(int x, int y) { return Get(x, y) == TileKind.OneWay; }
        public bool IsSpike(int x, int y) { return Get(x, y) == TileKind.Spike; }

        /// <summary>Does the axis-aligned box overlap any fully-solid tile?</summary>
        public bool BoxOverlapsSolid(float minX, float minY, float maxX, float maxY)
        {
            int x0 = Mathf.FloorToInt(minX + SimMath.Eps);
            int x1 = Mathf.FloorToInt(maxX - SimMath.Eps);
            int y0 = Mathf.FloorToInt(minY + SimMath.Eps);
            int y1 = Mathf.FloorToInt(maxY - SimMath.Eps);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    if (IsBlocking(x, y)) return true;
            return false;
        }

        public bool BoxOverlapsSpike(float minX, float minY, float maxX, float maxY)
        {
            int x0 = Mathf.FloorToInt(minX + 0.14f);
            int x1 = Mathf.FloorToInt(maxX - 0.14f);
            int y0 = Mathf.FloorToInt(minY + 0.14f);
            int y1 = Mathf.FloorToInt(maxY - 0.14f);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    if (IsSpike(x, y)) return true;
            return false;
        }

        /// <summary>
        /// 检查角色是否触碰到了红色危险方块（无论脚踩、头顶撞击、还是身体左右侧面贴靠接触）。
        /// </summary>
        public bool TouchesSpike(float minX, float minY, float maxX, float maxY)
        {
            if (BoxOverlapsSpike(minX, minY, maxX, maxY)) return true;

            // 1. 脚底踩踏检测 (向下微量延伸 0.06)
            int footY = Mathf.FloorToInt(minY - 0.06f);
            int fx0 = Mathf.FloorToInt(minX + 0.12f);
            int fx1 = Mathf.FloorToInt(maxX - 0.12f);
            for (int x = fx0; x <= fx1; x++)
            {
                if (IsSpike(x, footY)) return true;
            }

            // 2. 头顶撞击检测 (向上微量延伸 0.06)
            int headY = Mathf.FloorToInt(maxY + 0.06f);
            for (int x = fx0; x <= fx1; x++)
            {
                if (IsSpike(x, headY)) return true;
            }

            // 3. 身体左右侧面触碰检测 (向左右微量延伸 0.06，排除上下边缘防止误触斜对角地面)
            int by0 = Mathf.FloorToInt(minY + 0.12f);
            int by1 = Mathf.FloorToInt(maxY - 0.12f);
            int leftX = Mathf.FloorToInt(minX - 0.06f);
            int rightX = Mathf.FloorToInt(maxX + 0.06f);
            for (int y = by0; y <= by1; y++)
            {
                if (IsSpike(leftX, y) || IsSpike(rightX, y)) return true;
            }

            return false;
        }

        /// <summary>
        /// Largest allowed horizontal movement for a box before it hits solid tiles.
        /// </summary>
        public float ClampMoveX(float minX, float minY, float maxX, float maxY, float dx)
        {
            if (Mathf.Abs(dx) < SimMath.Eps) return dx;
            int y0 = Mathf.FloorToInt(minY + SimMath.Eps);
            int y1 = Mathf.FloorToInt(maxY - SimMath.Eps);
            if (dx > 0f)
            {
                int curCol = Mathf.FloorToInt(maxX - SimMath.Eps);
                for (int y = y0; y <= y1; y++)
                {
                    if (IsBlocking(curCol, y))
                        return Mathf.Min(0f, curCol - maxX);
                }
                int startCol = curCol + 1;
                int endCol = Mathf.FloorToInt(maxX + dx - SimMath.Eps);
                for (int x = startCol; x <= endCol; x++)
                {
                    for (int y = y0; y <= y1; y++)
                    {
                        if (IsBlocking(x, y))
                            return Mathf.Max(0f, x - maxX);
                    }
                }
            }
            else
            {
                int curCol = Mathf.FloorToInt(minX + SimMath.Eps);
                for (int y = y0; y <= y1; y++)
                {
                    if (IsBlocking(curCol, y))
                        return Mathf.Max(0f, (curCol + 1) - minX);
                }
                int startCol = curCol - 1;
                int endCol = Mathf.FloorToInt(minX + dx + SimMath.Eps);
                for (int x = startCol; x >= endCol; x--)
                {
                    for (int y = y0; y <= y1; y++)
                    {
                        if (IsBlocking(x, y))
                            return Mathf.Min(0f, (x + 1) - minX);
                    }
                }
            }
            return dx;
        }

        /// <summary>
        /// Largest allowed vertical movement. One-way tiles only block downward motion when
        /// the box starts at or above their top surface.
        /// </summary>
        public float ClampMoveY(float minX, float minY, float maxX, float maxY, float dy, bool useOneWay)
        {
            if (Mathf.Abs(dy) < SimMath.Eps) return dy;
            int x0 = Mathf.FloorToInt(minX + SimMath.Eps);
            int x1 = Mathf.FloorToInt(maxX - SimMath.Eps);
            if (dy > 0f)
            {
                int curRow = Mathf.FloorToInt(maxY - SimMath.Eps);
                for (int x = x0; x <= x1; x++)
                {
                    if (IsBlocking(x, curRow))
                        return Mathf.Min(0f, curRow - maxY);
                }
                int startRow = curRow + 1;
                int endRow = Mathf.FloorToInt(maxY + dy - SimMath.Eps);
                for (int y = startRow; y <= endRow; y++)
                {
                    for (int x = x0; x <= x1; x++)
                        if (IsBlocking(x, y)) return Mathf.Max(0f, y - maxY);
                }
            }
            else
            {
                int curRow = Mathf.FloorToInt(minY + SimMath.Eps);
                for (int x = x0; x <= x1; x++)
                {
                    if (IsBlocking(x, curRow))
                        return Mathf.Max(0f, (curRow + 1) - minY);
                    if (useOneWay && IsOneWay(x, curRow) && minY >= (curRow + 1) - 0.02f)
                        return Mathf.Max(0f, (curRow + 1) - minY);
                }
                int startRow = curRow - 1;
                int endRow = Mathf.FloorToInt(minY + dy + SimMath.Eps);
                for (int y = startRow; y >= endRow; y--)
                {
                    for (int x = x0; x <= x1; x++)
                    {
                        if (IsBlocking(x, y))
                            return Mathf.Min(0f, (y + 1) - minY);
                        if (useOneWay && IsOneWay(x, y) && minY >= (y + 1) - 0.02f)
                            return Mathf.Min(0f, (y + 1) - minY);
                    }
                }
            }
            return dy;
        }
    }
}
