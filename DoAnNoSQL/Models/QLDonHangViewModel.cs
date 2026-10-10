using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DoAnNoSQL.Models
{
    public class QLDonHangViewModel : tb_DonHang
    {
        public new string PhuongThucThanhToan { get; set; }
        public new string PhuongThucVanChuyen { get; set; }
        [MongoDB.Bson.Serialization.Attributes.BsonIgnore]
        public new string MaGiamGia { get; set; }
        [MongoDB.Bson.Serialization.Attributes.BsonIgnore]
        public new string SoDienThoai { get; set; }
        [MongoDB.Bson.Serialization.Attributes.BsonIgnore]
        public new int? SoLuongSanPham { get; set; }
    }
}