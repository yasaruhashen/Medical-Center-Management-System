using System.IO;
using UMCMS.Data;
using UMCMS.Data.Repositories;
using UMCMS.Domain.Entities;
using UMCMS.Services;
using Xunit;

namespace UMCMS.Tests
{
    public class InventoryDispenseTests : IDisposable
    {
        private readonly string _dbPath;
        private readonly DatabaseService _db;
        private readonly InventoryRepository _repo;
        private readonly InventoryService _service;

        public InventoryDispenseTests()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), "umcms_test_" + Guid.NewGuid().ToString("N") + ".db");
            _db = new DatabaseService(_dbPath);
            _db.InitializeDatabase(seedDemoData: false);   // clean DB for isolated tests
            _repo = new InventoryRepository(_db);
            _service = new InventoryService(_db, _repo);
        }

        private int MakeItemWithTwoBatches()
        {
            int id = _repo.Add(new InventoryItem { ItemName = "Test Med", Stream = "General", Unit = "tablets", ReorderLevel = 10 });
            _repo.AddBatch(new InventoryBatch { InventoryItemId = id, Quantity = 50, ExpiryDate = DateTime.Today.AddMonths(2) }); // earliest
            _repo.AddBatch(new InventoryBatch { InventoryItemId = id, Quantity = 50, ExpiryDate = DateTime.Today.AddMonths(6) });
            return id;
        }

        [Fact]
        public void Dispense_DrawsFromEarliestExpiringBatchFirst()
        {
            int id = MakeItemWithTwoBatches();

            var result = _service.Dispense(id, 70);

            Assert.True(result.Success);
            Assert.Equal(30, _repo.GetById(id)!.Quantity);   // 100 - 70

            // GetBatches returns only non-empty batches: the earliest (+2mo) was fully
            // drained and is gone; the remaining 30 sits in the later (+6mo) batch — proving FEFO.
            var batches = _repo.GetBatches(id);
            Assert.Single(batches);
            Assert.Equal(30, batches[0].Quantity);
            Assert.Equal(DateTime.Today.AddMonths(6).Date, batches[0].ExpiryDate!.Value.Date);
        }

        [Fact]
        public void Dispense_RejectsMoreThanOnHand()
        {
            int id = MakeItemWithTwoBatches();
            var result = _service.Dispense(id, 1000);
            Assert.False(result.Success);
            Assert.Equal(100, _repo.GetById(id)!.Quantity);  // unchanged
        }

        [Fact]
        public void Dispense_RejectsNonPositiveQuantity()
        {
            int id = MakeItemWithTwoBatches();
            Assert.False(_service.Dispense(id, 0).Success);
            Assert.False(_service.Dispense(id, -5).Success);
        }

        [Fact]
        public void Dispense_WritesConsumptionRecord()
        {
            int id = MakeItemWithTwoBatches();
            _service.Dispense(id, 20);
            var consumed = _db.ExecuteScalar<long>(
                "SELECT COALESCE(SUM(QuantityConsumed),0) FROM ConsumptionRecords WHERE InventoryItemId=@i;",
                new Dictionary<string, object?> { ["i"] = id });
            Assert.Equal(20, consumed);
        }

        public void Dispose()
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            try { File.Delete(_dbPath); } catch { }
        }
    }
}
