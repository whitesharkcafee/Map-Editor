using UnityEngine;

/// <summary>
/// Base runtime gizmo drawer for a mod-side level editor.
///
/// Unity's built-in Gizmos/Handles classes only draw in the Editor's Scene
/// view, so they never appear in a built game. Since this is a mod running
/// without access to the actual Unity Editor project, this script instead
/// draws the gizmo every frame with GL immediate-mode calls in
/// OnRenderObject, which works in builds too.
///
/// Attach this to an empty GameObject positioned at the pivot you want to
/// manipulate (or set <see cref="target"/> to point elsewhere). Switch
/// <see cref="type"/> to change which gizmo is drawn. Picking / dragging
/// logic is intentionally left as stub hooks — wire them up to your mod's
/// input system.
/// </summary>
namespace MapEditor.Editor
{
    public class EditorGizmo : MonoBehaviour
    {
        public enum GizmoType
        {
            Move,
            Rotate,
            Scale
        }

        [Header("Target")]
        [Tooltip("Transform the gizmo manipulates. Defaults to this object's own transform.")]
        public Transform target;

        [Header("Mode")]
        public GizmoType type = GizmoType.Move;

        [Header("Appearance")]
        [Tooltip("Overall gizmo size in world units.")]
        public float size = 1.5f;
        [Tooltip("Keep the gizmo a constant screen size regardless of camera distance.")]
        public bool constantScreenSize = true;
        [Range(0.5f, 3f)]
        public float lineThicknessHint = 1f; // GL lines are 1px; kept for future use with meshes

        public Color axisColorX = new Color(0.85f, 0.15f, 0.15f);
        public Color axisColorY = new Color(0.15f, 0.75f, 0.15f);
        public Color axisColorZ = new Color(0.15f, 0.4f, 0.9f);
        public Color highlightColor = Color.yellow;

        [Header("Interaction (stub)")]
        [Tooltip("Which axis is currently highlighted, e.g. from a raycast/picking pass. -1 = none, 0=X 1=Y 2=Z.")]
        public int hoveredAxis = -1;

        // Shape tuning
        const float ArrowHeadLength = 0.18f;
        const float ArrowHeadWidth = 0.06f;
        const int RingSegments = 48;
        const float ScaleHandleSize = 0.08f;
        const float PlaneHandleOffset = 0.28f;
        const float PlaneHandleSize = 0.14f;

        static Material _glMaterial;

        void OnEnable()
        {
            if (target == null) target = transform;
            EnsureMaterial();
        }

        static void EnsureMaterial()
        {
            if (_glMaterial != null) return;

            // "Hidden/Internal-Colored" is built into Unity and supports
            // vertex colors + alpha blending without needing an external asset.
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            _glMaterial = new Material(shader);
            _glMaterial.hideFlags = HideFlags.HideAndDontSave;
            _glMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _glMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _glMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            _glMaterial.SetInt("_ZWrite", 0);
            // Draw on top of scene geometry, like editor gizmos do.
            _glMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
        }

        void OnRenderObject()
        {
            if (target == null || !isActiveAndEnabled) return;

            Camera cam = Camera.current;
            if (cam == null) return;

            EnsureMaterial();
            _glMaterial.SetPass(0);

            float worldSize = size;
            if (constantScreenSize)
            {
                float dist = Vector3.Distance(cam.transform.position, target.position);
                worldSize = size * dist * 0.15f;
            }

            Vector3 origin = target.position;
            Quaternion rot = target.rotation;

            GL.PushMatrix();

            switch (type)
            {
                case GizmoType.Move:
                    DrawMoveGizmo(origin, rot, worldSize);
                    break;
                case GizmoType.Rotate:
                    DrawRotateGizmo(origin, rot, worldSize, cam);
                    break;
                case GizmoType.Scale:
                    DrawScaleGizmo(origin, rot, worldSize);
                    break;
            }

            GL.PopMatrix();
        }

        // ---------------------------------------------------------------
        // MOVE: three arrows along local axes + small plane handles
        // ---------------------------------------------------------------
        void DrawMoveGizmo(Vector3 origin, Quaternion rot, float s)
        {
            DrawAxisArrow(origin, rot * Vector3.right, s, ColorFor(0), 0);
            DrawAxisArrow(origin, rot * Vector3.up, s, ColorFor(1), 1);
            DrawAxisArrow(origin, rot * Vector3.forward, s, ColorFor(2), 2);

            // Small square handles on each plane for planar dragging (XY, YZ, XZ)
            DrawPlaneHandle(origin, rot * Vector3.right, rot * Vector3.up, s, ColorFor(2));    // XY plane -> Z tint
            DrawPlaneHandle(origin, rot * Vector3.up, rot * Vector3.forward, s, ColorFor(0));  // YZ plane -> X tint
            DrawPlaneHandle(origin, rot * Vector3.forward, rot * Vector3.right, s, ColorFor(1)); // ZX plane -> Y tint
        }

        void DrawAxisArrow(Vector3 origin, Vector3 dir, float s, Color color, int axisIndex)
        {
            Vector3 end = origin + dir * s;
            GL.Begin(GL.LINES);
            GL.Color(color);
            GL.Vertex(origin);
            GL.Vertex(end);
            GL.End();

            DrawCone(end, dir, s * ArrowHeadLength, s * ArrowHeadWidth, color);
        }

        void DrawCone(Vector3 tip, Vector3 dir, float length, float width, Color color)
        {
            Vector3 baseCenter = tip - dir * length;
            Vector3 up = Vector3.Cross(dir, Vector3.right);
            if (up.sqrMagnitude < 0.001f) up = Vector3.Cross(dir, Vector3.up);
            up.Normalize();
            Vector3 right = Vector3.Cross(dir, up).normalized;

            const int segs = 10;
            GL.Begin(GL.TRIANGLES);
            GL.Color(color);
            for (int i = 0; i < segs; i++)
            {
                float a0 = (float)i / segs * Mathf.PI * 2f;
                float a1 = (float)(i + 1) / segs * Mathf.PI * 2f;
                Vector3 p0 = baseCenter + (up * Mathf.Cos(a0) + right * Mathf.Sin(a0)) * width;
                Vector3 p1 = baseCenter + (up * Mathf.Cos(a1) + right * Mathf.Sin(a1)) * width;
                GL.Vertex(tip);
                GL.Vertex(p0);
                GL.Vertex(p1);
            }
            GL.End();
        }

        void DrawPlaneHandle(Vector3 origin, Vector3 axisA, Vector3 axisB, float s, Color color)
        {
            Vector3 center = origin + (axisA + axisB) * (s * PlaneHandleOffset);
            float half = s * PlaneHandleSize * 0.5f;
            Vector3 p0 = center - axisA * half - axisB * half;
            Vector3 p1 = center + axisA * half - axisB * half;
            Vector3 p2 = center + axisA * half + axisB * half;
            Vector3 p3 = center - axisA * half + axisB * half;

            Color fill = color; fill.a = 0.35f;
            GL.Begin(GL.QUADS);
            GL.Color(fill);
            GL.Vertex(p0); GL.Vertex(p1); GL.Vertex(p2); GL.Vertex(p3);
            GL.End();
        }

        // ---------------------------------------------------------------
        // ROTATE: three rings, one per axis, facing the camera-friendly way
        // ---------------------------------------------------------------
        void DrawRotateGizmo(Vector3 origin, Quaternion rot, float s, Camera cam)
        {
            DrawRing(origin, rot * Vector3.right, s, ColorFor(0));   // rotation around X
            DrawRing(origin, rot * Vector3.up, s, ColorFor(1));      // rotation around Y
            DrawRing(origin, rot * Vector3.forward, s, ColorFor(2)); // rotation around Z

            // Outer "camera facing" ring, like Unity's screen-space rotate ring
            Vector3 toCam = (cam.transform.position - origin).normalized;
            DrawRing(origin, toCam, s * 1.15f, new Color(0.9f, 0.9f, 0.9f, 0.6f));
        }

        void DrawRing(Vector3 origin, Vector3 normal, float radius, Color color)
        {
            Vector3 up = Vector3.Cross(normal, Vector3.right);
            if (up.sqrMagnitude < 0.001f) up = Vector3.Cross(normal, Vector3.up);
            up.Normalize();
            Vector3 right = Vector3.Cross(up, normal).normalized;

            GL.Begin(GL.LINES);
            GL.Color(color);
            Vector3 prev = origin + right * radius;
            for (int i = 1; i <= RingSegments; i++)
            {
                float a = (float)i / RingSegments * Mathf.PI * 2f;
                Vector3 cur = origin + (right * Mathf.Cos(a) + up * Mathf.Sin(a)) * radius;
                GL.Vertex(prev);
                GL.Vertex(cur);
                prev = cur;
            }
            GL.End();
        }

        // ---------------------------------------------------------------
        // SCALE: three axis lines ending in small cubes + center uniform cube
        // ---------------------------------------------------------------
        void DrawScaleGizmo(Vector3 origin, Quaternion rot, float s)
        {
            DrawScaleAxis(origin, rot * Vector3.right, s, ColorFor(0));
            DrawScaleAxis(origin, rot * Vector3.up, s, ColorFor(1));
            DrawScaleAxis(origin, rot * Vector3.forward, s, ColorFor(2));

            DrawCube(origin, rot, s * ScaleHandleSize * 1.3f, new Color(0.9f, 0.9f, 0.9f, 0.9f));
        }

        void DrawScaleAxis(Vector3 origin, Vector3 dir, float s, Color color)
        {
            Vector3 end = origin + dir * s;
            GL.Begin(GL.LINES);
            GL.Color(color);
            GL.Vertex(origin);
            GL.Vertex(end);
            GL.End();

            DrawCube(end, Quaternion.LookRotation(dir), s * ScaleHandleSize, color);
        }

        void DrawCube(Vector3 center, Quaternion rot, float half, Color color)
        {
            Vector3[] c = new Vector3[8];
            int idx = 0;
            for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                    for (int z = -1; z <= 1; z += 2)
                        c[idx++] = center + rot * new Vector3(x, y, z) * half;

            int[,] faces =
            {
            {0,1,3,2}, {4,6,7,5}, {0,2,6,4},
            {1,5,7,3}, {0,4,5,1}, {2,3,7,6}
        };

            GL.Begin(GL.QUADS);
            GL.Color(color);
            for (int f = 0; f < 6; f++)
            {
                GL.Vertex(c[faces[f, 0]]);
                GL.Vertex(c[faces[f, 1]]);
                GL.Vertex(c[faces[f, 2]]);
                GL.Vertex(c[faces[f, 3]]);
            }
            GL.End();
        }

        // ---------------------------------------------------------------
        Color ColorFor(int axisIndex)
        {
            Color baseColor = axisIndex == 0 ? axisColorX : axisIndex == 1 ? axisColorY : axisColorZ;
            return hoveredAxis == axisIndex ? highlightColor : baseColor;
        }
    }
}
