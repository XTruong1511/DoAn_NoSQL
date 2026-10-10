using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DoAnNoSQL.Models
{
    /// <summary>
    /// Model cho một item trong giỏ hàng
    /// </summary>
    [Serializable]
    public class CartItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductImage { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string Color { get; set; } // Màu sắc đã chọn

        /// <summary>
        /// Tổng giá của item (Price * Quantity)
        /// </summary>
        public decimal TotalPrice
        {
            get { return Price * Quantity; }
        }
    }

    /// <summary>
    /// Model cho giỏ hàng
    /// </summary>
    [Serializable]
    public class ShoppingCart
    {
        /// <summary>
        /// Danh sách items trong giỏ hàng
        /// </summary>
        public List<CartItem> Items { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ShoppingCart()
        {
            Items = new List<CartItem>();
        }

        /// <summary>
        /// Thêm sản phẩm vào giỏ hàng
        /// </summary>
        public void AddItem(CartItem item)
        {
            // Kiểm tra sản phẩm đã có trong giỏ chưa (cùng ID và màu)
            var existingItem = Items.FirstOrDefault(i => 
                i.ProductId == item.ProductId && 
                i.Color == item.Color);

            if (existingItem != null)
            {
                // Nếu có rồi thì tăng số lượng
                existingItem.Quantity += item.Quantity;
            }
            else
            {
                // Chưa có thì thêm mới
                Items.Add(item);
            }
        }

        /// <summary>
        /// Xóa sản phẩm khỏi giỏ hàng
        /// </summary>
        public void RemoveItem(int productId, string color = null)
        {
            if (string.IsNullOrEmpty(color))
            {
                // Xóa tất cả items có productId này
                Items.RemoveAll(i => i.ProductId == productId);
            }
            else
            {
                // Xóa item cụ thể (productId + color)
                Items.RemoveAll(i => i.ProductId == productId && i.Color == color);
            }
        }

        /// <summary>
        /// Cập nhật số lượng sản phẩm
        /// </summary>
        public void UpdateQuantity(int productId, int quantity, string color = null)
        {
            CartItem item;
            
            if (string.IsNullOrEmpty(color))
            {
                item = Items.FirstOrDefault(i => i.ProductId == productId);
            }
            else
            {
                item = Items.FirstOrDefault(i => i.ProductId == productId && i.Color == color);
            }

            if (item != null)
            {
                if (quantity <= 0)
                {
                    // Nếu quantity <= 0 thì xóa item
                    RemoveItem(productId, color);
                }
                else
                {
                    // Cập nhật số lượng
                    item.Quantity = quantity;
                }
            }
        }

        /// <summary>
        /// Lấy tổng tiền của giỏ hàng
        /// </summary>
        public decimal GetTotal()
        {
            return Items.Sum(i => i.TotalPrice);
        }

        /// <summary>
        /// Lấy tổng số lượng sản phẩm trong giỏ
        /// </summary>
        public int GetTotalQuantity()
        {
            return Items.Sum(i => i.Quantity);
        }

        /// <summary>
        /// Xóa toàn bộ giỏ hàng
        /// </summary>
        public void Clear()
        {
            Items.Clear();
        }

        /// <summary>
        /// Kiểm tra giỏ hàng có rỗng không
        /// </summary>
        public bool IsEmpty()
        {
            return Items == null || Items.Count == 0;
        }

        /// <summary>
        /// Lấy item theo productId và color
        /// </summary>
        public CartItem GetItem(int productId, string color = null)
        {
            if (string.IsNullOrEmpty(color))
            {
                return Items.FirstOrDefault(i => i.ProductId == productId);
            }
            else
            {
                return Items.FirstOrDefault(i => i.ProductId == productId && i.Color == color);
            }
        }
    }
}