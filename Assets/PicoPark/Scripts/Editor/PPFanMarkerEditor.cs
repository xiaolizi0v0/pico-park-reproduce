#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace PicoPark
{
    [CustomEditor(typeof(PPFanMarker))]
    [CanEditMultipleObjects]
    public class PPFanMarkerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();
            bool changed = EditorGUI.EndChangeCheck();

            PPFanMarker marker = (PPFanMarker)target;
            if (changed && marker != null)
            {
                marker.UpdateVisual();
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("风扇状态信息", EditorStyles.miniBoldLabel);
            if (marker != null)
            {
                EditorGUILayout.LabelField("送风方向", marker.direction.ToString());
                EditorGUILayout.LabelField("风力强度", $"{marker.windForce:0.#} (逆风阻力 / 顺风推力)");
                EditorGUILayout.LabelField("风流范围", $"距离: {marker.windDistance:0.#} 格, 宽度: {marker.windWidth:0.#} 格");
                if (marker.attachedPlatform != null)
                {
                    EditorGUILayout.LabelField("挂载平台", marker.attachedPlatform.name);
                }
                else
                {
                    EditorGUILayout.LabelField("挂载平台", "自动（放置在移动平台上方时自动跟随）");
                }
                if (marker.channel >= 0)
                {
                    EditorGUILayout.LabelField("按钮控制", $"通道 {marker.channel} ({(marker.turnOffWhenPowered ? "通电关闭" : "通电开启")})");
                }
                else
                {
                    EditorGUILayout.LabelField("按钮控制", "常开（无按钮控制）");
                }
            }
            EditorGUILayout.EndVertical();
        }
    }
}
#endif
