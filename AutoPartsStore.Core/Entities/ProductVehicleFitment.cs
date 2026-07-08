namespace AutoPartsStore.Core.Entities;

// Совместимость товара с конкретной моделью авто, опционально ограниченная
// диапазоном годов выпуска (YearFrom/YearTo — оба null означает "подходит
// для всех годов этой модели"). Основа для будущего поиска "подобрать
// деталь под мою машину" — сам поиск по маркам/моделям/году пока не строим,
// только справочник и привязку со стороны товара.
public class ProductVehicleFitment : BaseEntity
{
    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int VehicleModelId { get; set; }
    public VehicleModel? VehicleModel { get; set; }

    public int? YearFrom { get; set; }
    public int? YearTo { get; set; }
}
