namespace AutoPartsStore.Core.Entities;

// Тип ссылочного номера детали.
//   Oem   — оригинальный номер производителя авто (то, что приходит из каталога по VIN).
//   Cross — кросс-номер (аналог/замена от других брендов).
// Хранится в БД как int (см. ProductReferenceNumberConfiguration).
public enum ReferenceNumberType
{
    Oem = 0,
    Cross = 1
}
