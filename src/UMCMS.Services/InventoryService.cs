using UMCMS.Data;
using UMCMS.Data.Repositories;
using UMCMS.Domain.Entities;

namespace UMCMS.Services
{
    public record DispenseResult(bool Success, string Message, int QuantityDispensed);

    /// <summary>
    /// Inventory operations including FEFO (First-Expired-First-Out) dispensing, which
    /// deducts stock from the earliest-expiring batch first and records consumption.
    /// </summary>
    public class InventoryService
    {
        private readonly DatabaseService _db;
        private readonly IInventoryRepository _inventory;

        public InventoryService(DatabaseService db, IInventoryRepository inventory)
        {
            _db = db;
            _inventory = inventory;
        }

        public List<InventoryItem> ForStream(string stream) => _inventory.GetByStream(stream);
        public List<InventoryItem> LowStock(string? stream = null) =>
            _inventory.GetByStream(stream).Where(i => i.IsLowStock).ToList();

        /// <summary>
        /// Dispenses <paramref name="quantity"/> of an item using FEFO across its batches,
        /// updates the cached on-hand quantity, and logs monthly consumption. Atomic.
        /// </summary>
        public DispenseResult Dispense(int itemId, int quantity)
        {
            if (quantity <= 0) return new DispenseResult(false, "Quantity must be positive.", 0);

            var item = _inventory.GetById(itemId);
            if (item is null) return new DispenseResult(false, "Item not found.", 0);
            if (item.Quantity < quantity)
                return new DispenseResult(false, $"Only {item.Quantity} {item.Unit} on hand.", 0);

            _db.ExecuteInTransaction((conn, tx) =>
            {
                int remaining = quantity;

                // FEFO: earliest expiry first (NULL expiry last)
                var batches = _db.Query(
                    "SELECT Id, Quantity FROM InventoryBatches WHERE InventoryItemId=@i AND Quantity>0 ORDER BY (ExpiryDate IS NULL), ExpiryDate;",
                    r => (Id: Convert.ToInt32(r["Id"]), Qty: Convert.ToInt32(r["Quantity"])),
                    new Dictionary<string, object?> { ["i"] = itemId });

                foreach (var b in batches)
                {
                    if (remaining <= 0) break;
                    int take = Math.Min(b.Qty, remaining);
                    using var cmd = conn.CreateCommand();
                    cmd.Transaction = tx;
                    cmd.CommandText = "UPDATE InventoryBatches SET Quantity = Quantity - @t WHERE Id=@id;";
                    cmd.Parameters.AddWithValue("@t", take);
                    cmd.Parameters.AddWithValue("@id", b.Id);
                    cmd.ExecuteNonQuery();
                    remaining -= take;
                }

                // Update cached on-hand quantity
                using (var upd = conn.CreateCommand())
                {
                    upd.Transaction = tx;
                    upd.CommandText = "UPDATE InventoryItems SET Quantity = Quantity - @q, UpdatedAt=@u WHERE Id=@i;";
                    upd.Parameters.AddWithValue("@q", quantity);
                    upd.Parameters.AddWithValue("@u", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    upd.Parameters.AddWithValue("@i", itemId);
                    upd.ExecuteNonQuery();
                }

                // Record consumption (monthly aggregation handled by the forecast query).
                var nowD = DateTime.Now;
                using var cons = conn.CreateCommand();
                cons.Transaction = tx;
                cons.CommandText =
                    @"INSERT INTO ConsumptionRecords (InventoryItemId, Year, Month, QuantityConsumed)
                      VALUES (@i,@y,@m,@q);";
                cons.Parameters.AddWithValue("@i", itemId);
                cons.Parameters.AddWithValue("@y", nowD.Year);
                cons.Parameters.AddWithValue("@m", nowD.Month);
                cons.Parameters.AddWithValue("@q", quantity);
                cons.ExecuteNonQuery();
            });

            return new DispenseResult(true, $"Dispensed {quantity} {item.Unit} of {item.ItemName}.", quantity);
        }
    }
}
