using UnityEngine;

namespace PicoPark
{
    public partial class GameRenderer
    {
        private void BuildTetris(SimWorld w)
        {
            if (w == null || w.def == null || w.def.rule != LevelRule.Tetris) return;

            TetrisView view = new TetrisView();
            // 窗口尺寸控制外框；行列数控制实际棋盘格数。棋盘按正方形格子
            // 等比缩放后居中放入窗口，两个参数可以独立调整。
            view.width = Mathf.Max(4, w.tetrisBoardColumns);
            view.height = Mathf.Max(4, w.tetrisBoardRows);
            view.windowWidth = Mathf.Max(4f, w.tetrisWindowWidth);
            view.windowHeight = Mathf.Max(4f, w.tetrisWindowHeight);
            float originX = Mathf.Max(0f, (w.def.gridW - view.windowWidth) * 0.5f);
            float originY = Mathf.Max(1f, w.def.gridH - view.windowHeight - 1f);
            view.cellSize = Mathf.Min(view.windowWidth / view.width, view.windowHeight / view.height);
            float boardWidth = view.width * view.cellSize;
            float boardHeight = view.height * view.cellSize;
            float boardOriginX = originX + (view.windowWidth - boardWidth) * 0.5f;
            float boardOriginY = originY + (view.windowHeight - boardHeight) * 0.5f;
            view.origin = new Vector2(boardOriginX, boardOriginY);

            SpriteRenderer frame = MakeSprite("TetrisWindow", dynRoot, PicoArt.S("numBoxFrame"), Color.white, 4, 1f);
            frame.drawMode = SpriteDrawMode.Sliced;
            frame.size = new Vector2(view.windowWidth, view.windowHeight);
            frame.transform.position = new Vector3(originX + view.windowWidth * 0.5f, originY + view.windowHeight * 0.5f, 0f);
            view.tr = frame.transform;
            view.frame = frame;

            Sprite outlineSprite = PicoArt.S("restrictedBox");
            if (tetrisFillSprite == null)
                tetrisFillSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            for (int y = 0; y < view.height; y++)
            {
                for (int x = 0; x < view.width; x++)
                {
                    string cellName = "TetrisCell_" + x + "_" + y;
                    // 外层使用带透明/灰边的方块精灵着色成玩家深色，内层用纯色面，
                    // 因而每个颜色都能稳定得到对应的深色描边，而不是统一灰边。
                    SpriteRenderer outline = MakeSprite(cellName + "_Outline", dynRoot, outlineSprite, Color.white, 12, 0.92f);
                    SpriteRenderer cell = MakeSprite(cellName, dynRoot, tetrisFillSprite, Color.white, 13, 0.76f);
                    outline.transform.localScale = Vector3.one * (view.cellSize * 0.92f);
                    cell.transform.localScale = Vector3.one * (view.cellSize * 0.76f);
                    Vector3 cellPosition = new Vector3(view.origin.x + (x + 0.5f) * view.cellSize,
                        view.origin.y + (y + 0.5f) * view.cellSize, 0f);
                    outline.transform.position = cellPosition;
                    cell.transform.position = cellPosition;
                    outline.enabled = false;
                    cell.enabled = false;
                    view.outlines.Add(outline);
                    view.cells.Add(cell);
                }
            }
            tetrisView = view;
        }

        private void SyncTetris(SimWorld w)
        {
            if (tetrisView == null || w == null || w.def == null || w.def.rule != LevelRule.Tetris) return;
            int index = 0;
            for (int y = 0; y < tetrisView.height; y++)
            {
                for (int x = 0; x < tetrisView.width; x++)
                {
                    if (index >= tetrisView.cells.Count) return;
                    SpriteRenderer outline = tetrisView.outlines[index];
                    SpriteRenderer cell = tetrisView.cells[index++];
                    int owner = w.GetTetrisCellOwner(x, y);
                    outline.enabled = owner >= 0;
                    cell.enabled = owner >= 0;
                    if (owner >= 0)
                    {
                        Color playerColor = PPConfig.PlayerColor(owner);
                        outline.color = Color.Lerp(playerColor, Color.black, 0.48f);
                        cell.color = playerColor;
                    }
                }
            }
        }
    }
}
