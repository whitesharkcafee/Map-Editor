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
        private static GameObject editorCamera;

        #region Raycasting for selecting objects
        private float maxDistance = 1000f;
        private LayerMask clickableLayers = ~0;
        #endregion
        #region Gizmo
        private GameObject editorGizmo;
        private EditorGizmo editorGizmoClass;
        #endregion

        void Awake()
        {
            Instance = this;
            ModMain.isInEditor = true;
            clickableLayers = ~(1 << LayerMask.NameToLayer("Ignore Raycast"));
            editorGizmo = new GameObject("EditorGizmo");
            editorGizmo.SetActive(false);
            editorGizmoClass = editorGizmo.AddComponent<EditorGizmo>();
            editorGizmoClass.target = null;
        }
        void Update()
        {
            if(Input.GetMouseButtonDown(0) && !MenuController.IsGamePaused)
            {
                ProcessClick();
            }
        }
        void ProcessClick()
        {
            Ray ray = editorCamera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
            if(Physics.Raycast(ray, out RaycastHit hit, maxDistance, clickableLayers))
            {
                GameObject clickedObject = hit.collider.gameObject;
                OnObjectClicked(clickedObject, hit);
            }
            else
            {
                OnClickedMiss();
            }
        }
        void OnObjectClicked(GameObject obj, RaycastHit hit)
        {
            Debug.Log($"[MapEditor] Clicked on {obj.name} at {hit.point}, normal {hit.normal}.");
            Vector3 center = GetWorldBoundsCenter(obj.GetComponent<MeshRenderer>());
            editorGizmo.SetActive(true);
            editorGizmoClass.target = obj.transform;

        }
        void OnClickedMiss()
        {
            Debug.Log($"[MapEditor] Miss.");
            editorGizmo.SetActive(false);
            editorGizmoClass.target = null;
        }
        public static void OnSceneEnter()
        {
            //When we load into the empty scene, the first thing we should do is turn off the player. The player, well, who would've guessed, shouldn't be
            //there until we start playtesting.
            editorCamera = new GameObject("EditorCamera");
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
        public static void PlayFromSpawnPoint()
        {
            //We'd assume there is a spawnpoint somewhere on the level, but since we don't have much right now, let's just assume 0 0 0 is the spawnpoint.
            Vector3 spawnPoint = new Vector3(0, 10, 0);
            Controls.Instance.gameObject.transform.position = spawnPoint;
            editorCamera.SetActive(false);
            Controls.Instance.gameObject.SetActive(true);
            InGameUIManager.Instance.gameObject.SetActive(true);
            ModMain.isInPlaytest = true;
            ModMain.isInEditor = false;
            MenuController.GetInstance().ResumeButtonPressed();
        }
        static IEnumerator DisableControls()
        {
            yield return new WaitForSeconds(.1f);
            Controls.Instance.gameObject.SetActive(false);
            yield return new WaitForSeconds(.5f);
            InGameUIManager.Instance.gameObject.SetActive(false);
        }
        private Vector3 GetWorldBoundsCenter(MeshRenderer meshRenderer)
        {
            if (meshRenderer != null)
            {
                return meshRenderer.bounds.center;
            }
            return transform.position;
        }
    }
}
