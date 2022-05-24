using RenderWareIo.Structs.Dff;
using RenderWarePreviewer.Extensions;
using RenderWarePreviewer.Helpers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace RenderWarePreviewer.Scenes
{
    public class Scene
    {
        private readonly PerspectiveCamera camera;
        private readonly ModelVisual3D visual;
        private readonly Transform3DGroup transformGroup;
        private readonly Transform3DGroup cameraTransformGroup;
        private readonly Model3DGroup group;
        private readonly AmbientLight light;

        private readonly Vector3D up = new Vector3D(0, 0, 1);
        private readonly Vector3D right = new Vector3D(1, 0, 0);
        private readonly Vector3D forward = new Vector3D(0, 1, 0);

        private Vector3 cameraPosition;
        private Vector3 cameraRotation;

        public Scene()
        {
            this.light = new AmbientLight(System.Windows.Media.Color.FromArgb(255, 255, 255, 255));
            this.transformGroup = new();

            this.group = new Model3DGroup();
            this.group.Children.Add(light);
            this.group.Transform = this.transformGroup;

            this.visual = new ModelVisual3D();
            visual.Content = this.group;

            this.cameraTransformGroup = new();
            var cameraOffset = new Point3D(0, 2, 0);
            var lookDirection = -(Vector3D)cameraOffset;
            lookDirection.Normalize();
            var upDirection = new Vector3D(0, -1, 1);
            upDirection.Normalize();
            this.camera = new PerspectiveCamera()
            {
                FieldOfView = 60,
                FarPlaneDistance = 30000,
                NearPlaneDistance = .1f,
                Transform = this.cameraTransformGroup,
            };
            this.SetCameraPosition(new Vector3(0, 2, 0));
            this.SetCameraRotation(new Vector3(180, 180, 0));
        }

        public void Clear()
        {
            this.group.Children.Clear();
            this.group.Children.Add(light);
        }

        public void Add(Dff dff, Vector3D position, Image<Rgba32>? texture)
        {
            var model = MeshHelper.GetModel(dff, texture);
            model.Transform = new TranslateTransform3D(position);
            this.group.Children.Add(model);
        }

        public void Add(GeometryModel3D model, Vector3D position, Vector3D rotation)
        {
            var group = new Transform3DGroup();
            group.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(this.right, -rotation.X)));
            group.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(this.forward, -rotation.Y)));
            group.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(this.up, -rotation.Z)));
            group.Children.Add(new TranslateTransform3D(position));

            model.Transform = group;
            this.group.Children.Add(model);
        }

        public void AddPyramid(Vector3D position, System.Windows.Media.Color color, float scale)
        {
            var model = MeshHelper.GetPyramid(color, scale);
            model.Transform = new TranslateTransform3D(position);
            this.group.Children.Add(model);
        }

        public void Add(MeshGeometry3D mesh, System.Windows.Media.Media3D.Material material, Vector3D position)
        {
            var model = MeshHelper.GetModel(mesh, material);
            model.Transform = new TranslateTransform3D(position);
            this.group.Children.Add(model);
        }

        public void ApplyTo(Viewport3D viewport)
        {
            viewport.Children.Clear();
            viewport.Children.Add(visual);
            viewport.Camera = this.camera;
        }

        public void RotateWorld(float angle)
        {
            this.transformGroup.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(this.up, angle)));
        }

        public void ZoomCamera(float value)
        {
            this.SetCameraPosition(this.CameraPosition + this.CameraForward * -value);
            //this.SetCameraPosition(new Vector3((float)this.camera.Position.X, (float)this.camera.Position.Y + value, (float)this.camera.Position.Z + value));
        }

        public void SetCameraPosition(Vector3 position)
        {
            this.cameraPosition = position;
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Input, () =>
            {
                this.camera.Position = position.ToPoint3D();
            });
        }

        public void SetCameraRotation(Vector3 rotation)
        {
            this.cameraRotation = rotation;

            Application.Current.Dispatcher.Invoke(DispatcherPriority.Input, () =>
            {
                //var oldPosition = this.cameraPosition;
                //this.cameraTransformGroup.Children.Clear();
                //this.cameraTransformGroup.Children.Add(new TranslateTransform3D((-oldPosition).ToVector3D()));
                //this.cameraTransformGroup.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(this.right, rotation.X)));
                //this.cameraTransformGroup.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(this.forward, rotation.Y)));
                //this.cameraTransformGroup.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(this.up, rotation.Z)));
                //this.cameraTransformGroup.Children.Add(new TranslateTransform3D(oldPosition.ToVector3D()));
                this.camera.LookDirection = this.CameraForward.ToVector3D();
                this.camera.UpDirection = this.CameraUp.ToVector3D();
            });
        }


        public Vector3 CameraPosition
            => this.cameraPosition;

        public Vector3 CameraForward
            => TransformAround(this.forward, this.cameraRotation).ToVector3();

        public Vector3 CameraRight
            => TransformAround(this.right, this.cameraRotation).ToVector3();

        public Vector3 CameraUp
            => TransformAround(this.up, this.cameraRotation).ToVector3();

        public Vector3D TransformAround(Vector3D point, Vector3 rotation)
        {
            var group = new Transform3DGroup();
            group.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(this.right, -rotation.X)));
            group.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(this.forward, -rotation.Y)));
            group.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(this.up, -rotation.Z)));

            return group.Transform(point);
        }

        public void MoveCamera(Vector3 delta)
        {
            var newPosition =
                this.CameraPosition +
                this.CameraRight * delta.X +
                this.CameraUp * delta.Z +
                this.CameraForward * delta.Y;

            this.SetCameraPosition(newPosition);
        }
    }
}
