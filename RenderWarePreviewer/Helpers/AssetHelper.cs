using BCnEncoder.Decoder;
using BCnEncoder.ImageSharp;
using RenderWareIo;
using RenderWareIo.ReadWriteHelpers;
using RenderWareIo.Structs.Dff;
using RenderWareIo.Structs.Ide;
using RenderWareIo.Structs.Img;
using RenderWareIo.Structs.Txd;
using RenderWarePreviewer.Constants;
using RenderWarePreviewer.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RenderWarePreviewer.Helpers
{
    public class AssetHelper
    {
        private string? gtaPath;
        private ImgFile? img;
        private ImgFile? intImg;

        public AssetHelper()
        {

        }

        public void SetGtaPath(string path)
        {
            var imgPath = Path.Join(path, "models", "gta3.img");
            if (!File.Exists(imgPath))
                throw new FileNotFoundException($"No img file found at {path}");

            var intImgPath = Path.Join(path, "models", "gta_int.img");
            if (!File.Exists(intImgPath))
                throw new FileNotFoundException($"No interior img file found at {path}");

            if (this.img != null)
                this.img.Dispose();

            if (this.intImg != null)
                this.intImg.Dispose();

            this.gtaPath = path;
            this.img = new ImgFile(imgPath);
            this.intImg = new ImgFile(intImgPath);
        }

        public Dff GetDff(string name)
        {
            if (this.img == null || this.intImg == null)
                throw new FileNotFoundException("No img file found");

            var key = SanitizeName(name) + ".dff";
            if (!this.img.Img.DataEntries.ContainsKey(key) && !this.intImg.Img.DataEntries.ContainsKey(key))
                throw new FileNotFoundException($"No {name}.dff file found in gta_int and img files");

            var data = this.img.Img.DataEntries.ContainsKey(key) ? this.img.Img.DataEntries[key] : this.intImg.Img.DataEntries[key];
            var stream = GetReadStream(data);

            var dff = new Dff();
            dff.Read(stream);

            return dff;
        }

        public Txd GetTxd(string name)
        {
            if (this.img == null || this.intImg == null)
                throw new FileNotFoundException("No img file found");

            var key = SanitizeName(name) + ".txd";
            if (!this.img.Img.DataEntries.ContainsKey(key) && !this.intImg.Img.DataEntries.ContainsKey(key))
                throw new FileNotFoundException($"No {name}.txd file found in gta_int and img files");

            var data = this.img.Img.DataEntries.ContainsKey(key) ? this.img.Img.DataEntries[key] : this.intImg.Img.DataEntries[key];
            var stream = GetReadStream(data);

            var txd = new Txd();
            txd.Read(stream);

            return txd;
        }

        public Dictionary<string, Image<Rgba32>> GetImages(Txd txd)
        {
            var images = new Dictionary<string, Image<Rgba32>>();

            foreach (var texture in txd.TextureContainer.Textures)
            {
                string sanitizedName = SanitizeName(texture.Data.TextureName);
                string format = texture.Data.TextureFormatString;
                var dds = texture.GetDds(false);
                var data = DdsHelper.StripDdsHeader(dds);

                Image<Rgba32> rgbaImage = new(texture.Data.Width, texture.Data.Height);

                switch (format)
                {
                    case "RGB32":
                    case "BGRA32":
                        {
                            var image = Image.LoadPixelData<Bgra32>(data, texture.Data.Width, texture.Data.Height);
                            rgbaImage = image.CloneAs<Rgba32>();
                            image.Dispose();
                            break;
                        }
                    case "RGBA32":
                        {
                            var image = Image.LoadPixelData<Rgba32>(data, texture.Data.Width, texture.Data.Height);
                            rgbaImage = image;
                            break;
                        }
                    case "DXT1":
                        {
                            BcDecoder decoder = new();
                            Image<Rgba32> image = decoder.DecodeRawToImageRgba32(data, texture.Data.Width, texture.Data.Height, BCnEncoder.Shared.CompressionFormat.Bc1);
                            rgbaImage = image;
                            break;
                        }
                }

                images[sanitizedName] = rgbaImage;
            }

            return images;
        }

        public IEnumerable<Ped> GetSkins()
        {
            var ide = new IdeFile(Path.Join(this.gtaPath, "data", "peds.ide"));
            return ide.Ide.Peds
                .Where(x => x.Id > 0)
                .Select(ReplaceSpecials)
                .Concat(GetAdditionalSkins());
        }

        public IEnumerable<Weapon> GetWeapons()
        {
            var ide = new IdeFile(Path.Join(this.gtaPath, "data", "default.ide"));
            return ide.Ide.Weapons;
        }

        public IEnumerable<Obj> GetObjects()
        {
            var ides = Directory.GetFiles(Path.Join(this.gtaPath, "data", "maps"), "*.ide", SearchOption.AllDirectories);
            var objects = new List<Obj>();

            foreach (var file in ides)
            {
                var ide = new IdeFile(file);
                foreach (var obj in ide.Ide.Objs)
                    objects.Add(obj);
            }

            return objects;
        }

        private Ped ReplaceSpecials(Ped ped)
        {
            if (ped.ModelName.StartsWith("special"))
            {
                var index = ped.ModelName[^2..];
                if (int.TryParse(index, out var value))
                {
                    ped.ModelName = PedModelConstants.Specials[value];
                    ped.TxdName = PedModelConstants.Specials[value];
                }
            }
            return ped;
        }

        private IEnumerable<Ped> GetAdditionalSkins()
        {
            List<Ped> additionalPeds = new();

            int i = 300;
            foreach (var additional in PedModelConstants.Additionals)
                additionalPeds.Add(new Ped()
                {
                    Id = i++,
                    TxdName = additional,
                    ModelName = additional
                });

            return additionalPeds;
        }

        public static string SanitizeName(string name)
        {
            return name.Trim('\0').ToLower();
        }

        private Stream GetReadStream(DataEntry data)
        {
            var stream = new MemoryStream();
            stream.Write(data.Data);
            stream.Position = 0;
            return stream;
        }
    }
}
