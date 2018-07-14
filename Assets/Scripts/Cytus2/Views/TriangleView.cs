using Cytus2.Models;
using UnityEngine;

namespace Cytus2.Views
{
    public class TriangleView : MonoBehaviour
    {
        public ChartNote Note;
        public bool IsShowing;

        private Mesh mesh;
        private ScanlineView scanline;
        private Camera mainCamera;

        private void OnEnable()
        {
            IsShowing = false;
            mesh = gameObject.GetComponent<MeshFilter>().mesh;
            mesh.vertices = new[]
            {
                new Vector3(),
                new Vector3(),
                new Vector3()
            };
            mesh.uv = new[]
            {
                new Vector2(),
                new Vector2(),
                new Vector2()
            };
            mesh.triangles = new[] {0, 1, 2};
            scanline = ScanlineView.Instance;
            mainCamera = Camera.main;
        }

        public void Reset()
        {
            mesh.vertices = new[]
            {
                new Vector3(),
                new Vector3(),
                new Vector3()
            };
            mesh.uv = new[]
            {
                new Vector2(),
                new Vector2(),
                new Vector2()
            };
            mesh.triangles = new[] {0, 1, 2};
        }

        private void Update()
        {
            if (IsShowing)
            {
                mesh.vertices = new[]
                {
                    Note.position,
                    new Vector3(-mainCamera.orthographicSize * Screen.width / Screen.height,
                        scanline.transform.position.y),
                    new Vector3(mainCamera.orthographicSize * Screen.width / Screen.height,
                        scanline.transform.position.y)
                };

                mesh.uv = new[]
                {
                    new Vector2(Note.position.x, Note.position.y),
                    new Vector2(-mainCamera.orthographicSize * Screen.width / Screen.height,
                        scanline.transform.position.y),
                    new Vector2(mainCamera.orthographicSize * Screen.width / Screen.height,
                        scanline.transform.position.y)
                };
                mesh.triangles = new[] {0, 1, 2};
            }
            else
            {
                mesh.vertices = new[]
                {
                    new Vector3(),
                    new Vector3(),
                    new Vector3()
                };
                mesh.uv = new[]
                {
                    new Vector2(),
                    new Vector2(),
                    new Vector2()
                };
                mesh.triangles = new[] {0, 1, 2};
            }
        }
    }
}