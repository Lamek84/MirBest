using System.ComponentModel.DataAnnotations;

namespace AutoPartsStore.Core.Entities;

// Марка автомобиля (VW, BMW, Mercedes-Benz и т.д.). Список известных марок
// сидится в DbInitializer, дальше админ может добавлять/удалять через
// VehicleMakesController.
public class VehicleMake : BaseEntity
{
    [Required(ErrorMessage = "Name ist erforderlich.")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public ICollection<VehicleModel> Models { get; set; } = new List<VehicleModel>();
}
