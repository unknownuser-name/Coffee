using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace CloudCafe.Crafting
{
    public sealed class StepTargetGlow : IDisposable
    {
        private sealed class ToolOverlay
        {
            public MeshRenderer Source;
            public MeshFilter SourceMesh;
            public GameObject Visual;
            public MeshFilter VisualMesh;
            public MeshRenderer Renderer;
            public int MaterialSlots;
        }

        private const int RingSegments = 56;
        private static readonly int GlowColor = Shader.PropertyToID("_GlowColor");
        private static readonly int OutlineWidth = Shader.PropertyToID("_OutlineWidth");
        private readonly Dictionary<MeshRenderer, ToolOverlay> overlays = new Dictionary<MeshRenderer, ToolOverlay>();
        private readonly HashSet<MeshRenderer> ownedRenderers = new HashSet<MeshRenderer>();
        private readonly List<ToolOverlay> selectedOverlays = new List<ToolOverlay>();
        private readonly Material toolMaterial;
        private readonly Material destinationMaterial;
        private readonly GameObject destinationRoot;
        private readonly Transform arrowRoot;
        private readonly LineRenderer destinationRing;
        private readonly Mesh arrowMesh;
        private Transform selectedPickup;
        private bool disposed;

        public int HighlightedRendererCount { get; private set; }
        public bool DestinationVisible => !disposed && destinationRoot != null && destinationRoot.activeSelf;

        public StepTargetGlow()
        {
            Shader shader = Shader.Find("CloudCafe/StepTargetGlow");
            if (shader == null) throw new InvalidOperationException("CloudCafe/StepTargetGlow shader is missing.");
            toolMaterial = new Material(shader)
            {
                name = "Current tool gold outline",
                hideFlags = HideFlags.DontSave
            };
            toolMaterial.SetFloat("_Cull", (float)CullMode.Front);
            toolMaterial.SetFloat("_ZTest", (float)CompareFunction.LessEqual);
            destinationMaterial = new Material(shader)
            {
                name = "Current destination cyan glow",
                hideFlags = HideFlags.DontSave
            };
            destinationMaterial.SetFloat(OutlineWidth, 0f);
            destinationMaterial.SetFloat("_Cull", (float)CullMode.Off);
            destinationMaterial.SetFloat("_ZTest", (float)CompareFunction.Always);

            destinationRoot = NewVisual("Current step destination", null);
            destinationRing = destinationRoot.AddComponent<LineRenderer>();
            destinationRing.sharedMaterial = destinationMaterial;
            destinationRing.useWorldSpace = false;
            destinationRing.loop = true;
            destinationRing.positionCount = RingSegments;
            destinationRing.widthMultiplier = .018f;
            destinationRing.numCapVertices = 3;
            destinationRing.numCornerVertices = 3;
            destinationRing.shadowCastingMode = ShadowCastingMode.Off;
            destinationRing.receiveShadows = false;
            destinationRing.lightProbeUsage = LightProbeUsage.Off;
            destinationRing.reflectionProbeUsage = ReflectionProbeUsage.Off;
            for (int i = 0; i < RingSegments; i++)
            {
                float angle = i * Mathf.PI * 2f / RingSegments;
                destinationRing.SetPosition(i, new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)));
            }

            arrowRoot = NewVisual("Destination down arrow", destinationRoot.transform).transform;
            arrowMesh = CreateArrowMesh();
            for (int side = 0; side < 2; side++)
            {
                GameObject arrow = NewVisual("Cyan arrow plane", arrowRoot);
                arrow.transform.localRotation = Quaternion.Euler(0, side * 90f, 0);
                arrow.AddComponent<MeshFilter>().sharedMesh = arrowMesh;
                MeshRenderer renderer = arrow.AddComponent<MeshRenderer>();
                ConfigureRenderer(renderer, destinationMaterial);
                ownedRenderers.Add(renderer);
            }
            destinationRoot.SetActive(false);
        }

        public void Show(Transform pickup, Vector3? destination, float radius, float time)
        {
            if (disposed || destinationRoot == null) return;
            if (float.IsNaN(time) || float.IsInfinity(time)) time = 0;
            float pulse = .5f + .5f * Mathf.Sin(time * 4.2f);
            toolMaterial.SetColor(GlowColor, new Color(1f, .67f, .12f, .75f + .2f * pulse));
            toolMaterial.SetFloat(OutlineWidth, .015f + .006f * pulse);
            UpdatePickup(pickup);

            bool validDestination = destination.HasValue && Finite(destination.Value);
            destinationRoot.SetActive(validDestination);
            if (!validDestination) return;
            if (float.IsNaN(radius) || float.IsInfinity(radius) || radius <= 0) radius = .3f;
            radius = Mathf.Clamp(radius, .08f, 1.2f);
            destinationRoot.transform.position = destination.Value + Vector3.up * .014f;
            float ringRadius = radius * (.95f + .07f * pulse);
            destinationRing.transform.localScale = Vector3.one;
            for (int i = 0; i < RingSegments; i++)
            {
                float angle = i * Mathf.PI * 2f / RingSegments;
                destinationRing.SetPosition(i, new Vector3(Mathf.Cos(angle) * ringRadius, 0, Mathf.Sin(angle) * ringRadius));
            }
            float arrowScale = Mathf.Clamp(radius / .3f, .75f, 1.4f);
            arrowRoot.localScale = Vector3.one * arrowScale;
            arrowRoot.localPosition = Vector3.up * (.025f + .035f * pulse);
            destinationMaterial.SetColor(GlowColor, new Color(.15f, .92f, 1f, .65f + .3f * pulse));
        }

        public void Hide()
        {
            if (disposed) return;
            HighlightedRendererCount = 0;
            foreach (ToolOverlay overlay in selectedOverlays)
                if (overlay.Visual != null) overlay.Visual.SetActive(false);
            if (destinationRoot != null) destinationRoot.SetActive(false);
        }

        private void UpdatePickup(Transform pickup)
        {
            if (selectedPickup != pickup)
            {
                foreach (ToolOverlay previous in selectedOverlays)
                    if (previous.Visual != null) previous.Visual.SetActive(false);
                selectedOverlays.Clear();
                selectedPickup = pickup;
                if (pickup != null)
                {
                    foreach (MeshRenderer source in pickup.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        if (ownedRenderers.Contains(source)) continue;
                        MeshFilter filter = source.GetComponent<MeshFilter>();
                        if (filter == null || filter.sharedMesh == null) continue;
                        if (!overlays.TryGetValue(source, out ToolOverlay overlay))
                        {
                            GameObject visual = NewVisual("Current tool outline", source.transform);
                            MeshFilter visualMesh = visual.AddComponent<MeshFilter>();
                            MeshRenderer renderer = visual.AddComponent<MeshRenderer>();
                            ConfigureRenderer(renderer, toolMaterial);
                            ownedRenderers.Add(renderer);
                            overlay = new ToolOverlay
                            {
                                Source = source,
                                SourceMesh = filter,
                                Visual = visual,
                                VisualMesh = visualMesh,
                                Renderer = renderer
                            };
                            overlays.Add(source, overlay);
                        }
                        selectedOverlays.Add(overlay);
                    }
                }
            }

            HighlightedRendererCount = 0;
            foreach (ToolOverlay overlay in selectedOverlays)
            {
                bool visible = overlay.Source != null && overlay.SourceMesh != null &&
                    overlay.Source.enabled && overlay.Source.gameObject.activeInHierarchy &&
                    overlay.SourceMesh.sharedMesh != null;
                if (overlay.Visual == null) continue;
                overlay.Visual.SetActive(visible);
                if (!visible) continue;
                Mesh mesh = overlay.SourceMesh.sharedMesh;
                overlay.VisualMesh.sharedMesh = mesh;
                int slots = Math.Max(1, mesh.subMeshCount);
                if (overlay.MaterialSlots != slots)
                {
                    var materials = new Material[slots];
                    for (int i = 0; i < slots; i++) materials[i] = toolMaterial;
                    overlay.Renderer.sharedMaterials = materials;
                    overlay.MaterialSlots = slots;
                }
                HighlightedRendererCount++;
            }
        }

        private static GameObject NewVisual(string name, Transform parent)
        {
            var visual = new GameObject(name)
            {
                layer = 2,
                hideFlags = HideFlags.DontSave
            };
            if (parent != null) visual.transform.SetParent(parent, false);
            return visual;
        }

        private static void ConfigureRenderer(MeshRenderer renderer, Material material)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        private static Mesh CreateArrowMesh()
        {
            var mesh = new Mesh { name = "Step destination arrow", hideFlags = HideFlags.DontSave };
            mesh.vertices = new[]
            {
                new Vector3(-.025f, .43f, 0), new Vector3(.025f, .43f, 0),
                new Vector3(-.025f, .18f, 0), new Vector3(.025f, .18f, 0),
                new Vector3(-.095f, .18f, 0), new Vector3(.095f, .18f, 0),
                new Vector3(0, .045f, 0)
            };
            mesh.triangles = new[] { 0, 1, 2, 2, 1, 3, 4, 5, 6 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static bool Finite(Vector3 value) =>
            !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
            !float.IsNaN(value.y) && !float.IsInfinity(value.y) &&
            !float.IsNaN(value.z) && !float.IsInfinity(value.z);

        public void Dispose()
        {
            if (disposed) return;
            Hide();
            disposed = true;
            foreach (ToolOverlay overlay in overlays.Values)
            {
                if (overlay.Visual == null) continue;
                overlay.Visual.SetActive(false);
                Object.Destroy(overlay.Visual);
            }
            Object.Destroy(destinationRoot);
            Object.Destroy(toolMaterial);
            Object.Destroy(destinationMaterial);
            Object.Destroy(arrowMesh);
            overlays.Clear();
            selectedOverlays.Clear();
            ownedRenderers.Clear();
            selectedPickup = null;
        }
    }
}
