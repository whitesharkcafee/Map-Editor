using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MapEditor.Editor
{
    public class EditorController : MonoBehaviour
    {
        public static EditorController Instance { get; private set; }
        void Awake()
        {
            Instance = this;
            ModMain.isInEditor = true;
        }
        public static void OnSceneEnter()
        {
            //When we load into the empty scene, the first thing we should do is turn off the player. The player, well, who would've guessed, shouldn't be
            //there until we start playtesting.
            GameObject editorCamera = new GameObject("EditorCamera");
            Camera camera_component = editorCamera.AddComponent<Camera>();
            camera_component.clearFlags = CameraClearFlags.Skybox;
            camera_component.cullingMask = ~((1 << LayerMask.NameToLayer("InGame2DUI")) | (1 << LayerMask.NameToLayer("2D GUI")));
            camera_component.fieldOfView = 90f;
            camera_component.nearClipPlane = .01f;
            camera_component.farClipPlane = 1000f;
            editorCamera.AddComponent<CameraController>();
            editorCamera.AddComponent<AudioListener>();
            NativeModLoader.Instance.RunModCoroutine(DisableControls());
        }
        public static IEnumerator DisableControls()
        {
            yield return new WaitForSeconds(.1f);
            Controls.Instance.gameObject.SetActive(false);
            yield return new WaitForSeconds(.5f);
            InGameUIManager.Instance.gameObject.SetActive(false);
        }
    }
}
