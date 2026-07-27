using OrderHub.Core.Domain;

namespace OrderHub.Tests;

public class ProductServiceTests
{
    [Fact]
    public async Task GetAll_ReturnsAllProductsIncludingInactive()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, sku: "SKU-A001");
        TestSetup.AddProduct(db, sku: "SKU-A002", isActive: false);

        var products = await service.GetAllAsync();

        Assert.Equal(2, products.Count);
    }

    [Fact]
    public async Task GetActive_ExcludesInactiveProducts()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, sku: "SKU-A001");
        TestSetup.AddProduct(db, sku: "SKU-A002", isActive: false);

        var products = await service.GetActiveAsync();

        Assert.All(products, p => Assert.True(p.IsActive));
        Assert.Single(products);
    }

    [Fact]
    public async Task GetLowStock_ReturnsActiveProductsBelowThresholdInStockOrder()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, stock: 3, sku: "SKU-A003");
        TestSetup.AddProduct(db, stock: 9, sku: "SKU-A009");
        TestSetup.AddProduct(db, stock: 10, sku: "SKU-A010");
        TestSetup.AddProduct(db, stock: 11, sku: "SKU-A011");
        TestSetup.AddProduct(db, stock: 2, isActive: false, sku: "SKU-I002");

        var result = await service.GetLowStockAsync(10);

        Assert.True(result.Success);
        var products = result.Value!;
        Assert.Equal(new[] { "SKU-A003", "SKU-A009" }, products.Select(p => p.Sku));
        Assert.All(products, p => Assert.Equal(0, p.SoldLast30Days));
    }

    [Fact]
    public async Task GetLowStock_SumsRecentNonCancelledOrderItems()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        var customer = TestSetup.AddCustomer(db);
        var product = TestSetup.AddProduct(db, stock: 3, sku: "SKU-LOW");
        var now = DateTime.UtcNow;

        db.Orders.AddRange(
            CreateOrder(customer.Id, product.Id, OrderStatus.Pending, now.AddDays(-1), 2),
            CreateOrder(customer.Id, product.Id, OrderStatus.Confirmed, now.AddDays(-2), 3),
            CreateOrder(customer.Id, product.Id, OrderStatus.Shipped, now.AddDays(-3), 4),
            CreateOrder(customer.Id, product.Id, OrderStatus.Cancelled, now.AddDays(-4), 5),
            CreateOrder(customer.Id, product.Id, OrderStatus.Shipped, now.AddDays(-31), 6));
        await db.SaveChangesAsync();

        var result = await service.GetLowStockAsync(10);

        Assert.True(result.Success);
        Assert.Equal(9, result.Value!.Single().SoldLast30Days);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetLowStock_NonPositiveThreshold_ReturnsFailure(int threshold)
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);

        var result = await service.GetLowStockAsync(threshold);

        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
    }

    private static Order CreateOrder(int customerId, int productId, OrderStatus status, DateTime createdAt, int quantity) =>
        new()
        {
            CustomerId = customerId,
            Status = status,
            CreatedAt = createdAt,
            Items =
            {
                new OrderItem
                {
                    ProductId = productId,
                    Quantity = quantity,
                    UnitPriceSnapshot = 100m
                }
            }
        };
}
