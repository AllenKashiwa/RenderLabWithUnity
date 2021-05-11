using System.Collections.Generic;

namespace UnityEngine.Rendering.Universal
{
    public class ScreenSpaceScannerRenderFeature : ScriptableRendererFeature
    {
        public Material m_Material;
        private ScreenSpaceScannerPass m_SSScannerPass;

        public override void Create()
        {
            if (m_SSScannerPass == null)
            {
                m_SSScannerPass = new ScreenSpaceScannerPass();
            }

            m_SSScannerPass.profilerTag = name;
            m_SSScannerPass.material = m_Material;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (m_Material == null)
            {
                Debug.LogErrorFormat(
                    "{0}.AddRenderPasses(): Missing material. {1} render pass will not be added. Check for missing reference in the renderer resources.",
                    GetType().Name, m_SSScannerPass.profilerTag);
                return;
            }

            renderer.EnqueuePass(m_SSScannerPass);
        }

        private class ScreenSpaceScannerPass : ScriptableRenderPass
        {
            // Public Variables
            internal string profilerTag;
            internal Material material;

            // Constants

            internal ScreenSpaceScannerPass()
            {
                renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
            }

            /// <inheritdoc/>
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (material == null)
                {
                    Debug.LogErrorFormat(
                        "{0}.Execute(): Missing material. {1} render pass will not execute. Check for missing reference in the renderer resources.",
                        GetType().Name, profilerTag);
                    return;
                }

                var cameraData = renderingData.cameraData;
                if (!cameraData.requiresDepthTexture)
                {
                    Debug.LogErrorFormat(
                        "{0}.Execute(): camera don'r require depth texture. {1} render pass will not execute. Check for missing render pipeline settings in the renderer resources.",
                        GetType().Name, profilerTag);
                    return;
                }


                Camera camera = cameraData.camera;

                CommandBuffer cmd = CommandBufferPool.Get();

                cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
                if (s_FullscreenMesh == null)
                {
                    float topV = 1.0f;
                    float bottomV = 0.0f;

                    s_FullscreenMesh = new Mesh {name = "Fullscreen Quad"};
                    s_FullscreenMesh.SetVertices(new List<Vector3>
                    {
                        new Vector3(-1.0f, -1.0f, 0.0f),
                        new Vector3(-1.0f, 1.0f, 0.0f),
                        new Vector3(1.0f, -1.0f, 0.0f),
                        new Vector3(1.0f, 1.0f, 0.0f)
                    });

                    s_FullscreenMesh.SetUVs(0, new List<Vector2>
                    {
                        new Vector2(0.0f, bottomV),
                        new Vector2(0.0f, topV),
                        new Vector2(1.0f, bottomV),
                        new Vector2(1.0f, topV)
                    });

                    s_FullscreenMesh.SetIndices(new[] {0, 1, 2, 2, 1, 3}, MeshTopology.Triangles, 0, false);
                }

                GetRaycastCamera(camera, out var bottomLeft, out var topLeft, out var bottomRight, out var topRight);

                s_FullscreenMesh.SetUVs(1, new List<Vector3>
                {
                    bottomLeft,
                    topLeft,
                    bottomRight,
                    topRight
                });

                s_FullscreenMesh.UploadMeshData(false);
                cmd.DrawMesh(s_FullscreenMesh, Matrix4x4.identity, material);
                cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            private void GetRaycastCamera(Camera camera, out Vector3 bottomLeft, out Vector3 topLeft, out Vector3 bottomRight, out Vector3 topRight)
            {
                float camFar = camera.farClipPlane;
                float camFov = camera.fieldOfView;
                float camAspect = camera.aspect;
                float fovWHalf = camFov * 0.5f;

                Vector3 toRight = camera.transform.right * Mathf.Tan(fovWHalf * Mathf.Deg2Rad) * camAspect;
                Vector3 toTop = camera.transform.up * Mathf.Tan(fovWHalf * Mathf.Deg2Rad);

                var forward = camera.transform.forward;
                topLeft = forward - toRight + toTop;
                float camScale = topLeft.magnitude * camFar;

                topLeft.Normalize();
                topLeft *= camScale;

                topRight = forward + toRight + toTop;
                topRight.Normalize();
                topRight *= camScale;

                bottomRight = forward + toRight - toTop;
                bottomRight.Normalize();
                bottomRight *= camScale;

                bottomLeft = forward - topRight - toTop;
                bottomLeft.Normalize();
                bottomLeft *= camScale;
            }

            static Mesh s_FullscreenMesh;
        }
    }


}
