using System;

namespace Easy3D.Core
{
    public enum ProjectionType { Perspective, Orthographic }

    public class Camera
    {
        public Vector3 Position { get; set; } = new(3, 3, 3);
        public Vector3 Target { get; set; } = Vector3.Zero;
        public Vector3 Up { get; set; } = Vector3.Up;
        public ProjectionType Projection { get; set; } = ProjectionType.Perspective;
        public float FieldOfView { get; set; } = 60f;
        public float NearPlane { get; set; } = 0.1f;
        public float FarPlane { get; set; } = 1000f;
        public float OrthoSize { get; set; } = 5f;
        public float Pitch { get; private set; } = 0.45f;
        public float Yaw { get; private set; } = -0.75f;
        public float Distance { get; private set; } = 5f;
        public float Sensitivity { get; set; } = 0.005f;
        public float ZoomSpeed { get; set; } = 0.1f;
        public float MoveSpeed { get; set; } = 0.01f;

        public Matrix4x4 ViewMatrix { get; private set; }
        public Matrix4x4 ProjectionMatrix { get; private set; }
        public Matrix4x4 VPMatrix { get; private set; }

        public Camera() => UpdateViewMatrix();

        public void UpdateViewMatrix()
        {
            Position = new Vector3(
                Distance * MathF.Cos(Pitch) * MathF.Sin(Yaw),
                Distance * MathF.Sin(Pitch),
                Distance * MathF.Cos(Pitch) * MathF.Cos(Yaw)
            ) + Target;
            ViewMatrix = Matrix4x4.LookAt(Position, Target, Up);
        }

        public void UpdateProjectionMatrix(float aspect)
        {
            ProjectionMatrix = Projection == ProjectionType.Perspective
                ? Matrix4x4.Perspective(FieldOfView * MathF.PI / 180f, aspect, NearPlane, FarPlane)
                : Matrix4x4.Orthographic(-OrthoSize * aspect, OrthoSize * aspect, -OrthoSize, OrthoSize, NearPlane, FarPlane);
            VPMatrix = ProjectionMatrix * ViewMatrix;
        }

        public Vector3 GetForward() => (Target - Position).Normalized();
        public Vector3 GetRight() => GetForward().Cross(Up).Normalized();
        public Vector3 GetUp() => GetRight().Cross(GetForward()).Normalized();

        public void Zoom(float delta) { Distance = Math.Clamp(Distance - delta * ZoomSpeed, 0.5f, 200f); UpdateViewMatrix(); }
        public void Orbit(float dx, float dy) { Yaw -= dx * Sensitivity; Pitch = Math.Clamp(Pitch + dy * Sensitivity, -1.55f, 1.55f); UpdateViewMatrix(); }
        public void Pan(float dx, float dy) { var r = GetRight(); var u = GetUp(); Target += r * dx * MoveSpeed * Distance + u * dy * MoveSpeed * Distance; UpdateViewMatrix(); }
        public void Reset() { Position = new(3, 3, 3); Target = Vector3.Zero; Pitch = 0.45f; Yaw = -0.75f; Distance = 5f; UpdateViewMatrix(); }

        public void SetView(CameraView view)
        {
            switch (view)
            {
                case CameraView.Front: Pitch = 0; Yaw = 0; break;
                case CameraView.Back: Pitch = 0; Yaw = MathF.PI; break;
                case CameraView.Right: Pitch = 0; Yaw = -MathF.PI / 2; break;
                case CameraView.Left: Pitch = 0; Yaw = MathF.PI / 2; break;
                case CameraView.Top: Pitch = 1.55f; Yaw = 0; break;
                case CameraView.Bottom: Pitch = -1.55f; Yaw = 0; break;
                case CameraView.FrontLeft: Pitch = 0.3f; Yaw = MathF.PI / 4; break;
                case CameraView.FrontRight: Pitch = 0.3f; Yaw = -MathF.PI / 4; break;
            }
            UpdateViewMatrix();
        }

        public Vector3? WorldToScreen(Vector3 worldPos, int screenW, int screenH)
        {
            var clip = VPMatrix.Transform(new Vector4(worldPos, 1));
            if (clip.W <= 0) return null;
            var ndc = new Vector3(clip.X / clip.W, clip.Y / clip.W, clip.Z / clip.W);
            return new Vector3((ndc.X + 1) * screenW / 2, (1 - ndc.Y) * screenH / 2, ndc.Z);
        }

        public (Vector3 origin, Vector3 direction) ScreenToWorldRay(float sx, float sy, int screenW, int screenH)
        {
            float nx = (2 * sx / screenW) - 1, ny = 1 - (2 * sy / screenH);
            var near = VPMatrix.Inverse().Transform(new Vector4(nx, ny, -1, 1));
            var far = VPMatrix.Inverse().Transform(new Vector4(nx, ny, 1, 1));
            var o = near.ToVector3();
            return (o, (far.ToVector3() - o).Normalized());
        }

        public Camera Clone() => new()
        {
            Position = Position, Target = Target, Up = Up, Projection = Projection,
            FieldOfView = FieldOfView, NearPlane = NearPlane, FarPlane = FarPlane,
            OrthoSize = OrthoSize, Pitch = Pitch, Yaw = Yaw, Distance = Distance,
            Sensitivity = Sensitivity, ZoomSpeed = ZoomSpeed, MoveSpeed = MoveSpeed
        };
    }

    public enum CameraView
    {
        Front, Back, Right, Left, Top, Bottom,
        FrontLeft, FrontRight, Perspective
    }
}
