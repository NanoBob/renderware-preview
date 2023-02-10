using RenderWareIo.Structs.Ide;

namespace RenderWarePreviewer.Models;

public record GtaModel(string ModelName, string TxdName)
{
    public static GtaModel Create(Ped ped) => new(ped.ModelName, ped.TxdName);
}