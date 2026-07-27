# 低庫存警示頁面實作計畫

本計畫依目前可見需求與既有程式慣例整理，實作前採用以下假設：

- 低庫存定義為 `StockQuantity < threshold`，不包含剛好等於 threshold 的商品。
- 只顯示仍在販售的商品。
- 結果依 `StockQuantity` 升冪排序；相同庫存量再依 SKU 排序，確保順序穩定。
- `StockQuantity < 5` 的表格列套用 Bootstrap `table-danger` 警示色。
- 「售出」包含 Pending、Confirmed、Shipped，只排除 Cancelled。
- 近 30 天包含剛好第 30 天，即 `CreatedAt >= DateTime.UtcNow.AddDays(-30)`。

若完整規格與上述假設不同，實作前應先調整。

## 參考慣例

動手前已閱讀：

- `src/OrderHub.Web/Controllers/ProductsController.cs`
- `src/OrderHub.Core/Services/ProductService.cs`
- `src/OrderHub.Core/Services/IProductService.cs`
- `src/OrderHub.Web/Views/Products/Index.cshtml`

後續沿用既有寫法：Controller 建立 Web ViewModel、ProductService 負責商業規則並轉接 repository、View 使用 Razor 與 Bootstrap 表格呈現。

## 檔案規劃

### 新增

#### `src/OrderHub.Core/Services/LowStockProduct.cs`

- Core 層的低庫存查詢結果型別。
- 包含商品 ID、SKU、名稱、目前庫存、近 30 天售出數量。
- 避免 repository 回傳 Web ViewModel，也不把報表欄位加入 `Product` domain entity。

#### `src/OrderHub.Web/ViewModels/LowStockViewModel.cs`

- 定義頁面 ViewModel 與表格 Row ViewModel。
- `Threshold` 預設為 10。
- 使用 `[Range(1, int.MaxValue)]` 驗證 threshold。
- 包含 `IReadOnlyList<LowStockRowViewModel>`，讓 View 不直接綁 Core 或 domain 型別。

#### `src/OrderHub.Web/Views/Products/LowStock.cshtml`

- 提供 GET 查詢表單。
- 顯示 threshold 驗證訊息與低庫存結果表格。
- 沿用 `Views/Products/Index.cshtml` 的 Bootstrap 表格與 Razor 寫法。
- 使用 `asp-for`、`asp-validation-for` 顯示輸入與錯誤。
- 對 `StockQuantity < 5` 的商品列套用 `table-danger`；這是純顯示規則，保留在 View。

### 修改

#### `src/OrderHub.Core/Interfaces/IProductRepository.cs`

- 新增依 threshold 與銷售起始日期查詢低庫存摘要的方法。
- 預計方法：

```csharp
Task<IReadOnlyList<LowStockProduct>> GetLowStockAsync(
    int threshold,
    DateTime soldSinceUtc);
```

#### `src/OrderHub.Infrastructure/Repositories/ProductRepository.cs`

- 篩選 `IsActive` 且 `StockQuantity < threshold` 的商品。
- 聚合近 30 天且非 Cancelled 訂單的品項數量。
- 依 `StockQuantity` 升冪排序，相同庫存量再依 SKU 排序。
- 這是唯一新增 EF Core 查詢的地方。
- 使用 `AsNoTracking()`，並以一次 `ToListAsync()` 執行單一 SQL。

#### `src/OrderHub.Core/Services/IProductService.cs`

- 新增低庫存查詢 service contract。
- 預計回傳：

```csharp
Task<ServiceResult<IReadOnlyList<LowStockProduct>>> GetLowStockAsync(
    int threshold);
```

- 使用 `ServiceResult<T>` 表達預期的 threshold 驗證失敗。

#### `src/OrderHub.Core/Services/ProductService.cs`

- 驗證 threshold 必須大於 0。
- 在 Core 定義「近 30 天」商業規則並計算 UTC 起始時間。
- 呼叫 repository，不接觸 EF Core。
- 不在 Controller 或 View 重算售出數量。

#### `src/OrderHub.Web/Controllers/ProductsController.cs`

- 新增 `LowStock(LowStockViewModel vm)` GET action。
- `ModelState` 無效時直接回傳 View，不呼叫 service。
- 輸入有效時呼叫 ProductService。
- 將 Core 結果映射為 Web Row ViewModel。
- Service 失敗時把錯誤加入 `ModelState`，不形成 500。

#### `src/OrderHub.Web/Views/Shared/_Layout.cshtml`

- 在商品導覽附近新增「低庫存警示」連結。
- 連到 `Products/LowStock`。

#### `tests/OrderHub.Tests/ProductServiceTests.cs`

- 增加三個低庫存 service 測試。
- 沿用目前 `TestSetup.CreateProductService` 與 EF Core InMemory 寫法。
- 不新增 NuGet 套件。

### 不需修改

- `src/OrderHub.Web/Program.cs`：既有 `IProductService` 與 `IProductRepository` 註冊可直接沿用。
- `src/OrderHub.Infrastructure/Data/OrderHubDbContext.cs`：現有關聯及索引足以支援查詢。
- `src/OrderHub.Infrastructure/Migrations/**`：不需要資料庫結構變更。
- `src/OrderHub.Web/appsettings.json`：不涉及設定變更。

## 分層職責

### Web Controller

- 接收 `LowStockViewModel`。
- 檢查 `ModelState`。
- 呼叫 ProductService。
- 將 Core 查詢結果映射為 Web Row ViewModel。
- 不包含 threshold 商業規則、日期計算或 EF Core 查詢。

### Core Service

- 防禦性驗證 threshold。
- 定義近 30 天時間範圍。
- 呼叫 repository。
- 以 `ServiceResult<T>` 表達預期失敗。

### Repository

- 唯一接觸 `DbContext` 的位置。
- 篩選低庫存與販售狀態。
- 篩選訂單日期及狀態。
- 聚合各商品售出數量。

### ViewModel

- 承接表單輸入。
- 使用 DataAnnotations 驗證 threshold。
- 提供 View 所需的輸出欄位。

### View

- 綁定 Web ViewModel。
- 顯示表單、驗證錯誤及結果表格。
- 對庫存量小於 5 的列套用 `table-danger` 警示色。
- 不綁 domain model，也不執行商業計算。

## 近 30 天售出數量

### 時間規則

由 Core service 計算：

```csharp
var soldSinceUtc = DateTime.UtcNow.AddDays(-30);
```

Repository 接收已算好的起始時間，避免 Infrastructure 自行定義「30 天」商業規則。

### EF Core 查詢

Repository 預計使用單次投影查詢：

```csharp
await _db.Products
    .AsNoTracking()
    .Where(p => p.IsActive && p.StockQuantity < threshold)
    .Select(p => new LowStockProduct
    {
        Id = p.Id,
        Sku = p.Sku,
        Name = p.Name,
        StockQuantity = p.StockQuantity,
        SoldLast30Days = _db.OrderItems
            .Where(i =>
                i.ProductId == p.Id &&
                i.Order!.CreatedAt >= soldSinceUtc &&
                i.Order.Status != OrderStatus.Cancelled)
            .Sum(i => (int?)i.Quantity) ?? 0
    })
    .OrderBy(p => p.StockQuantity)
    .ThenBy(p => p.Sku)
    .ToListAsync();
```

EF Core 會把相關聚合轉譯為單一 SQL。查詢不會先載入商品，再逐項查詢 OrderItems，因此沒有 N+1。沒有近期銷售資料時，以 nullable `Sum` 加 `?? 0` 回傳 0。

## Threshold 驗證

Web ViewModel 預計定義：

```csharp
[Range(1, int.MaxValue, ErrorMessage = "庫存門檻必須大於 0")]
public int Threshold { get; set; } = 10;
```

預期行為：

- 未帶 `threshold`：保留屬性預設值 10。
- `threshold=0` 或負數：DataAnnotations 讓 `ModelState` 無效。
- 非數字：MVC model binding 自動加入 `ModelState` 錯誤。
- Controller 回傳同一個 View 顯示錯誤，不呼叫 repository，也不產生 500。
- Service 再做 `threshold <= 0` 防禦性檢查，讓非 MVC 呼叫者同樣得到 `ServiceResult` 失敗，而不是例外。

## Service 測試

### 1. `GetLowStockAsync_ReturnsActiveProductsBelowThresholdInStockOrder`

- 建立庫存為 3、9、10、11 的商品。
- 另建一個庫存為 2、但已停售的商品。
- threshold 設為 10。
- 驗證只回傳庫存為 3、9 且仍販售的商品；庫存剛好等於 10 的商品不應出現。
- 驗證結果依庫存量升冪排列。
- 同時驗證沒有訂單時，近 30 天售出數量為 0。

### 2. `GetLowStockAsync_SumsRecentNonCancelledOrderItems`

- 建立近 30 天的 Pending、Confirmed、Shipped、Cancelled 訂單。
- 另建超過 30 天的非 Cancelled 訂單。
- 驗證只加總近 30 天且非 Cancelled 的品項數量。

### 3. `GetLowStockAsync_NonPositiveThreshold_ReturnsFailure`

- 傳入 0 或負數。
- 驗證 service 回傳失敗的 `ServiceResult`。
- 驗證不丟例外，避免錯誤輸入演變為 500。

## 建議實作順序

1. 新增 Core 查詢結果型別及 service/repository contracts。
2. 在 ProductRepository 完成單一 SQL 的低庫存彙總查詢。
3. 在 ProductService 加入 threshold 防禦性驗證與 30 天規則。
4. 新增 ViewModel、Controller action 與 Razor View。
5. 加入導覽連結。
6. 補三個 service 測試。
7. 執行 `dotnet build` 與 `dotnet test`。
