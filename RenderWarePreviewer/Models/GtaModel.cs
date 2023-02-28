using RenderWareIo.Structs.Ide;

namespace RenderWarePreviewer.Models;

public record GtaModel(string ModelName, string TxdName)
{
    public static GtaModel Create(Ped ped) => new(ped.ModelName, ped.TxdName);
    public static GtaModel Create(Weapon weapon) => new(weapon.ModelName, weapon.TxdName);
    public static GtaModel Create(Obj obj) => new(obj.ModelName, obj.TxdName);
    public static GtaModel Create(Car car) => new(car.ModelName, car.TxdName);
}