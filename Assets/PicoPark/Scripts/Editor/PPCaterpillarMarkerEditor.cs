#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace PicoPark
{
    [CustomEditor(typeof(PPCaterpillarMarker))]
    [CanEditMultipleObjects]
    public class PPCaterpillarMarkerEditor : Editor
    {
        private SerializedProperty sizeScaleProp;
        private SerializedProperty patrolDistanceProp;
        private SerializedProperty moveSpeedProp;
        private SerializedProperty patrolDirectionProp;

        private void OnEnable()
        {
            sizeScaleProp = serializedObject.FindProperty("sizeScale");
            patrolDistanceProp = serializedObject.FindProperty("patrolDistance");
            moveSpeedProp = serializedObject.FindProperty("moveSpeed");
            patrolDirectionProp = serializedObject.FindProperty("patrolDirection");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("🐛 毛毛虫 (Caterpillar)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "毛毛虫会在巡逻范围内往返移动。角色从侧面或身体横向碰到它会死亡；从上方向下踩中时会安全弹起，毛毛虫不会消失。",
                MessageType.Info);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("巡逻设置 (Patrol)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(sizeScaleProp,
                new GUIContent("大小 (Size)", "同时缩放毛毛虫的显示尺寸和碰撞尺寸；1.0 为默认大小"));
            EditorGUILayout.PropertyField(patrolDistanceProp,
                new GUIContent("巡逻距离 (Distance)", "从毛毛虫摆放位置向右延伸的巡逻距离（格）"));
            EditorGUILayout.PropertyField(moveSpeedProp,
                new GUIContent("巡逻移动速度 (Speed)", "毛毛虫的水平巡逻速度（格/秒）"));
            EditorGUILayout.PropertyField(patrolDirectionProp,
                new GUIContent("巡逻方向 (Direction)", "从摆放位置开始移动的方向，到达端点后自动往返"));
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4f);
            CaterpillarPatrolDirection direction = (CaterpillarPatrolDirection)patrolDirectionProp.enumValueIndex;
            string endpoint = direction == CaterpillarPatrolDirection.Left ? "左侧" : "右侧";
            EditorGUILayout.HelpBox("基础碰撞体约为 1×0.5 格；“大小”会等比例缩放显示和碰撞。巡逻起点为当前物体位置，终点为" + endpoint + "“巡逻距离”处。", MessageType.None);
            if (serializedObject.ApplyModifiedProperties())
            {
                foreach (Object obj in targets)
                {
                    PPCaterpillarMarker marker = obj as PPCaterpillarMarker;
                    if (marker != null) marker.SyncVisual();
                }
            }
        }
    }
}
#endif
