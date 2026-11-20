#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PicoPark
{
    public enum BrushToolMode
    {
        TextStamp = 0,   // 文字印章画笔
        FreehandBrush = 1, // 自由手绘画笔
        Eraser = 2         // 橡皮擦
    }

    /// <summary>
    /// PICO PARK 场景画笔与文字工具窗口（PPTextBrushWindow）。
    /// 允许用户在场景视图（SceneView）中通过鼠标直接印章绘制 PICO PARK 原生艺术字（如 THANK YOU），
    /// 以及使用圆角粗笔刷进行自由手绘、涂鸦与关卡搭建。
    /// </summary>
    public class PPTextBrushWindow : EditorWindow
    {
        [MenuItem("PicoPark/🎨 场景画笔与文字工具 (Text & Brush)", false, 50)]
        public static void ShowWindow()
        {
            var win = GetWindow<PPTextBrushWindow>("画笔与文字");
            win.minSize = new Vector2(340, 480);
            win.Show();
        }

        // 当前模式
        [SerializeField] private BrushToolMode mode = BrushToolMode.TextStamp;

        // 文字印章设置
        [SerializeField] private string text = "THANK YOU";
        [SerializeField] private float fontSize = 2.0f;
        [SerializeField] private Color textColor = new Color(1.0f, 0.57f, 0.31f, 1.0f); // #FF9150
        [SerializeField] private float strokeRatio = 0.145f;
        [SerializeField] private float letterSpacing = 0.25f;
        [SerializeField] private float wordSpacing = 0.65f;
        [SerializeField] private TextCollisionType collisionType = TextCollisionType.Deco;
        [SerializeField] private int sortingOrder = 3;

        // 网格对齐
        [SerializeField] private bool snapToGrid = true;
        [SerializeField] private float gridSize = 1.0f;

        // 自由手绘画笔设置
        [SerializeField] private float brushWidth = 0.28f;

        // 手绘实时状态
        private PPSceneStroke currentStroke = null;
        private Vector2 lastStrokePoint;

        private Vector2 scrollPos;

        private void OnEnable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("🎨 PICO PARK 场景画笔与文字工具", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("在 Scene 视图中移动鼠标可实时预览，点击鼠标左键即可在点击处生成如图所示的圆角胶囊文字或手绘笔画！", MessageType.Info);

            EditorGUILayout.Space(4);

            // ---- 模式切换工具栏 ----
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = (mode == BrushToolMode.TextStamp) ? new Color(1f, 0.75f, 0.3f) : Color.white;
            if (GUILayout.Button("🔤 文字印章画笔", GUILayout.Height(30))) mode = BrushToolMode.TextStamp;

            GUI.backgroundColor = (mode == BrushToolMode.FreehandBrush) ? new Color(1f, 0.75f, 0.3f) : Color.white;
            if (GUILayout.Button("🖌️ 自由手绘画笔", GUILayout.Height(30))) mode = BrushToolMode.FreehandBrush;

            GUI.backgroundColor = (mode == BrushToolMode.Eraser) ? new Color(1f, 0.4f, 0.4f) : Color.white;
            if (GUILayout.Button("🧹 橡皮擦", GUILayout.Height(30))) mode = BrushToolMode.Eraser;
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);

            // ---- 色彩选择器与预设色板 ----
            EditorGUILayout.LabelField("色彩选择 (Color):", EditorStyles.boldLabel);
            textColor = EditorGUILayout.ColorField("当前笔刷颜色", textColor);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("大地橙 (原图)")) textColor = new Color(1.0f, 0.57f, 0.31f, 1.0f);
            if (GUILayout.Button("平台绿")) textColor = new Color(0.45f, 0.71f, 0.46f, 1.0f);
            if (GUILayout.Button("钥匙金")) textColor = new Color(0.99f, 0.81f, 0.21f, 1.0f);
            if (GUILayout.Button("纯白")) textColor = Color.white;
            if (GUILayout.Button("天青蓝")) textColor = new Color(0.39f, 0.71f, 0.96f, 1.0f);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);

            // ---- 网格吸附 ----
            EditorGUILayout.BeginHorizontal();
            snapToGrid = EditorGUILayout.ToggleLeft("网格对齐 (Snap Grid)", snapToGrid, GUILayout.Width(140));
            if (snapToGrid)
            {
                EditorGUILayout.LabelField("步长:", GUILayout.Width(35));
                gridSize = EditorGUILayout.FloatField(gridSize, GUILayout.Width(50));
                gridSize = Mathf.Max(0.1f, gridSize);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);

            // ---- 各模式专属参数 ----
            if (mode == BrushToolMode.TextStamp)
            {
                DrawTextStampGUI();
            }
            else if (mode == BrushToolMode.FreehandBrush)
            {
                DrawFreehandBrushGUI();
            }
            else if (mode == BrushToolMode.Eraser)
            {
                DrawEraserGUI();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawTextStampGUI()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("文字参数设置 (Text Stamp Settings)", EditorStyles.boldLabel);

            // 常用预设快捷按钮
            EditorGUILayout.LabelField("常用文字预设一键填入:", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("THANK YOU (如图)"))
            {
                text = "THANK YOU";
                textColor = new Color(1.0f, 0.57f, 0.31f, 1.0f);
                fontSize = 2.0f;
            }
            if (GUILayout.Button("PICO PARK"))
            {
                text = "PICO PARK";
                textColor = new Color(1.0f, 0.57f, 0.31f, 1.0f);
                fontSize = 2.0f;
            }
            if (GUILayout.Button("STAGE CLEAR"))
            {
                text = "STAGE CLEAR";
                textColor = new Color(0.45f, 0.71f, 0.46f, 1.0f);
                fontSize = 2.0f;
            }
            if (GUILayout.Button("GOAL"))
            {
                text = "GOAL";
                textColor = new Color(0.99f, 0.81f, 0.21f, 1.0f);
                fontSize = 2.0f;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("输入文字 (支持换行):");
            text = EditorGUILayout.TextArea(text, GUILayout.Height(40));

            fontSize = EditorGUILayout.Slider("字号高度 (格)", fontSize, 0.5f, 6.0f);
            strokeRatio = EditorGUILayout.Slider("笔画粗细比", strokeRatio, 0.08f, 0.25f);
            letterSpacing = EditorGUILayout.Slider("字符间距比", letterSpacing, 0.05f, 0.60f);
            wordSpacing = EditorGUILayout.Slider("空格间距比", wordSpacing, 0.20f, 1.20f);

            EditorGUILayout.Space(4);
            collisionType = (TextCollisionType)EditorGUILayout.EnumPopup("物理碰撞类型", collisionType);
            sortingOrder = EditorGUILayout.IntField("渲染层级 (Sorting Order)", sortingOrder);

            EditorGUILayout.Space(8);
            if (GUILayout.Button("📍 在当前场景视口中心生成", GUILayout.Height(30)))
            {
                Vector3 center = GetSceneViewCenter();
                if (snapToGrid) center = Snap(center);
                CreateTextAt(center);
            }
            if (Selection.activeTransform != null)
            {
                if (GUILayout.Button($"📍 在选中物体 [{Selection.activeTransform.name}] 处生成", GUILayout.Height(24)))
                {
                    CreateTextAt(Selection.activeTransform.position);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawFreehandBrushGUI()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("自由手绘画笔设置 (Freehand Settings)", EditorStyles.boldLabel);
            brushWidth = EditorGUILayout.Slider("笔画粗细 (世界单位)", brushWidth, 0.10f, 1.00f);
            collisionType = (TextCollisionType)EditorGUILayout.EnumPopup("物理碰撞类型", collisionType);
            sortingOrder = EditorGUILayout.IntField("渲染层级 (Sorting Order)", sortingOrder);

            EditorGUILayout.HelpBox("使用方法：在 Scene 视图中按住【鼠标左键】自由拖动拖曳，即可实时连续描摹出圆润流畅的线条、签名、箭头、涂鸦或平台。", MessageType.Info);
            EditorGUILayout.EndVertical();
        }

        private void DrawEraserGUI()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("橡皮擦设置 (Eraser Settings)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("使用方法：在 Scene 视图中将鼠标悬浮在文字或手绘笔画上方，高亮后左键点击即可直接擦除删除。", MessageType.Warning);
            if (GUILayout.Button("🗑️ 清空场景中所有画笔文字与手绘线条", GUILayout.Height(28)))
            {
                if (EditorUtility.DisplayDialog("确认清空", "确定要删除场景内所有的 PPSceneText 和 PPSceneStroke 物体吗？", "确定删除", "取消"))
                {
                    ClearAllDrawings();
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            Event e = Event.current;
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            Vector3 worldPos = new Vector3(ray.origin.x, ray.origin.y, 0f);
            if (snapToGrid) worldPos = Snap(worldPos);

            int controlId = GUIUtility.GetControlID(FocusType.Passive);

            // 1. 文字印章模式
            if (mode == BrushToolMode.TextStamp)
            {
                HandleUtility.AddDefaultControl(controlId);
                Vector2 textSize = PicoTextFont.MeasureTextSize(text, fontSize, letterSpacing, wordSpacing);
                Vector3 center = worldPos + new Vector3(textSize.x * 0.5f, textSize.y * 0.5f, 0f);

                // 绘制虚线预览边框与文字标签
                Handles.color = new Color(textColor.r, textColor.g, textColor.b, 0.85f);
                Handles.DrawWireCube(center, new Vector3(textSize.x, textSize.y, 0.05f));
                Handles.Label(worldPos + Vector3.down * 0.25f, $"[文字印章: \"{text}\"]", EditorStyles.whiteMiniLabel);

                if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
                {
                    CreateTextAt(worldPos);
                    e.Use();
                }
            }
            // 2. 自由手绘画笔模式
            else if (mode == BrushToolMode.FreehandBrush)
            {
                HandleUtility.AddDefaultControl(controlId);
                // 绘制圆形笔刷十字预览
                Handles.color = new Color(textColor.r, textColor.g, textColor.b, 0.7f);
                Handles.DrawWireDisc(worldPos, Vector3.forward, brushWidth * 0.5f);
                Handles.DrawLine(worldPos - Vector3.right * 0.2f, worldPos + Vector3.right * 0.2f);
                Handles.DrawLine(worldPos - Vector3.up * 0.2f, worldPos + Vector3.up * 0.2f);

                if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
                {
                    BeginFreehandStroke(worldPos);
                    e.Use();
                }
                else if (e.type == EventType.MouseDrag && e.button == 0 && currentStroke != null && !e.alt)
                {
                    ContinueFreehandStroke(worldPos);
                    e.Use();
                }
                else if (e.type == EventType.MouseUp && e.button == 0)
                {
                    EndFreehandStroke();
                }
            }
            // 3. 橡皮擦模式
            else if (mode == BrushToolMode.Eraser)
            {
                HandleUtility.AddDefaultControl(controlId);
                Handles.color = Color.red;
                Handles.DrawWireDisc(worldPos, Vector3.forward, 0.4f);

                // 检测悬浮的文字或笔画
                GameObject hovered = FindDrawingAt(worldPos, 0.6f);
                if (hovered != null)
                {
                    var sr = hovered.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        Handles.color = new Color(1f, 0.2f, 0.2f, 0.9f);
                        Handles.DrawWireCube(sr.bounds.center, sr.bounds.size);
                    }
                    if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
                    {
                        Undo.DestroyObjectImmediate(hovered);
                        e.Use();
                    }
                }
            }

            // 保持鼠标移动时光标重绘流畅
            if (e.type == EventType.MouseMove)
            {
                sceneView.Repaint();
            }
        }

        private Vector3 Snap(Vector3 p)
        {
            return new Vector3(
                Mathf.Round(p.x / gridSize) * gridSize,
                Mathf.Round(p.y / gridSize) * gridSize,
                0f
            );
        }

        private Vector3 GetSceneViewCenter()
        {
            if (SceneView.lastActiveSceneView != null)
            {
                Vector3 p = SceneView.lastActiveSceneView.pivot;
                return new Vector3(p.x, p.y, 0f);
            }
            return Vector3.zero;
        }

        private void CreateTextAt(Vector3 pos)
        {
            GameObject go = new GameObject($"Text_{text.Replace('\n', '_')}");
            go.transform.position = pos;

            // 查找或收纳到 [Texts] 根节点或场景根下
            Transform parent = GetDrawingParent();
            if (parent != null) go.transform.SetParent(parent, true);

            PPSceneText st = go.AddComponent<PPSceneText>();
            st.text = text;
            st.fontSize = fontSize;
            st.color = textColor;
            st.strokeRatio = strokeRatio;
            st.letterSpacing = letterSpacing;
            st.wordSpacing = wordSpacing;
            st.collisionType = collisionType;
            st.sortingOrder = sortingOrder;
            st.RebuildText();

            Undo.RegisterCreatedObjectUndo(go, $"Create Scene Text \"{text}\"");
            Selection.activeGameObject = go;
        }

        private void BeginFreehandStroke(Vector3 pos)
        {
            GameObject go = new GameObject("Stroke");
            go.transform.position = Vector3.zero;

            Transform parent = GetDrawingParent();
            if (parent != null) go.transform.SetParent(parent, true);

            currentStroke = go.AddComponent<PPSceneStroke>();
            currentStroke.strokeWidth = brushWidth;
            currentStroke.color = textColor;
            currentStroke.collisionType = collisionType;
            currentStroke.sortingOrder = sortingOrder;
            currentStroke.points = new List<Vector2> { pos };
            lastStrokePoint = pos;
            currentStroke.RebuildStroke();

            Undo.RegisterCreatedObjectUndo(go, "Draw Stroke");
        }

        private void ContinueFreehandStroke(Vector3 pos)
        {
            if (currentStroke == null) return;
            if (Vector2.Distance(pos, lastStrokePoint) >= 0.08f)
            {
                currentStroke.points.Add(pos);
                lastStrokePoint = pos;
                currentStroke.RebuildStroke();
            }
        }

        private void EndFreehandStroke()
        {
            if (currentStroke != null)
            {
                currentStroke.RebuildStroke();
                currentStroke = null;
            }
        }

        private Transform GetDrawingParent()
        {
            // 如果场景中有激活的 LevelSceneRoot，收纳到当前关卡下
            var activeLevel = Object.FindObjectOfType<LevelSceneRoot>();
            if (activeLevel != null)
            {
                Transform textsGroup = activeLevel.transform.Find("[Texts]");
                if (textsGroup == null)
                {
                    var g = new GameObject("[Texts]");
                    g.transform.SetParent(activeLevel.transform, false);
                    textsGroup = g.transform;
                }
                return textsGroup;
            }

            // 否则在场景根目录下创建 [Texts]
            GameObject rootTexts = GameObject.Find("[Texts]");
            if (rootTexts == null)
            {
                rootTexts = new GameObject("[Texts]");
            }
            return rootTexts.transform;
        }

        private GameObject FindDrawingAt(Vector3 pos, float radius)
        {
            var texts = Object.FindObjectsOfType<PPSceneText>();
            foreach (var t in texts)
            {
                var sr = t.GetComponent<SpriteRenderer>();
                if (sr != null && sr.bounds.Contains(pos)) return t.gameObject;
            }

            var strokes = Object.FindObjectsOfType<PPSceneStroke>();
            foreach (var s in strokes)
            {
                if (s.points != null)
                {
                    for (int i = 0; i < s.points.Count; i++)
                    {
                        if (Vector2.Distance(pos, s.transform.TransformPoint(s.points[i])) <= radius)
                            return s.gameObject;
                    }
                }
            }
            return null;
        }

        private void ClearAllDrawings()
        {
            var texts = Object.FindObjectsOfType<PPSceneText>();
            for (int i = texts.Length - 1; i >= 0; i--)
            {
                Undo.DestroyObjectImmediate(texts[i].gameObject);
            }

            var strokes = Object.FindObjectsOfType<PPSceneStroke>();
            for (int i = strokes.Length - 1; i >= 0; i--)
            {
                Undo.DestroyObjectImmediate(strokes[i].gameObject);
            }
        }
    }
}
#endif
