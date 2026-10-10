using System;
using MongoDB.Driver;
using System.Configuration;

namespace DoAnNoSQL.Models
{
    public class MongoDbContext : IDisposable
    {
        private readonly IMongoDatabase _database;
        public IMongoDatabase MongoDatabase => _database;

        // 14 Primary Collections (Mapped to new MongoDB collections without tb_ prefix)
        public MongoDbSet<tb_SanPham> tb_SanPham { get; }
        public MongoDbSet<tb_DanhMuc> tb_DanhMuc { get; }
        public MongoDbSet<tb_ThuongHieu> tb_ThuongHieu { get; }
        public MongoDbSet<tb_DonHang> tb_DonHang { get; }
        public MongoDbSet<tb_TaiKhoan> tb_TaiKhoan { get; }
        public MongoDbSet<tb_BaoHanh> tb_BaoHanh { get; }
        public MongoDbSet<tb_TinTuc> tb_TinTuc { get; }
        public MongoDbSet<tb_QuangCao> tb_QuangCao { get; }
        public MongoDbSet<tb_Menu> tb_Menu { get; }
        public MongoDbSet<tb_LienHe> tb_LienHe { get; }
        public MongoDbSet<tb_MaGiamGia> tb_MaGiamGia { get; }
        public MongoDbSet<tb_DanhGia> tb_DanhGia { get; }
        public MongoDbSet<tb_DangKyNhanTin> tb_DangKyNhanTin { get; }
        public MongoDbSet<tb_CauHinh> tb_CauHinh { get; }

        // Secondary / Backward-compatibility Sets
        public MongoDbSet<tb_AnhSanPham> tb_AnhSanPham { get; }
        public MongoDbSet<tb_ThongSo> tb_ThongSo { get; }
        public MongoDbSet<tb_ChiTietDonHang> tb_ChiTietDonHang { get; }
        public MongoDbSet<tb_DanhMucTin> tb_DanhMucTin { get; }
        public MongoDbSet<tb_PhieuSuaChua> tb_PhieuSuaChua { get; }
        public MongoDbSet<tb_PhuongThucThanhToan> tb_PhuongThucThanhToan { get; }
        public MongoDbSet<tb_PhuongThucVanChuyen> tb_PhuongThucVanChuyen { get; }
        public MongoDbSet<tb_Quyen> tb_Quyen { get; }
        public MongoDbSet<tb_YeuThich> tb_YeuThich { get; }
        public MongoDbSet<tb_Setting> tb_Setting { get; }

        private static readonly MongoClient _client;
        private static readonly IMongoDatabase _sharedDatabase;

        static MongoDbContext()
        {
            var pack = new MongoDB.Bson.Serialization.Conventions.ConventionPack();
            pack.Add(new MongoDB.Bson.Serialization.Conventions.IgnoreExtraElementsConvention(true));
            MongoDB.Bson.Serialization.Conventions.ConventionRegistry.Register("IgnoreExtraElements", pack, t => true);

            var connectionString = ConfigurationManager.ConnectionStrings["MongoDB"].ConnectionString;
            var mongoUrl = new MongoUrl(connectionString);
            var clientSettings = MongoClientSettings.FromUrl(mongoUrl);
            clientSettings.MaxConnectionPoolSize = 200;
            clientSettings.MinConnectionPoolSize = 10;
            clientSettings.WaitQueueTimeout = TimeSpan.FromSeconds(5);
            clientSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);
            _client = new MongoClient(clientSettings);

            var databaseName = ConfigurationManager.ConnectionStrings["MongoDatabaseName"].ConnectionString;
            _sharedDatabase = _client.GetDatabase(databaseName);
        }

        public MongoDbContext()
        {
            _database = _sharedDatabase;

            // Connect to the 14 new Collections
            tb_SanPham = new MongoDbSet<tb_SanPham>(_database.GetCollection<tb_SanPham>("SanPham"));
            tb_DanhMuc = new MongoDbSet<tb_DanhMuc>(_database.GetCollection<tb_DanhMuc>("DanhMuc"));
            tb_ThuongHieu = new MongoDbSet<tb_ThuongHieu>(_database.GetCollection<tb_ThuongHieu>("ThuongHieu"));
            tb_DonHang = new MongoDbSet<tb_DonHang>(_database.GetCollection<tb_DonHang>("DonHang"));
            tb_TaiKhoan = new MongoDbSet<tb_TaiKhoan>(_database.GetCollection<tb_TaiKhoan>("TaiKhoan"));
            tb_BaoHanh = new MongoDbSet<tb_BaoHanh>(_database.GetCollection<tb_BaoHanh>("BaoHanh"));
            tb_TinTuc = new MongoDbSet<tb_TinTuc>(_database.GetCollection<tb_TinTuc>("TinTuc"));
            tb_QuangCao = new MongoDbSet<tb_QuangCao>(_database.GetCollection<tb_QuangCao>("QuangCao"));
            tb_Menu = new MongoDbSet<tb_Menu>(_database.GetCollection<tb_Menu>("Menu"));
            tb_LienHe = new MongoDbSet<tb_LienHe>(_database.GetCollection<tb_LienHe>("LienHe"));
            tb_MaGiamGia = new MongoDbSet<tb_MaGiamGia>(_database.GetCollection<tb_MaGiamGia>("MaGiamGia"));
            tb_DanhGia = new MongoDbSet<tb_DanhGia>(_database.GetCollection<tb_DanhGia>("DanhGia"));
            tb_DangKyNhanTin = new MongoDbSet<tb_DangKyNhanTin>(_database.GetCollection<tb_DangKyNhanTin>("DangKyNhanTin"));
            tb_CauHinh = new MongoDbSet<tb_CauHinh>(_database.GetCollection<tb_CauHinh>("CauHinh"));

            // Secondary sets - map to existing MongoDB collections (no tb_ prefix)
            tb_AnhSanPham = new MongoDbSet<tb_AnhSanPham>(_database.GetCollection<tb_AnhSanPham>("AnhSanPham"));
            tb_ThongSo = new MongoDbSet<tb_ThongSo>(_database.GetCollection<tb_ThongSo>("ThongSo"));
            tb_ChiTietDonHang = new MongoDbSet<tb_ChiTietDonHang>(_database.GetCollection<tb_ChiTietDonHang>("ChiTietDonHang"));
            tb_DanhMucTin = new MongoDbSet<tb_DanhMucTin>(_database.GetCollection<tb_DanhMucTin>("DanhMucTin"));
            tb_PhieuSuaChua = new MongoDbSet<tb_PhieuSuaChua>(_database.GetCollection<tb_PhieuSuaChua>("PhieuSuaChua"));
            tb_PhuongThucThanhToan = new MongoDbSet<tb_PhuongThucThanhToan>(_database.GetCollection<tb_PhuongThucThanhToan>("PhuongThucThanhToan"));
            tb_PhuongThucVanChuyen = new MongoDbSet<tb_PhuongThucVanChuyen>(_database.GetCollection<tb_PhuongThucVanChuyen>("PhuongThucVanChuyen"));
            tb_Quyen = new MongoDbSet<tb_Quyen>(_database.GetCollection<tb_Quyen>("Quyen"));
            tb_YeuThich = new MongoDbSet<tb_YeuThich>(_database.GetCollection<tb_YeuThich>("YeuThich"));
            tb_Setting = new MongoDbSet<tb_Setting>(_database.GetCollection<tb_Setting>("Setting"));
        }

        public int GetNextSequenceValue(string sequenceName)
        {
            var counters = _database.GetCollection<MongoDB.Bson.BsonDocument>("Counters");
            var filter = Builders<MongoDB.Bson.BsonDocument>.Filter.Eq("_id", sequenceName);

            var existingCounter = counters.Find(filter).FirstOrDefault();

            string colName = sequenceName.StartsWith("tb_") ? sequenceName.Substring(3) : sequenceName;
            var targetCollection = _database.GetCollection<MongoDB.Bson.BsonDocument>(colName);
            
            // Look for max SqlId first
            var maxDoc = targetCollection.Find(Builders<MongoDB.Bson.BsonDocument>.Filter.Empty)
                                         .Sort(Builders<MongoDB.Bson.BsonDocument>.Sort.Descending("SqlId"))
                                         .Limit(1)
                                         .FirstOrDefault();

            int maxId = 0;
            if (maxDoc != null && maxDoc.Contains("SqlId") && maxDoc["SqlId"].IsInt32)
            {
                maxId = maxDoc["SqlId"].AsInt32;
            }
            else
            {
                var maxDocId = targetCollection.Find(Builders<MongoDB.Bson.BsonDocument>.Filter.Empty)
                                               .Sort(Builders<MongoDB.Bson.BsonDocument>.Sort.Descending("_id"))
                                               .Limit(1)
                                               .FirstOrDefault();
                if (maxDocId != null && maxDocId.Contains("_id") && maxDocId["_id"].IsInt32)
                {
                    maxId = maxDocId["_id"].AsInt32;
                }
            }

            if (existingCounter == null)
            {
                try
                {
                    counters.InsertOne(new MongoDB.Bson.BsonDocument { { "_id", sequenceName }, { "seq", maxId } });
                }
                catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
                {
                    // Ignore, another thread might have initialized it
                }
            }
            else
            {
                int currentSeq = existingCounter["seq"].AsInt32;
                if (currentSeq < maxId)
                {
                    var updateMax = Builders<MongoDB.Bson.BsonDocument>.Update.Set("seq", maxId);
                    counters.UpdateOne(filter, updateMax);
                }
            }

            var update = Builders<MongoDB.Bson.BsonDocument>.Update.Inc("seq", 1);
            var options = new FindOneAndUpdateOptions<MongoDB.Bson.BsonDocument>
            {
                ReturnDocument = ReturnDocument.After,
                IsUpsert = true
            };
            var result = counters.FindOneAndUpdate(filter, update, options);
            return result["seq"].AsInt32;
        }

        public void SaveChanges() { }
        public void Dispose() { }
        public DummyConfig Configuration = new DummyConfig();
        public class DummyConfig { 
            public bool ValidateOnSaveEnabled { get; set; } 
            public bool ProxyCreationEnabled { get; set; }
        }
        public DummyDatabase Database = new DummyDatabase();
        public class DummyDatabase { 
            public DummyTransaction BeginTransaction() { return new DummyTransaction(); } 
            public int ExecuteSqlCommand(string sql, params object[] parameters) { return 0; } 
            public System.Collections.Generic.List<T> SqlQuery<T>(string sql, params object[] parameters) { return new System.Collections.Generic.List<T>(); }
        }
        public class DummyTransaction : IDisposable { 
            public void Commit() { } 
            public void Rollback() { } 
            public void Dispose() { } 
        }
    }

    public class MongoDbSet<T> : System.Collections.Generic.IEnumerable<T> where T : class
    {
        private readonly IMongoCollection<T> _collection;

        public IMongoCollection<T> Collection => _collection;

        public MongoDbSet(IMongoCollection<T> collection)
        {
            _collection = collection;
        }

        public void Add(T entity)
        {
            _collection.InsertOne(entity);
        }
        
        public void InsertOne(T entity)
        {
            _collection.InsertOne(entity);
        }

        public void Remove(T entity)
        {
            var idProperty = typeof(T).GetProperty("Id") ?? typeof(T).GetProperty("id");
            if (idProperty != null)
            {
                var idValue = idProperty.GetValue(entity);
                var filter = Builders<T>.Filter.Eq("SqlId", idValue) | Builders<T>.Filter.Eq("Id", idValue);
                _collection.DeleteOne(filter);
            }
        }

        public T Find(params object[] keyValues)
        {
            if (keyValues != null && keyValues.Length > 0)
            {
                var idValue = keyValues[0];
                var filter = Builders<T>.Filter.Eq("SqlId", idValue) | Builders<T>.Filter.Eq("Id", idValue);
                return _collection.Find(filter).FirstOrDefault();
            }
            return null;
        }

        public System.Collections.Generic.IEnumerator<T> GetEnumerator() => _collection.Find(Builders<T>.Filter.Empty).ToCursor().ToEnumerable().GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public static class MongoDbSetExtensions
    {
        public static System.Linq.IQueryable<T> Include<T>(this System.Linq.IQueryable<T> source, string path) where T : class
        {
            return source;
        }
        
        public static System.Linq.IQueryable<T> Include<T, TProperty>(this System.Linq.IQueryable<T> source, System.Linq.Expressions.Expression<Func<T, TProperty>> path) where T : class
        {
            return source;
        }
    }
}

public static class MongoExtensions
{
    public static void RemoveRange<T>(this DoAnNoSQL.Models.MongoDbSet<T> collection, System.Collections.Generic.IEnumerable<T> entities) where T : class
    {
        if (entities != null)
        {
            foreach (var entity in System.Linq.Enumerable.ToList(entities))
            {
                collection.Remove(entity);
            }
        }
    }
    public static void AddRange<T>(this DoAnNoSQL.Models.MongoDbSet<T> collection, System.Collections.Generic.IEnumerable<T> entities) where T : class
    {
        if (entities != null)
        {
            foreach (var entity in entities)
            {
                collection.Add(entity);
            }
        }
    }
    public static System.Linq.IQueryable<T> AsNoTracking<T>(this System.Linq.IQueryable<T> query)
    {
        return query;
    }
}

namespace System.Data.Entity.Validation
{
    public class DbEntityValidationException : Exception
    {
        public System.Collections.Generic.IEnumerable<DbEntityValidationResult> EntityValidationErrors { get; set; } = new System.Collections.Generic.List<DbEntityValidationResult>();
    }
    public class DbEntityValidationResult
    {
        public System.Collections.Generic.IEnumerable<DbValidationError> ValidationErrors { get; set; } = new System.Collections.Generic.List<DbValidationError>();
    }
    public class DbValidationError
    {
        public string ErrorMessage { get; set; }
        public string PropertyName { get; set; }
    }
}

namespace System.Data.Entity
{
    public enum EntityState
    {
        Detached = 1,
        Unchanged = 2,
        Added = 4,
        Deleted = 8,
        Modified = 16
    }
}
