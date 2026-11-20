#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PicoPark
{
    [CustomEditor(typeof(PPGateMarker))]
    [CanEditMultipleObjects]
    public class PPGateMarkerEditor : Editor
    {
        private SerializedProperty channelProp;
        private SerializedProperty halfProp;
        private SerializedProperty openWhenPoweredProp;
        private SerializedProperty triggerModeProp;
        private SerializedProperty animModeProp;
        private SerializedProperty directionProp;
        private SerializedProperty visualTypeProp;

        private void OnEnable()
        {
            channelProp = serializedObject.FindProperty("channel");
            halfProp = serializedObject.FindProperty("half");
            openWhenPoweredProp = serializedObject.FindProperty("openWhenPowered");
            triggerModeProp = serializedObject.FindProperty("triggerMode");
            animModeProp = serializedObject.FindProperty("animMode");
            directionProp = serializedObject.FindProperty("direction");
            visualTypeProp = serializedObject.FindProperty("visualType");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            PPGateMarker marker = (PPGateMarker)target;

            // 1. 按钮联动分析面板
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("🚪 闸门设置 (Gate Marker)", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            var controllingButtons = FindControllingButtons(marker.gameObject);
            if (controllingButtons.Count > 1)
            {
                EditorGUILayout.HelpBox($"💡 当前受 {controllingButtons.Count} 个按钮联动控制！\n按原版 Pico Park 规则，多个按钮控制同一个物体时，需所有按钮同时踩下才激活。", MessageType.Info);
                
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField($"🔗 关联的按钮列表 ({controllingButtons.Count}):", EditorStyles.miniBoldLabel);
                foreach (var btn in controllingButtons)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($" • {btn.name}", EditorStyles.label);
                    if (GUILayout.Button("🔍 定位", GUILayout.Width(60), GUILayout.Height(18)))
                    {
                        Selection.activeGameObject = btn.gameObject;
                        EditorGUIUtility.PingObject(btn.gameObject);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(2);
                EditorGUILayout.PropertyField(triggerModeProp, new GUIContent("多按钮触发模式", "AllPressed: 必须全部按钮同时按下才开门（默认合作解密机制）；AnyPressed: 任一按下即开门"));
            }
            else if (controllingButtons.Count == 1)
            {
                EditorGUILayout.HelpBox($"💡 受单个按钮控制:「{controllingButtons[0].name}」", MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox("💡 当前未关联按钮。可将此物体拖入任意按钮的「受控目标数组」完成自动绑定，或使用下方手动通道。", MessageType.None);
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4);

            // 2. 闸门基础属性
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("⚙️ 闸门物理与动画属性", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(animModeProp, new GUIContent("动画模式", "Grow: 生长（默认收起，通电向外生长展开）；Shrink: 收缩（默认展开，通电向内收缩缩回）；Scale: 居中缩放；CenterExpand: 居中展开"));

            var currentAnimMode = (GateAnimMode)animModeProp.enumValueIndex;
            if (currentAnimMode == GateAnimMode.Grow)
            {
                openWhenPoweredProp.boolValue = false;
                EditorGUILayout.HelpBox("🌱【生长模式 (Grow)】：默认收起隐藏（无碰撞），踩下按钮后向选定方向「生长伸展」出实体平台/墙体。", MessageType.Info);
                EditorGUILayout.PropertyField(directionProp, new GUIContent("生长方向", "激活时的定向伸展方向：从右往左、从左往右、从下往上、从上往下"));
            }
            else if (currentAnimMode == GateAnimMode.Shrink)
            {
                openWhenPoweredProp.boolValue = true;
                EditorGUILayout.HelpBox("🔻【收缩模式 (Shrink)】：默认完全展开（有碰撞），踩下按钮后向固定端「收缩缩回」让出通道或缩起浮桥。", MessageType.Info);

                string dirDesc = "收缩缩入端：";
                switch ((GateDirection)directionProp.enumValueIndex)
                {
                    case GateDirection.BottomToTop: dirDesc = "底端固定，从上往下收回底部 (Bottom)"; break;
                    case GateDirection.TopToBottom: dirDesc = "顶端固定，从下往上收回顶部 (Top)"; break;
                    case GateDirection.LeftToRight: dirDesc = "左端固定，从右往左收回左侧 (Left)"; break;
                    case GateDirection.RightToLeft: dirDesc = "右端固定，从左往右收回右侧 (Right)"; break;
                }
                EditorGUILayout.PropertyField(directionProp, new GUIContent("收缩缩入端", "踩下按钮时向哪一端收缩退回"));
                EditorGUILayout.LabelField($"💡 {dirDesc}", EditorStyles.miniLabel);
            }
            else if (currentAnimMode == GateAnimMode.CenterExpand)
            {
                EditorGUILayout.HelpBox("↔️【居中展开模式 (CenterExpand)】：以中心点为基准，沿选定主轴向两侧对称展开。", MessageType.Info);
                EditorGUILayout.PropertyField(directionProp, new GUIContent("展开主轴", "居中展开的主轴方向（水平轴或垂直轴）"));
                EditorGUILayout.PropertyField(openWhenPoweredProp, new GUIContent("通电时展开出现", "勾选: 通电时展开出现；取消勾选: 通电时收拢消失"));
            }
            else // Scale
            {
                EditorGUILayout.HelpBox("🔍【居中缩放模式 (Scale)】：以物体中心为基准，整体等比居中放大缩小。", MessageType.Info);
                EditorGUILayout.PropertyField(openWhenPoweredProp, new GUIContent("通电时放大出现", "勾选: 通电时放大出现；取消勾选: 通电时缩小消失"));
            }

            EditorGUILayout.PropertyField(halfProp, new GUIContent("闸门半尺寸 (Half Size)", "X 为半宽，Y 为半高（实际物理全长为 2X × 2Y）"));
            EditorGUILayout.PropertyField(visualTypeProp, new GUIContent("渲染外观样式", "Auto: 自动根据精灵与模式推导；TileBlock: 地形实体色方块；IronGate: 铁栅栏闸门"));
            
            if (controllingButtons.Count == 0)
            {
                EditorGUILayout.PropertyField(channelProp, new GUIContent("手动通道编号", "未绑定按钮时的手动回退通道编号"));
            }

            // 尺寸同步与所见即所得检测
            var sr = marker.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                float wpx = sr.sprite.rect.width / sr.sprite.pixelsPerUnit;
                float hpx = sr.sprite.rect.height / sr.sprite.pixelsPerUnit;
                float visualW = Mathf.Abs(marker.transform.localScale.x) * wpx;
                float visualH = Mathf.Abs(marker.transform.localScale.y) * hpx;
                float halfW = marker.half.x * 2f;
                float halfH = marker.half.y * 2f;

                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField($"📐 碰撞尺寸: {halfW:0.##} × {halfH:0.##} 格 | 精灵尺寸: {visualW:0.##} × {visualH:0.##} 格", EditorStyles.miniLabel);

                if (Mathf.Abs(visualW - halfW) > 0.05f || Mathf.Abs(visualH - halfH) > 0.05f)
                {
                    EditorGUILayout.HelpBox($"⚠️ 预览线框尺寸 ({halfW:0.##} × {halfH:0.##}) 与场景中精灵显示 ({visualW:0.##} × {visualH:0.##}) 不一致！\n点击下方按钮一键对齐：", MessageType.Warning);

                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("🔄 以精灵为准 (同步半尺寸)", GUILayout.Height(24)))
                    {
                        Undo.RecordObject(marker, "Sync Half to Visual");
                        marker.SyncHalfToVisual();
                        EditorUtility.SetDirty(marker);
                    }
                    if (GUILayout.Button("🔄 以半尺寸为准 (缩放精灵)", GUILayout.Height(24)))
                    {
                        Undo.RecordObject(marker.transform, "Sync Visual to Half");
                        marker.SyncVisualToHalf();
                        EditorUtility.SetDirty(marker.gameObject);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndVertical();

            if (serializedObject.ApplyModifiedProperties())
            {
                marker.SyncVisualToHalf();
                EditorUtility.SetDirty(marker.gameObject);
            }
        }

        private List<PPButtonMarker> FindControllingButtons(GameObject targetGo)
        {
            var list = new List<PPButtonMarker>();
            if (targetGo == null) return list;

            var buttons = Object.FindObjectsOfType<PPButtonMarker>(true);
            foreach (var b in buttons)
            {
                if (b == null) continue;
                var targets = b.GetAllTargets();
                if (targets.Contains(targetGo))
                {
                    list.Add(b);
                }
            }
            return list;
        }
    }
}
#endif
