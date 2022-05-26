using RenderWareIo.Structs.Ide;
using RenderWarePreviewer.Helpers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Windows.Controls;
using System.Windows.Media.Media3D;

namespace RenderWarePreviewer.Scenes
{
    public class SceneManager
    {
        private readonly Scene scene;
        private readonly AssetHelper assetHelper;
        private readonly string backgroundDffName = "cj_changing_room";
        private readonly string backgroundTxdName = "CJ_CHANGE_ROOM";

        private Vector3 cameraRotation = new Vector3(180, 180, 0);

        public SceneManager()
        {
            this.scene = new Scene();
            this.assetHelper = new AssetHelper();
        }

        public void ZoomCamera(float value)
        {
            this.scene.ZoomCamera(value * .025f);
        }

        public void MoveCamera(Vector3 delta)
        {
            this.scene.MoveCamera(delta);
        }

        public void RotateCamera(Vector2 delta)
        {
            this.cameraRotation = new Vector3(
                (this.cameraRotation.X + delta.Y + 360) % 360,
                this.cameraRotation.Y,
                (this.cameraRotation.Z + delta.X + 360) % 360);
            this.scene.SetCameraRotation(this.cameraRotation);
        }

        public void LoadGta(string path)
        {
            this.assetHelper.SetGtaPath(path);
        }


        public IEnumerable<Ped> GetSkinNames()
        {
            return this.assetHelper.GetSkins();
        }

        public IEnumerable<string> GetTextures(Ped ped)
        {
            var txd = this.assetHelper.GetTxd(ped.TxdName);
            return txd.TextureContainer.Textures.Select(x => x.Data.TextureName);
        }

        public void LoadModel(Ped ped)
        {
            var dff = this.assetHelper.GetDff(ped.ModelName);
            var txd = this.assetHelper.GetTxd(ped.TxdName);

            var images = this.assetHelper.GetImages(txd);

            var models = MeshHelper.GetModels(dff, images);

            var rotation = DetermineRotation(models);
            this.scene.Clear();
            LoadBackgroundModels();
            foreach (var model in models)
                this.scene.Add(model, new Vector3D(0, 0, 0), rotation);
        }

        public void LoadModel(Ped ped, Image<Rgba32> image, string imageName)
        {
            var dff = this.assetHelper.GetDff(ped.ModelName);
            var txd = this.assetHelper.GetTxd(ped.TxdName);

            var images = this.assetHelper.GetImages(txd);
            images[AssetHelper.SanitizeName(imageName)] = image;
            var models = MeshHelper.GetModels(dff, images);

            var rotation = DetermineRotation(models);
            this.scene.Clear();
            foreach (var model in models)
                this.scene.Add(model, new Vector3D(0, 0, 0), rotation);
        }

        private Vector3D DetermineRotation(IEnumerable<GeometryModel3D> models)
        {
            var vertices = models
                .SelectMany(x => (x.Geometry as MeshGeometry3D)!.Positions);
            var highestX = vertices.Max(x => x.X);
            var highestY = vertices.Max(x => x.Y);
            var highestZ = vertices.Max(x => x.Z);

            if (highestZ > highestX)
                return new Vector3D(0, 0, 270);

            return new Vector3D(0, 90, 0);
        }

        public Image<Rgba32> GetImage(Ped ped, string texture)
        {
            var txd = this.assetHelper.GetTxd(ped.TxdName);

            var images = this.assetHelper.GetImages(txd);

            var imageName = AssetHelper.SanitizeName(texture);
            var image = images.ContainsKey(imageName) ? images[imageName] : images.Values.First();
            return image;
        }

        public void ApplyTo(Viewport3D viewport)
        {
            this.scene.ApplyTo(viewport);
        }

        public void LoadBackgroundModels()
        {
            var backgroundDff = this.assetHelper.GetDff(backgroundDffName);
            var backgroundTxd = this.assetHelper.GetTxd(backgroundTxdName);

            var images = this.assetHelper.GetImages(backgroundTxd);

            var backgroundModels = MeshHelper.GetModels(backgroundDff, images);

            foreach (var model in backgroundModels)
                this.scene.Add(model, new Vector3D(0.6, 0.2, 0.8), new Vector3D(0, 0, 90));
        }
    }
}
