using UnityEngine;
using UnityEngine.UI;

namespace Cytoid.Storyboard.Sprites
{
    public sealed class StoryboardImage : Image
    {
        protected override void UpdateGeometry()
        {
            base.UpdateGeometry();
            if (canvas == null || canvas.rootCanvas.renderMode != RenderMode.ScreenSpaceCamera) return;
            var camera = canvas.rootCanvas.worldCamera;
            if (camera == null || camera.orthographic) return;

            // Perspective Canvas culling can reject depth-transformed sprites that
            // still project into view. Keep their vertices and bounds center intact,
            // but give the native culling test a conservative extent.
            var mesh = workerMesh;
            var bounds = mesh.bounds;
            bounds.extents = Vector3.Max(bounds.extents, Vector3.one * 1000000f);
            mesh.bounds = bounds;
            canvasRenderer.SetMesh(mesh);
        }
    }
}
