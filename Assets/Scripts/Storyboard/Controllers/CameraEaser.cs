namespace Cytoid.Storyboard.Controllers
{
    public class CameraEaser : StoryboardRendererEaser<ControllerState>
    {
        public CameraEaser(StoryboardRenderer renderer) : base(renderer)
        {
        }
        
        public override void OnUpdate()
        {
            var camera = Provider.Camera;
            var transform = camera.transform;

            // X
            if (From.X != null)
            {
                transform.SetX(EaseFloat(From.X, To.X));
            }

            // Y
            if (From.Y != null)
            {
                transform.SetY(EaseFloat(From.Y, To.Y));
            }
            
            // Z
            if (From.Z != null)
            {
                transform.SetZ(EaseFloat(From.Z, To.Z));
            }

            // RotX
            if (From.RotX != null)
            {
                var eulerAngles = transform.eulerAngles;
                eulerAngles.x = EaseFloat(From.RotX, To.RotX);
                transform.eulerAngles = eulerAngles;
            }

            // RotY
            if (From.RotY != null)
            {
                var eulerAngles = transform.eulerAngles;
                eulerAngles.y = EaseFloat(From.RotY, To.RotY);
                transform.eulerAngles = eulerAngles;
            }

            // RotZ
            if (From.RotZ != null)
            {
                var eulerAngles = transform.eulerAngles;
                eulerAngles.z = EaseFloat(From.RotZ, To.RotZ);
                transform.eulerAngles = eulerAngles;
            }

            // Perspective
            if (From.Perspective != null)
            {
                camera.orthographic = !From.Perspective.Value;
                if (From.Perspective.Value && From.Fov != null)
                {
                    camera.fieldOfView = EaseFloat(From.Fov, To.Fov);
                }
            }
        }
    }
}