namespace OrderHub.Core.Services;

public record LowStockProduct(
    int Id,
    string Sku,
    string Name,
    int StockQuantity,
    int SoldLast30Days);
