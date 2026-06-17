using System.Data;
using UMCMS.Domain.Entities;

namespace UMCMS.Data.Repositories
{
    public interface IInventoryRepository
    {
        List<InventoryItem> GetByStream(string? stream = null);
        InventoryItem? GetById(int id);
        List<InventoryBatch> GetBatches(int itemId);
        List<int> GetMonthlyConsumption(int itemId);
        int Add(InventoryItem item);
        void Update(InventoryItem item);
        void AddBatch(InventoryBatch batch);
    }

    public class InventoryRepository : IInventoryRepository
    {
        private readonly DatabaseService _db;
        public InventoryRepository(DatabaseService db) => _db = db;

        internal static InventoryItem MapItem(IDataReader r) => new()
        {
            Id = Convert.ToInt32(r["Id"]),
            ItemName = r["ItemName"] as string ?? "",
            Stream = r["Stream"] as string ?? "General",
            Category = r["Category"] as string ?? "",
            Unit = r["Unit"] as string ?? "",
            UnitPrice = Convert.ToDecimal(r["UnitPrice"]),
            ReorderLevel = Convert.ToInt32(r["ReorderLevel"]),
            Quantity = Convert.ToInt32(r["Quantity"])
        };

        private static InventoryBatch MapBatch(IDataReader r) => new()
        {
            Id = Convert.ToInt32(r["Id"]),
            InventoryItemId = Convert.ToInt32(r["InventoryItemId"]),
            Quantity = Convert.ToInt32(r["Quantity"]),
            PurchasePrice = Convert.ToDecimal(r["PurchasePrice"]),
            Supplier = r["Supplier"] as string ?? "",
            ExpiryDate = r["ExpiryDate"] is string e && DateTime.TryParse(e, out var ed) ? ed : null
        };

        public List<InventoryItem> GetByStream(string? stream = null) =>
            stream is null
                ? _db.Query("SELECT * FROM InventoryItems ORDER BY ItemName;", MapItem)
                : _db.Query("SELECT * FROM InventoryItems WHERE Stream=@s ORDER BY ItemName;", MapItem,
                    new Dictionary<string, object?> { ["s"] = stream });

        public InventoryItem? GetById(int id) =>
            _db.Query("SELECT * FROM InventoryItems WHERE Id=@id;", MapItem,
                new Dictionary<string, object?> { ["id"] = id }).FirstOrDefault();

        public List<InventoryBatch> GetBatches(int itemId) =>
            _db.Query("SELECT * FROM InventoryBatches WHERE InventoryItemId=@i AND Quantity>0 ORDER BY ExpiryDate;",
                MapBatch, new Dictionary<string, object?> { ["i"] = itemId });

        public List<int> GetMonthlyConsumption(int itemId) =>
            _db.Query(
                @"SELECT SUM(QuantityConsumed) AS Q FROM ConsumptionRecords
                  WHERE InventoryItemId=@i GROUP BY Year, Month ORDER BY Year, Month;",
                r => Convert.ToInt32(r["Q"]), new Dictionary<string, object?> { ["i"] = itemId });

        public int Add(InventoryItem i) => (int)_db.ExecuteInsertReturnId(
            @"INSERT INTO InventoryItems (ItemName,Stream,Category,Unit,UnitPrice,ReorderLevel,Quantity,UpdatedAt)
              VALUES (@n,@s,@cat,@u,@pr,@rl,@q,@up);",
            new Dictionary<string, object?>
            {
                ["n"] = i.ItemName, ["s"] = i.Stream, ["cat"] = i.Category, ["u"] = i.Unit,
                ["pr"] = i.UnitPrice, ["rl"] = i.ReorderLevel, ["q"] = i.Quantity,
                ["up"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });

        public void Update(InventoryItem i) => _db.ExecuteNonQuery(
            @"UPDATE InventoryItems SET ItemName=@n,Stream=@s,Category=@cat,Unit=@u,UnitPrice=@pr,ReorderLevel=@rl,Quantity=@q,UpdatedAt=@up
              WHERE Id=@id;",
            new Dictionary<string, object?>
            {
                ["n"] = i.ItemName, ["s"] = i.Stream, ["cat"] = i.Category, ["u"] = i.Unit,
                ["pr"] = i.UnitPrice, ["rl"] = i.ReorderLevel, ["q"] = i.Quantity,
                ["up"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), ["id"] = i.Id
            });

        public void AddBatch(InventoryBatch b)
        {
            _db.ExecuteNonQuery(
                @"INSERT INTO InventoryBatches (InventoryItemId,Quantity,PurchasePrice,Supplier,ReceivedDate,ExpiryDate)
                  VALUES (@i,@q,@p,@s,@rd,@ed);",
                new Dictionary<string, object?>
                {
                    ["i"] = b.InventoryItemId, ["q"] = b.Quantity, ["p"] = b.PurchasePrice, ["s"] = b.Supplier,
                    ["rd"] = b.ReceivedDate.ToString("yyyy-MM-dd"), ["ed"] = b.ExpiryDate?.ToString("yyyy-MM-dd")
                });
            _db.ExecuteNonQuery("UPDATE InventoryItems SET Quantity=Quantity+@q WHERE Id=@i;",
                new Dictionary<string, object?> { ["q"] = b.Quantity, ["i"] = b.InventoryItemId });
        }
    }
}
