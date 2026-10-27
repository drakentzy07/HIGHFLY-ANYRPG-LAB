// HIGHFLY A6: world-space targeting feedback.
// A skill button is only an input: trajectories always originate at the hunter.
using UnityEngine;
using UnityEngine.Rendering;

namespace HIGHFLY.Mobile
{
    [DisallowMultipleComponent]
    public sealed class HighflyA6AimGuide : MonoBehaviour
    {
        private LineRenderer trajectory;
        private LineRenderer impactRing;
        private Material lineMaterial;
        private const int Segments = 48;

        private void Awake()
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null)
            {
                Debug.LogWarning("[HIGHFLY A6] No compatible shader for world aiming preview.");
                return;
            }
            lineMaterial = new Material(shader);
            trajectory = CreateLine("FURY world trajectory", 0.11f, false);
            impactRing = CreateLine("FURY ground impact radius", 0.085f, true);
            Hide();
        }

        private LineRenderer CreateLine(string lineName, float width, bool loop)
        {
            var child = new GameObject(lineName);
            child.transform.SetParent(transform, false);
            var line = child.AddComponent<LineRenderer>();
            line.sharedMaterial = lineMaterial;
            line.useWorldSpace = true;
            line.loop = loop;
            line.positionCount = 0;
            line.startWidth = width;
            line.endWidth = width * .7f;
            line.numCapVertices = 4;
            line.numCornerVertices = 3;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.startColor = new Color(.15f,.84f,1f,.9f);
            line.endColor = new Color(.62f,.96f,1f,.88f);
            return line;
        }

        public void Show(Vector3 from, Vector3 to, float radius, bool valid)
        {
            if (trajectory == null) return;
            trajectory.enabled = true;
            trajectory.startColor = valid ? new Color(.19f,.86f,1f,.95f) : new Color(1f,.24f,.27f,.95f);
            trajectory.endColor = trajectory.startColor;
            trajectory.positionCount = 2;
            trajectory.SetPosition(0, from + Vector3.up * .22f);
            trajectory.SetPosition(1, to + Vector3.up * .13f);
            if (impactRing == null) return;
            impactRing.enabled = valid && radius > .05f;
            if (!impactRing.enabled) return;
            impactRing.positionCount = Segments;
            for (int i=0; i<Segments; i++)
            {
                float angle = (Mathf.PI * 2f * i) / Segments;
                impactRing.SetPosition(i, to + new Vector3(
                    Mathf.Cos(angle) * radius, .14f, Mathf.Sin(angle) * radius));
            }
        }

        public void Hide()
        {
            if (trajectory != null) trajectory.enabled = false;
            if (impactRing != null) impactRing.enabled = false;
        }

        private void OnDestroy()
        {
            if (lineMaterial != null) Destroy(lineMaterial);
        }
    }
}
