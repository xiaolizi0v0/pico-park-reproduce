using UnityEngine;

namespace PicoPark
{
    /// <summary>
    /// Bootstraps the whole game: creates the camera rig, the loop, the UI and the
    /// procedural art in one place. Assets/Scenes/Main only needs an empty object with
    /// this component on it - everything else is generated, so the project stays tiny.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        public Camera rootCamera;

        private void Awake()
        {
            PicoArt.Init();
            UIHelper.Init();
            PicoAudio.Init();

            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                cam = camGo.AddComponent<Camera>();
            }
            cam.orthographic = true;
            cam.orthographicSize = PPConfig.CamOrthoSize;
            cam.transform.position = new Vector3(PPConfig.GridW * 0.5f, PPConfig.GridH * 0.5f, -10f);
            cam.backgroundColor = PPConfig.BgBottom;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.cullingMask &= ~(1 << UIHelper.UILayer);
            cam.nearClipPlane = -100f;
            if (cam.GetComponent<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();
            if (cam.GetComponent<PicoCamera>() == null) cam.gameObject.AddComponent<PicoCamera>();
            cam.farClipPlane = 100f;
            rootCamera = cam;

            if (Object.FindObjectOfType<PicoLoop>() == null)
            {
                GameObject loopGo = new GameObject("PicoLoop");
                loopGo.AddComponent<PicoLoop>();
            }

            if (Object.FindObjectOfType<MenuUI>() == null)
            {
                GameObject uiGo = new GameObject("MenuUI");
                MenuUI ui = uiGo.AddComponent<MenuUI>();
                ui.loop = Object.FindObjectOfType<PicoLoop>();
            }
        }
    }
}
