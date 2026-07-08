using System.ComponentModel.DataAnnotations;

namespace AutoPartsStore.Core.Entities;

// Модель в рамках марки (VW -> Golf, Passat...). Список моделей не сидится
// заранее — админ добавляет их по мере необходимости через VehicleModelsController.
public class VehicleModel : BaseEntity
{
    [Required(ErrorMessage = "Name ist erforderlich.")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Marke ist erforderlich.")]
    public int VehicleMakeId { get; set; }
    public VehicleMake? VehicleMake { get; set; }
}
