using MongoDB.Driver;
using DoAnNoSQL.Models;

namespace DoAnNoSQL
{
    public class MongoConfig
    {
        public static void RegisterIndexes()
        {
            var db = new MongoDbContext();
            
            // tb_SanPham indexes
            var spBuilder = Builders<tb_SanPham>.IndexKeys;
            
            // Compound Index for Category & HienThi
            var dmIndex = new CreateIndexModel<tb_SanPham>(
                spBuilder.Ascending(x => x.DanhMuc.Id).Ascending("CacCo.HienThi").Ascending(x => x.SoLuong)
            );
            
            // Compound Index for Brands
            var thIndex = new CreateIndexModel<tb_SanPham>(
                spBuilder.Ascending(x => x.ThuongHieu.Id).Ascending("CacCo.HienThi").Ascending(x => x.SoLuong)
            );
            
            // Index for latest products (Moi)
            var moiIndex = new CreateIndexModel<tb_SanPham>(
                spBuilder.Ascending("CacCo.HienThi").Ascending("CacCo.LaSanPhamMoi").Descending(x => x.NgayTao)
            );

            // Index for popular products (Ban chay)
            var viewIndex = new CreateIndexModel<tb_SanPham>(
                spBuilder.Ascending("CacCo.HienThi").Ascending("CacCo.LaSanPhamBanChay").Descending(x => x.LuotXem)
            );

            // Price sorting index
            var priceIndex = new CreateIndexModel<tb_SanPham>(
                spBuilder.Ascending(x => x.Gia)
            );

            try 
            {
                db.tb_SanPham.Collection.Indexes.CreateMany(new[] { dmIndex, thIndex, moiIndex, viewIndex, priceIndex });
            } 
            catch { }

            // tb_DanhMuc indexes
            var dmBuilder = Builders<tb_DanhMuc>.IndexKeys;
            try
            {
                db.tb_DanhMuc.Collection.Indexes.CreateOne(new CreateIndexModel<tb_DanhMuc>(
                    dmBuilder.Ascending(x => x.HienThi).Ascending(x => x.ThuTu)
                ));
            }
            catch { }

            // tb_TinTuc indexes
            var ttBuilder = Builders<tb_TinTuc>.IndexKeys;
            try
            {
                db.tb_TinTuc.Collection.Indexes.CreateOne(new CreateIndexModel<tb_TinTuc>(
                    ttBuilder.Ascending(x => x.HienThi).Descending(x => x.NgayTao)
                ));
            }
            catch { }
        }
    }
}
