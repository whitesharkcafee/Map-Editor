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
        private float pickThresholdPixels = 12f;
        int draggingAxis = -1; //-1 = none, 0/1/2 - x/y/z
        Vector3 dragAnchorWorld;
        float dragAnchorAngle;
        Vector3 dragStartScale;
        Quaternion dragStartRotation;
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
            if (Input.GetMouseButtonDown(0) && !MenuController.IsGamePaused)
            {
                ProcessGizmos(); // let it try to grab an axis first
                if (draggingAxis == -1)
                    ProcessClick(); // only fall through to object-select if no axis was grabbed
            }
            else
            {
                ProcessGizmos();
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
        #region Gizmo Logic
        void ProcessGizmos()
        {
            if(editorGizmo!=null && editorGizmo.activeSelf)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    TryPickAxis();
                }
                else if (Input.GetMouseButton(0) && draggingAxis != -1)
                {
                    UpdateDrag();
                }
                else if (Input.GetMouseButtonUp(0) && draggingAxis != -1)
                {
                    draggingAxis = -1;
                    editorGizmoClass.hoveredAxis = -1;
                }
                if (Input.GetKeyDown(KeyCode.R) && !MenuController.IsGamePaused)
                {
                    switch(editorGizmoClass.type)
                    {
                        case EditorGizmo.GizmoType.Move:
                            editorGizmoClass.type = EditorGizmo.GizmoType.Rotate;
                            break;
                        case EditorGizmo.GizmoType.Rotate:
                            editorGizmoClass.type = EditorGizmo.GizmoType.Scale;
                            break;
                        case EditorGizmo.GizmoType.Scale:
                            editorGizmoClass.type = EditorGizmo.GizmoType.Move;
                            break;
                    }
                }
            }
        }
        void TryPickAxis()
        {
            Vector3 origin = editorGizmoClass.target.position;
            Quaternion rot = editorGizmoClass.target.rotation;
            float worldSize = GetGizmoWorldSize(origin);

            Vector3[] axisDirs =
            {
                rot * Vector3.right,
                rot * Vector3.up,
                rot * Vector3.forward
            };
            int best = -1;
            float bestDirs = pickThresholdPixels;
            for(int i = 0; i < 3; i++)
            {
                float dist = editorGizmoClass.type == EditorGizmo.GizmoType.Rotate
                    ? ScreenDistanceToRing(origin, axisDirs[i], worldSize)
                    : ScreenDistanceToSegment(origin, origin + axisDirs[i] * worldSize);
                if(dist < bestDirs)
                {
                    bestDirs = dist;
                    best = i;
                }
            }

            if (best == -1) return; //empty/noaxis

            draggingAxis = best;
            editorGizmoClass.hoveredAxis = best;
            switch(editorGizmoClass.type)
            {
                case EditorGizmo.GizmoType.Move:
                case EditorGizmo.GizmoType.Scale:
                    dragAnchorWorld = ClosestPointOnAxisToMouse(origin, axisDirs[best]);
                    dragStartScale = editorGizmoClass.target.localScale;
                    break;
                case EditorGizmo.GizmoType.Rotate:
                    dragAnchorAngle = AngleOnRing(origin, axisDirs[best]);
                    dragStartRotation = editorGizmoClass.target.rotation;
                    break;
            }
        }
        void UpdateDrag()
        {
            Vector3 origin = editorGizmoClass.target.position;
            Quaternion rot = dragStartRotation.eulerAngles == Vector3.zero ? editorGizmoClass.target.rotation : editorGizmoClass.target.rotation;
            Vector3 axisDir = AxisDirForCurrentGizmo(draggingAxis);

            switch (editorGizmoClass.type)
            {
                case EditorGizmo.GizmoType.Move:
                    {
                        Vector3 current = ClosestPointOnAxisToMouse(origin, axisDir);
                        Vector3 delta = current - dragAnchorWorld;
                        Debug.Log($"[GizmoInteractor] axis={draggingAxis} delta={delta} beforePos={editorGizmoClass.target.position}");
                        editorGizmoClass.target.position += delta;
                        Debug.Log($"[GizmoInteractor] afterPos={editorGizmoClass.target.position}");
                        dragAnchorWorld = ClosestPointOnAxisToMouse(editorGizmoClass.target.position, axisDir);
                        break;
                    }
                case EditorGizmo.GizmoType.Scale:
                    {
                        Vector3 current = ClosestPointOnAxisToMouse(origin, axisDir);
                        float startDist = Vector3.Dot(dragAnchorWorld - origin, axisDir);
                        float curDist = Vector3.Dot(current - origin, axisDir);
                        float factor = 1f + (curDist - startDist) * 0.5f; // 0.5 = drag sensitivity
                        factor = Mathf.Max(factor, 0.01f);

                        Vector3 scale = dragStartScale;
                        switch (draggingAxis)
                        {
                            case 0: scale.x = dragStartScale.x * factor; break;
                            case 1: scale.y = dragStartScale.y * factor; break;
                            case 2: scale.z = dragStartScale.z * factor; break;
                        }
                        editorGizmoClass.target.localScale = scale;
                        break;
                    }
                case EditorGizmo.GizmoType.Rotate:
                    {
                        float currentAngle = AngleOnRing(origin, axisDir);
                        float deltaAngle = Mathf.DeltaAngle(dragAnchorAngle, currentAngle);
                        editorGizmoClass.target.rotation = Quaternion.AngleAxis(deltaAngle, axisDir) * editorGizmoClass.target.rotation;
                        dragAnchorAngle = currentAngle;
                        break;
                    }
            }
        }

        Vector3 AxisDirForCurrentGizmo(int axisIndex)
        {
            Quaternion rot = editorGizmoClass.target.rotation;
            return axisIndex == 0 ? rot * Vector3.right
                 : axisIndex == 1 ? rot * Vector3.up
                 : rot * Vector3.forward;
        }
        Vector3 ClosestPointOnAxisToMouse(Vector3 origin, Vector3 dir)
        {
            Ray ray = editorCamera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
            Vector3 u = ray.direction.normalized;
            Vector3 v = dir.normalized;
            Vector3 w0 = ray.origin - origin;

            float a = Vector3.Dot(u, u);
            float b = Vector3.Dot(u, v);
            float c = Vector3.Dot(v, v);
            float d = Vector3.Dot(u, w0);
            float e = Vector3.Dot(v, w0);
            float denom = a * c - b * b;

            if (Mathf.Abs(denom) < 1e-6f) return origin; // parallel, fallback

            float t2 = (a * e - b * d) / denom;
            return origin + v * t2;
        }

        float AngleOnRing(Vector3 origin, Vector3 normal)
        {
            Ray ray = editorCamera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
            float denom = Vector3.Dot(ray.direction, normal);
            if (Mathf.Abs(denom) < 1e-6f) return dragAnchorAngle; // ray parallel to plane, ignore

            float t = Vector3.Dot(origin - ray.origin, normal) / denom;
            Vector3 hit = ray.origin + ray.direction * t;

            Vector3 refDir = Vector3.Cross(normal, Vector3.right);
            if (refDir.sqrMagnitude < 0.001f) refDir = Vector3.Cross(normal, Vector3.up);
            refDir.Normalize();
            Vector3 up = Vector3.Cross(normal, refDir).normalized;

            Vector3 local = hit - origin;
            float x = Vector3.Dot(local, refDir);
            float y = Vector3.Dot(local, up);
            return Mathf.Atan2(y, x) * Mathf.Rad2Deg;
        }

        float ScreenDistanceToSegment(Vector3 worldA, Vector3 worldB)
        {
            Vector3 a = editorCamera.GetComponent<Camera>().WorldToScreenPoint(worldA);
            Vector3 b = editorCamera.GetComponent<Camera>().WorldToScreenPoint(worldB);
            Vector2 mouse = Input.mousePosition;

            Vector2 a2 = new Vector2(a.x, a.y);
            Vector2 b2 = new Vector2(b.x, b.y);
            Vector2 ab = b2 - a2;
            float t = ab.sqrMagnitude < 0.0001f ? 0f : Mathf.Clamp01(Vector2.Dot(mouse - a2, ab) / ab.sqrMagnitude);
            Vector2 closest = a2 + ab * t;
            return Vector2.Distance(mouse, closest);
        }
        float ScreenDistanceToRing(Vector3 origin, Vector3 normal, float radius)
        {
            Ray ray = editorCamera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
            float denom = Vector3.Dot(ray.direction, normal);
            if (Mathf.Abs(denom) < 1e-6f) return float.MaxValue;

            float t = Vector3.Dot(origin - ray.origin, normal) / denom;
            if (t < 0) return float.MaxValue;
            Vector3 hit = ray.origin + ray.direction * t;

            float worldDelta = Mathf.Abs(Vector3.Distance(origin, hit) - radius);

            // Convert world-space slop to an approximate pixel distance using
            // the screen-space size of the radius as a scale reference.
            Vector3 edgeScreen = editorCamera.GetComponent<Camera>().WorldToScreenPoint(origin + Vector3.Cross(normal, Vector3.up).normalized * radius);
            Vector3 originScreen = editorCamera.GetComponent<Camera>().WorldToScreenPoint(origin);
            float pixelsPerWorldUnit = Vector2.Distance(edgeScreen, originScreen) / Mathf.Max(radius, 0.0001f);

            return worldDelta * pixelsPerWorldUnit;
        }

        float GetGizmoWorldSize(Vector3 origin)
        {
            if (!editorGizmoClass.constantScreenSize) return editorGizmoClass.size;
            float dist = Vector3.Distance(editorCamera.GetComponent<Camera>().transform.position, origin);
            return editorGizmoClass.size * dist * 0.15f; // must match EditorGizmo's own formula
        }
        #endregion
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
