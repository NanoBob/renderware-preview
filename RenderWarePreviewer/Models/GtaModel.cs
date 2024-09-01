using RenderWareIo.Structs.Dff;
using RenderWareIo.Structs.Ide;
using RenderWareIo.Structs.Txd;
using System.IO;

namespace RenderWarePreviewer.Models;

public enum AdditionalTextures
{
    None = 0x00,
    Vehicle = 0x01,
    Wheels = 0x02,

}

public class GtaModel(string modelName, string txdName, AdditionalTextures additionalTextures = AdditionalTextures.None)
{
    public string ModelName { get; } = modelName;
    public string TxdName { get; } = txdName;
    public AdditionalTextures AdditionalTextures { get; } = additionalTextures;

    public static GtaModel Create(Ped ped) => new(ped.ModelName, ped.TxdName);
    public static GtaModel Create(Weapon weapon) => new(weapon.ModelName, weapon.TxdName);
    public static GtaModel Create(Obj obj) => new(obj.ModelName, obj.TxdName);
    public static GtaModel Create(Car car) => new(car.ModelName, car.TxdName, AdditionalTextures.Vehicle | AdditionalTextures.Wheels);
}

public class CustomGtaModel : GtaModel
{
    public Dff Dff { get; }
    public Txd Txd { get; }

    public CustomGtaModel(byte[] dff, byte[] txd) : base("custom", "custom")
    {
        using var dffStream = new MemoryStream();
        dffStream.Write(dff);
        dffStream.Position = 0;

        this.Dff = new Dff().Read(dffStream);

        using var txdStream = new MemoryStream();
        txdStream.Write(txd);
        txdStream.Position = 0;

        this.Txd = new Txd().Read(txdStream);        
    }

    public static CustomGtaModel Create(byte[] dff, byte[] txd) => new(dff, txd);
}