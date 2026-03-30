using RPGEconomy.Application.DTOs;
using RPGEconomy.Domain.Production;

namespace RPGEconomy.Application.Services;

internal static class InventoryItemMappings
{
    public static IReadOnlyList<InventoryItemDto> Map(
        IEnumerable<InventoryItem> items,
        IReadOnlyDictionary<int, string> productNames) =>
        items
            .Select(item => new InventoryItemDto(
                item.ProductTypeId,
                ResolveProductName(productNames, item.ProductTypeId),
                item.Quantity,
                item.Quality))
            .ToList()
            .AsReadOnly();

    private static string ResolveProductName(
        IReadOnlyDictionary<int, string> productNames,
        int productTypeId) =>
        productNames.GetValueOrDefault(productTypeId, "Неизвестный товар");
}
