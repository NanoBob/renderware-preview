using RenderWareIo;
using RenderWareIo.Structs.Dff;
using RenderWareIo.Structs.Ide;
using RenderWareIo.Structs.Txd;
using RenderWarePreviewer.Extensions;
using RenderWarePreviewer.Helpers;
using RenderWarePreviewer.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Windows.Controls;
using System.Windows.Media.Media3D;

namespace RenderWarePreviewer.Scenes;

public class SceneManager
{
    private readonly Scene scene;
    private readonly AssetHelper assetHelper;

    private const string backgroundDffName = "cj_changing_room";
    private const string backgroundTxdName = "CJ_CHANGE_ROOM";
    private IEnumerable<GeometryModel3D> backgroundModels = Array.Empty<GeometryModel3D>();

    private Vector3 cameraRotation = new(180, 180, 0);

    public bool RenderBackground { get; set; }
    public bool RotatesObjects { get; set; }
    public bool UseBinMeshPlugin { get; set; }

    private Dictionary<AdditionalTextures, Txd> additionalTxds = [];

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
        this.GtaDirectoryChanged?.Invoke(this, path);
    }

    public IEnumerable<Ped> GetDefinedPeds() => this.assetHelper.GetSkins();
    public IEnumerable<Weapon> GetDefinedWeapons() => this.assetHelper.GetWeapons();
    public IEnumerable<Obj> GetDefinedObjects() => this.assetHelper.GetObjects();
    public IEnumerable<Car> GetDefinedVehicles() => this.assetHelper.GetVehicles();

    public IEnumerable<string> GetTextures(GtaModel model)
    {
        var txd = GetTexture(model);
        var names = txd.TextureContainer.Textures
            .Select(x => x.Data.TextureName)
            .ToList();

        foreach (var additionalTxd in GetAdditionalTxds(model.AdditionalTextures))
            foreach (var image in this.assetHelper.GetImages(additionalTxd))
                names.Add(image.Key);

        return names;
    }

    public void LoadModel(GtaModel gtaModel)
    {
        var (dff, txd) = GetModelAndTexture(gtaModel);

        var images = this.assetHelper.GetImages(txd);
        if (gtaModel.AdditionalTextures != AdditionalTextures.None)
        {
            foreach (var additionalTxd in GetAdditionalTxds(gtaModel.AdditionalTextures))
                foreach (var image in this.assetHelper.GetImages(additionalTxd))
                    images.Add(image.Key, image.Value);
        }

        var models = MeshHelper.GetModels(dff, images, this.UseBinMeshPlugin);

        var rotation = DetermineRotation(models.Select(x => x.model));
        this.scene.Clear();

        LoadBackgroundModels();

        foreach (var model in models)
            this.scene.Add(model.model, model.position.ToVector3D(), model.rotation.ToVector3D() + rotation);

        this.ModelLoaded?.Invoke(this, gtaModel);
    }

    public void LoadModel(GtaModel gtaModel, Image<Rgba32> image, string imageName)
    {
        var (dff, txd) = GetModelAndTexture(gtaModel);

        var images = this.assetHelper.GetImages(txd);
        if (gtaModel.AdditionalTextures != AdditionalTextures.None)
        {
            foreach (var additionalTxd in GetAdditionalTxds(gtaModel.AdditionalTextures))
                foreach (var additionalImage in this.assetHelper.GetImages(additionalTxd))
                    images.Add(additionalImage.Key, additionalImage.Value);
        }

        images[AssetHelper.SanitizeName(imageName)] = image;
        var models = MeshHelper.GetModels(dff, images, this.UseBinMeshPlugin);

        var rotation = DetermineRotation(models.Select(x => x.model));
        this.scene.Clear();
        LoadBackgroundModels();

        foreach (var model in models)
            this.scene.Add(model.model, model.position.ToVector3D(), model.rotation.ToVector3D() + rotation);

        this.ModelLoaded?.Invoke(this, gtaModel);
    }

    private IEnumerable<Txd> GetAdditionalTxds(AdditionalTextures textures)
    {
        foreach (var value in Enum.GetValues<AdditionalTextures>())
        {
            if (textures.HasFlag(value))
            {
                if (this.additionalTxds.TryGetValue(value, out var existingTxd))
                    yield return existingTxd;
                else
                {
                    var txd = value switch
                    {
                        AdditionalTextures.Vehicle => new TxdFile(Path.Join(this.assetHelper.GtaPath, "models", "generic", "vehicle.txd")).Txd,
                        AdditionalTextures.Wheels => new TxdFile(Path.Join(this.assetHelper.GtaPath, "models", "generic", "wheels.txd")).Txd,
                        _ => null
                    };

                    if (txd != null)
                    {
                        this.additionalTxds[textures] = txd;
                        yield return txd;
                    }
                }
            }
        }
    }

    private Txd GetTexture(GtaModel model)
    {
        if (model is CustomGtaModel customModel)
            return customModel.Txd;

        return this.assetHelper.GetTxd(model.TxdName);
    }

    private (Dff, Txd) GetModelAndTexture(GtaModel model)
    {
        if (model is CustomGtaModel customModel)
            return (customModel.Dff, customModel.Txd);

        var dff = this.assetHelper.GetDff(model.ModelName);
        var txd = this.assetHelper.GetTxd(model.TxdName);

        return (dff, txd);
    }

    private Vector3D DetermineRotation(IEnumerable<GeometryModel3D> models)
    {
        if (!this.RotatesObjects)
            return new Vector3D(0, 0, 0);

        var vertices = models
            .SelectMany(x => (x.Geometry as MeshGeometry3D)!.Positions);
        var highestX = vertices.Max(x => x.X);
        var highestY = vertices.Max(x => x.Y);
        var highestZ = vertices.Max(x => x.Z);

        if (highestZ > highestX)
            return new Vector3D(0, 0, 270);

        return new Vector3D(0, 90, 0);
    }

    public Image<Rgba32> GetImage(GtaModel gtaModel, string texture)
    {
        var txd = GetTexture(gtaModel);

        var images = this.assetHelper.GetImages(txd);
        if (gtaModel.AdditionalTextures != AdditionalTextures.None)
        {
            foreach (var additionalTxd in GetAdditionalTxds(gtaModel.AdditionalTextures))
                foreach (var additionalImage in this.assetHelper.GetImages(additionalTxd))
                    images.Add(additionalImage.Key, additionalImage.Value);
        }

        var imageName = AssetHelper.SanitizeName(texture);
        var image = images.TryGetValue(imageName, out var value) ? value :
            images.Values.FirstOrDefault() ?? MeshHelper.MissingTexture;

        return image;
    }

    public byte[] GetDff(GtaModel gtaModel)
    {
        return this.assetHelper.GetRawDff(gtaModel.ModelName);
    }

    public byte[] GetTxd(GtaModel gtaModel)
    {
        return this.assetHelper.GetRawTxd(gtaModel.TxdName);
    }

    public void ApplyTo(Viewport3D viewport)
    {
        this.scene.ApplyTo(viewport);
    }

    public void RequestRender()
    {
        this.RenderRequested?.Invoke(this);
    }

    public void LoadBackgroundModels()
    {
        if (!this.RenderBackground)
            return;

        if (!this.backgroundModels.Any())
        {
            var backgroundDff = this.assetHelper.GetDff(backgroundDffName);
            var backgroundTxd = this.assetHelper.GetTxd(backgroundTxdName);

            var images = this.assetHelper.GetImages(backgroundTxd);

            this.backgroundModels = MeshHelper.GetModels(backgroundDff, images).Select(x => x.model);
        }

        foreach (var model in this.backgroundModels)
            this.scene.Add(model, new Vector3D(0.6, 0.2, 0.8), new Vector3D(0, 0, 90));
    }

    public event Action<SceneManager>? RenderRequested;
    public event Action<SceneManager, string>? GtaDirectoryChanged;
    public event Action<SceneManager, GtaModel>? ModelLoaded;
}
