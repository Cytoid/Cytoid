namespace Cytoid.Storyboard.Controllers
{
    public class CameraEaser : StoryboardRendererEaser<ControllerState>
    {
        public override void OnUpdate()
        {
            var camera = Provider.Camera;
            var transform = camera.transform;

            // X
            if (From.X.IsSet())
            {
                transform.SetX(EaseFloat(From.X, To.X));
            }

            // Y
            if (From.Y.IsSet())
            {
                transform.SetY(EaseFloat(From.Y, To.Y));
            }

            // RotX
            if (From.RotX.IsSet())
            {
                var eulerAngles = transform.eulerAngles;
                eulerAngles.x = EaseFloat(From.RotX, To.RotX);
                transform.eulerAngles = eulerAngles;
            }

            // RotY
            if (From.RotY.IsSet())
            {
                var eulerAngles = transform.eulerAngles;
                eulerAngles.y = EaseFloat(From.RotY, To.RotY);
                transform.eulerAngles = eulerAngles;
            }

            // RotZ
            if (From.RotZ.IsSet())
            {
                var eulerAngles = transform.eulerAngles;
                eulerAngles.z = EaseFloat(From.RotZ, To.RotZ);
                transform.eulerAngles = eulerAngles;
            }

            // Perspective
            if (From.Perspective.IsSet())
            {
                camera.orthographic = !From.Perspective.Value;
                if (From.Perspective.Value && From.Fov.IsSet())
                {
                    camera.fieldOfView = EaseFloat(From.Fov, To.Fov);
                }
            }
        }
    }
}