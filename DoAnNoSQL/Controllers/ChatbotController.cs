using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using DoAnNoSQL.Models;

namespace DoAnNoSQL.Controllers
{
    /// <summary>
    /// Controller xử lý chatbot
    /// </summary>
    public class ChatbotController : BaseController
    {
        /// <summary>
        /// POST: Xử lý tin nhắn từ user
        /// </summary>
        [HttpPost]
        public JsonResult ProcessMessage(string message)
        {
            try
            {
                // Debug logging
                System.Diagnostics.Debug.WriteLine($"[Chatbot] Received message: {message}");

                if (string.IsNullOrWhiteSpace(message))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Vui lòng nhập tin nhắn"
                    });
                }

                // Normalize message
                string normalizedMessage = message.ToLower().Trim();
                System.Diagnostics.Debug.WriteLine($"[Chatbot] Normalized message: {normalizedMessage}");

                // Xử lý các intent khác nhau
                var response = ProcessIntent(normalizedMessage);

                System.Diagnostics.Debug.WriteLine($"[Chatbot] Response: {response.Message}");
                System.Diagnostics.Debug.WriteLine($"[Chatbot] Products count: {response.Products?.Count ?? 0}");
                System.Diagnostics.Debug.WriteLine($"[Chatbot] Suggestions count: {response.Suggestions?.Count ?? 0}");

                return Json(new
                {
                    success = true,
                    message = response.Message,
                    products = response.Products,
                    suggestions = response.Suggestions,
                    redirectUrl = response.RedirectUrl,
                    redirectText = response.RedirectText,
                    categoryLinks = response.CategoryLinks,
                    brandLinks = response.BrandLinks
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Chatbot] ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[Chatbot] Stack trace: {ex.StackTrace}");

                return Json(new
                {
                    success = false,
                    message = "Có lỗi xảy ra: " + ex.Message
                });
            }
        }

        /// <summary>
        /// Xử lý intent của user - NÂNG CAO
        /// </summary>
        private ChatbotResponse ProcessIntent(string message)
        {
            var response = new ChatbotResponse();

            System.Diagnostics.Debug.WriteLine($"[ProcessIntent] Processing: {message}");

            // Intent 0: Category or Brand navigation
            // "Danh mục điện thoại", "Thương hiệu Samsung", "Xem danh mục"
            var categoryOrBrandResponse = CheckCategoryOrBrandIntent(message);
            if (categoryOrBrandResponse != null)
            {
                return categoryOrBrandResponse;
            }

            // Intent 1: Multi-criteria search (kết hợp nhiều điều kiện)
            // "Xiaomi 15 dưới 20 triệu pin khủng"
            if (IsMultiCriteriaSearch(message))
            {
                return ProcessMultiCriteriaSearch(message);
            }

            // Intent 2: Product comparison
            // "So sánh iPhone 15 và Samsung S24"
            if (ContainsKeywords(message, new[] { "so sánh", "khác nhau", "giống", "tốt hơn" }))
            {
                return CompareProducts(message);
            }

            // Intent 3: Specific product with filters
            // "Xiaomi 15 giá rẻ nhất"
            if (ContainsKeywords(message, new[] { "iphone", "samsung", "xiaomi", "oppo", "vivo", "realme" }))
            {
                return SearchProductByBrand(message);
            }

            // Intent 4: Price-based search
            // "điện thoại dưới 10 triệu"
            if (ContainsKeywords(message, new[] { "giá", "bao nhiêu", "tiền", "rẻ", "mức", "dưới", "trên", "từ" }))
            {
                return SearchProductByPrice(message);
            }

            // Intent 5: Spec-based search
            // "điện thoại pin khủng camera tốt"
            if (ContainsKeywords(message, new[] { "pin", "camera", "màn hình", "ram", "bộ nhớ", "chip", "sạc" }))
            {
                return SearchProductBySpec(message);
            }

            // Intent 6: Trending products
            // "Sản phẩm hot nhất"
            if (ContainsKeywords(message, new[] { "hot", "mới", "bán chạy", "phổ biến", "nổi bật", "trend" }))
            {
                return GetTrendingProducts(message);
            }

            // Intent 7: Recommendations
            // "Gợi ý cho tôi", "Nên mua gì"
            if (ContainsKeywords(message, new[] { "gợi ý", "đề xuất", "nên mua", "giúp chọn", "tư vấn" }))
            {
                return GetRecommendations(message);
            }

            // Intent 8: Help/Greeting
            if (ContainsKeywords(message, new[] { "chào", "hello", "hi", "hỗ trợ", "giúp", "hướng dẫn" }))
            {
                return GetGreeting();
            }

            // Default: Smart fallback
            return GetDefaultResponse();
        }

        /// <summary>
        /// Kiểm tra xem có phải tìm kiếm đa tiêu chí không
        /// </summary>
        private bool IsMultiCriteriaSearch(string message)
        {
            int criteriaCount = 0;

            // Check brand
            if (ContainsKeywords(message, new[] { "iphone", "samsung", "xiaomi", "oppo", "vivo" }))
                criteriaCount++;

            // Check price
            if (ContainsKeywords(message, new[] { "dưới", "trên", "từ", "giá", "triệu" }))
                criteriaCount++;

            // Check specs
            if (ContainsKeywords(message, new[] { "pin", "camera", "ram", "màn hình", "chip" }))
                criteriaCount++;

            return criteriaCount >= 2;
        }

        /// <summary>
        /// Tìm kiếm đa tiêu chí (Multi-criteria search)
        /// </summary>
        private ChatbotResponse ProcessMultiCriteriaSearch(string message)
        {
            System.Diagnostics.Debug.WriteLine("[MultiCriteria] Processing multi-criteria search");

            var response = new ChatbotResponse();
            var query = GetAllProducts().Where(sp => sp.HienThi == true);

            // Criteria 1: Specific product/brand
            string specificProduct = ExtractSpecificProductName(message);
            if (!string.IsNullOrEmpty(specificProduct))
            {
                query = query.Where(sp => sp.TenSanPham.Contains(specificProduct));
                System.Diagnostics.Debug.WriteLine($"[MultiCriteria] Filter by product: {specificProduct}");
            }
            else
            {
                // Fallback to brand
                string brand = "";
                if (message.Contains("iphone")) brand = "iPhone";
                else if (message.Contains("samsung")) brand = "Samsung";
                else if (message.Contains("xiaomi")) brand = "Xiaomi";
                else if (message.Contains("oppo")) brand = "OPPO";

                if (!string.IsNullOrEmpty(brand))
                {
                    query = query.Where(sp => sp.TenSanPham.Contains(brand) || 
                                              sp.tb_ThuongHieu.TenThuongHieu.Contains(brand));
                    System.Diagnostics.Debug.WriteLine($"[MultiCriteria] Filter by brand: {brand}");
                }
            }

            // Criteria 2: Price range
            decimal minPrice = 0, maxPrice = 0;
            if (message.Contains("dưới") || message.Contains("duoi"))
            {
                if (message.Contains("5")) maxPrice = 5000000;
                else if (message.Contains("10")) maxPrice = 10000000;
                else if (message.Contains("15")) maxPrice = 15000000;
                else if (message.Contains("20")) maxPrice = 20000000;
                else if (message.Contains("30")) maxPrice = 30000000;
            }
            else if (message.Contains("từ") || message.Contains("tu"))
            {
                if (message.Contains("5") && message.Contains("10"))
                {
                    minPrice = 5000000;
                    maxPrice = 10000000;
                }
                else if (message.Contains("10") && message.Contains("15"))
                {
                    minPrice = 10000000;
                    maxPrice = 15000000;
                }
            }
            else if (message.Contains("trên") || message.Contains("tren"))
            {
                if (message.Contains("20")) minPrice = 20000000;
                else if (message.Contains("30")) minPrice = 30000000;
            }

            if (minPrice > 0)
            {
                query = query.Where(sp => (sp.GiaKhuyenMai ?? sp.Gia) >= minPrice);
                System.Diagnostics.Debug.WriteLine($"[MultiCriteria] Filter by min price: {minPrice}");
            }
            if (maxPrice > 0)
            {
                query = query.Where(sp => (sp.GiaKhuyenMai ?? sp.Gia) <= maxPrice);
                System.Diagnostics.Debug.WriteLine($"[MultiCriteria] Filter by max price: {maxPrice}");
            }

            // Criteria 3: Sort by requirement
            if (message.Contains("rẻ nhất") || message.Contains("re nhat"))
            {
                query = query.OrderBy(sp => sp.GiaKhuyenMai ?? sp.Gia);
                System.Diagnostics.Debug.WriteLine("[MultiCriteria] Sort by: cheapest");
            }
            else if (message.Contains("mắc nhất") || message.Contains("mac nhat") || message.Contains("đắt nhất"))
            {
                query = query.OrderByDescending(sp => sp.GiaKhuyenMai ?? sp.Gia);
                System.Diagnostics.Debug.WriteLine("[MultiCriteria] Sort by: most expensive");
            }
            else if (message.Contains("mới nhất") || message.Contains("moi nhat"))
            {
                query = query.OrderByDescending(sp => sp.NgayTao);
                System.Diagnostics.Debug.WriteLine("[MultiCriteria] Sort by: newest");
            }
            else if (message.Contains("bán chạy") || message.Contains("ban chay") || message.Contains("phổ biến"))
            {
                query = query.OrderByDescending(sp => sp.LuotXem);
                System.Diagnostics.Debug.WriteLine("[MultiCriteria] Sort by: best-selling");
            }
            else
            {
                query = query.OrderByDescending(sp => sp.LuotXem);
            }

            // Execute query
            var products = query
                .Take(5)
                .Select(sp => new ProductChatbotViewModel
                {
                    Id = sp.Id,
                    TenSanPham = sp.TenSanPham,
                    Gia = sp.GiaKhuyenMai ?? sp.Gia ?? 0,
                    GiaGoc = sp.Gia,
                    HinhAnh = sp.HinhAnh,
                    ThuongHieu = sp.tb_ThuongHieu.TenThuongHieu
                })
                .ToList();

            System.Diagnostics.Debug.WriteLine($"[MultiCriteria] Found {products.Count} products");

            if (products.Any())
            {
                response.Message = $"Tôi tìm thấy {products.Count} sản phẩm phù hợp với yêu cầu của bạn:";
                response.Products = products;
                response.Suggestions = new List<string>
                {
                    "Sản phẩm rẻ hơn",
                    "Sản phẩm tốt hơn",
                    "So sánh chi tiết",
                    "Xem thêm"
                };
            }
            else
            {
                response.Message = "Không tìm thấy sản phẩm phù hợp. Bạn có thể thử với điều kiện khác.";
                response.Suggestions = new List<string>
                {
                    "Sản phẩm bán chạy",
                    "Sản phẩm mới",
                    "Mở rộng tìm kiếm"
                };
            }

            return response;
        }

        /// <summary>
        /// So sánh sản phẩm (Product Comparison)
        /// </summary>
        private ChatbotResponse CompareProducts(string message)
        {
            System.Diagnostics.Debug.WriteLine("[Compare] Processing comparison request");

            var response = new ChatbotResponse();

            // Extract product names to compare
            // Pattern: "so sánh A và B", "A với B", "A hay B"
            var products = ExtractProductsForComparison(message);

            if (products.Count >= 2)
            {
                response.Message = $"Đây là so sánh giữa {products[0].TenSanPham} và {products[1].TenSanPham}:\n\n" +
                                   $"👉 {products[0].TenSanPham}\n" +
                                   $"👉 Giá: {FormatPrice(products[0].Gia)}đ\n" +
                                   $"👉 Thương hiệu: {products[0].ThuongHieu}\n\n" +
                                   $"👉 {products[1].TenSanPham}\n" +
                                   $"👉 Giá: {FormatPrice(products[1].Gia)}đ\n" +
                                   $"👉 Thương hiệu: {products[1].ThuongHieu}\n\n" +
                                   $"👉 Kết luận: {GetComparisonConclusion(products[0], products[1])}";

                response.Products = products;
                response.Suggestions = new List<string>
                {
                    "Xem chi tiết cấu hình",
                    "Sản phẩm tương tự",
                    "Thêm vào giỏ hàng"
                };
            }
            else
            {
                response.Message = "Vui lòng cho tôi biết 2 sản phẩm bạn muốn so sánh.\n" +
                                   "Ví dụ: 'So sánh iPhone 15 và Samsung S24'";
                response.Suggestions = new List<string>
                {
                    "iPhone 15 vs Samsung S24",
                    "Xiaomi 15 vs OPPO Find X7",
                    "Sản phẩm phổ biến"
                };
            }

            return response;
        }

        /// <summary>
        /// Gợi ý sản phẩm dựa trên ngữ cảnh
        /// </summary>
        private ChatbotResponse GetRecommendations(string message)
        {
            System.Diagnostics.Debug.WriteLine("[Recommendations] Processing recommendation request");

            var response = new ChatbotResponse();

            // Detect user preferences from message
            bool wantsCheap = message.Contains("rẻ") || message.Contains("tiết kiệm") || message.Contains("sinh viên");
            bool wantsGaming = message.Contains("game") || message.Contains("chơi game");
            bool wantsCamera = message.Contains("camera") || message.Contains("chụp ảnh") || message.Contains("sống ảo");
            bool wantsBusiness = message.Contains("văn phòng") || message.Contains("công việc") || message.Contains("doanh nhân");

            var query = GetAllProducts().Where(sp => sp.HienThi == true);

            if (wantsCheap)
            {
                query = query.Where(sp => (sp.GiaKhuyenMai ?? sp.Gia) <= 10000000)
                             .OrderBy(sp => sp.GiaKhuyenMai ?? sp.Gia);
                response.Message = "Dựa trên nhu cầu tiết kiệm của bạn, tôi gợi ý:";
            }
            else if (wantsGaming)
            {
                query = query.OrderByDescending(sp => sp.Gia);
                response.Message = "Cho nhu cầu chơi game, tôi gợi ý các sản phẩm cấu hình cao:";
            }
            else if (wantsCamera)
            {
                query = query.OrderByDescending(sp => sp.LuotXem);
                response.Message = "Cho nhu cầu chụp ảnh, tôi gợi ý:";
            }
            else if (wantsBusiness)
            {
                query = query.Where(sp => (sp.GiaKhuyenMai ?? sp.Gia) >= 15000000)
                             .OrderByDescending(sp => sp.Gia);
                response.Message = "Cho công việc văn phòng, tôi gợi ý:";
            }
            else
            {
                query = query.OrderByDescending(sp => sp.LuotXem);
                response.Message = "Dựa trên xu hướng hiện tại, tôi gợi ý:";
            }

            var products = query
                .Take(5)
                .Select(sp => new ProductChatbotViewModel
                {
                    Id = sp.Id,
                    TenSanPham = sp.TenSanPham,
                    Gia = sp.GiaKhuyenMai ?? sp.Gia ?? 0,
                    GiaGoc = sp.Gia,
                    HinhAnh = sp.HinhAnh,
                    ThuongHieu = sp.tb_ThuongHieu.TenThuongHieu
                })
                .ToList();

            response.Products = products;
            response.Suggestions = new List<string>
            {
                "Gợi ý khác",
                "Xem chi tiết",
                "So sánh"
            };

            return response;
        }

        /// <summary>
        /// Trích xuất sản phẩm để so sánh
        /// </summary>
        private List<ProductChatbotViewModel> ExtractProductsForComparison(string message)
        {
            var products = new List<ProductChatbotViewModel>();

            // Try to extract 2 product names
            var parts = message.Split(new[] { " và ", " với ", " hay ", " vs ", " hoặc " }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts.Take(2))
            {
                string productName = ExtractSpecificProductName(part);
                if (!string.IsNullOrEmpty(productName))
                {
                    var product = GetAllProducts()
                        .Where(sp => sp.HienThi == true && sp.TenSanPham.Contains(productName))
                        .OrderByDescending(sp => sp.LuotXem)
                        .Select(sp => new ProductChatbotViewModel
                        {
                            Id = sp.Id,
                            TenSanPham = sp.TenSanPham,
                            Gia = sp.GiaKhuyenMai ?? sp.Gia ?? 0,
                            GiaGoc = sp.Gia,
                            HinhAnh = sp.HinhAnh,
                            ThuongHieu = sp.tb_ThuongHieu.TenThuongHieu
                        })
                        .FirstOrDefault();

                    if (product != null)
                        products.Add(product);
                }
            }

            return products;
        }

        /// <summary>
        /// Kết luận so sánh
        /// </summary>
        private string GetComparisonConclusion(ProductChatbotViewModel p1, ProductChatbotViewModel p2)
        {
            if (p1.Gia < p2.Gia)
                return $"{p1.TenSanPham} rẻ hơn, phù hợp nếu bạn cần tiết kiệm.";
            else if (p1.Gia > p2.Gia)
                return $"{p2.TenSanPham} rẻ hơn, phù hợp nếu bạn cần tiết kiệm.";
            else
                return "Hai máy có giá tương đương, hãy xem cấu hình để chọn phù hợp.";
        }

        /// <summary>
        /// Format giá
        /// </summary>
        private string FormatPrice(decimal price)
        {
            return string.Format("{0:0,0}", price);
        }

        /// <summary>
        /// Tìm sản phẩm theo thương hiệu hoặc tên sản phẩm cụ thể - ADVANCED SEARCH
        /// </summary>
        private ChatbotResponse SearchProductByBrand(string message)
        {
            var response = new ChatbotResponse();

            System.Diagnostics.Debug.WriteLine($"[SearchByBrand] Original message: {message}");

            // Step 1: Try exact match first (full message) - Case-insensitive
            var exactMatchProducts = GetAllProducts()
                .Where(sp => sp.HienThi == true)
                .ToList()
                .Where(sp => sp.TenSanPham.ToLower().Contains(message.ToLower()))
                .OrderByDescending(sp => sp.LuotXem)
                .Take(5)
                .Select(sp => new ProductChatbotViewModel
                {
                    Id = sp.Id,
                    TenSanPham = sp.TenSanPham,
                    Gia = sp.GiaKhuyenMai ?? sp.Gia ?? 0,
                    GiaGoc = sp.Gia,
                    HinhAnh = sp.HinhAnh,
                    ThuongHieu = sp.tb_ThuongHieu.TenThuongHieu
                })
                .ToList();

            if (exactMatchProducts.Any())
            {
                System.Diagnostics.Debug.WriteLine($"[SearchByBrand] Exact match found: {exactMatchProducts.Count}");
                response.Message = $"Tôi tìm thấy {exactMatchProducts.Count} sản phẩm '{message}':";
                response.Products = exactMatchProducts;
                response.Suggestions = new List<string>
                {
                    "Sản phẩm tương tự",
                    "Giá tốt nhất",
                    "Xem thêm"
                };
                return response;
            }

            // Step 2: Try regex pattern matching for specific products
            string specificProduct = ExtractSpecificProductName(message);
            
            System.Diagnostics.Debug.WriteLine($"[SearchByBrand] Specific product: {specificProduct}");

            if (!string.IsNullOrEmpty(specificProduct))
            {
                // Get all products and filter in-memory for exact/specific matching
                var allProducts = GetAllProducts()
                    .Where(sp => sp.HienThi == true)
                    .ToList();

                // Priority 1: Exact match (full product name)
                var exactProducts = allProducts
                    .Where(sp => sp.TenSanPham.Equals(specificProduct, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(sp => sp.LuotXem)
                    .Take(5)
                    .Select(sp => new ProductChatbotViewModel
                    {
                        Id = sp.Id,
                        TenSanPham = sp.TenSanPham,
                        Gia = sp.GiaKhuyenMai ?? sp.Gia ?? 0,
                        GiaGoc = sp.Gia,
                        HinhAnh = sp.HinhAnh,
                        ThuongHieu = sp.tb_ThuongHieu.TenThuongHieu
                    })
                    .ToList();

                if (exactProducts.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"[SearchByBrand] Exact product match found: {exactProducts.Count}");
                    response.Message = $"Tôi tìm thấy {exactProducts.Count} sản phẩm {specificProduct}:";
                    response.Products = exactProducts;
                    response.Suggestions = new List<string>
                    {
                        $"{specificProduct} giá tốt",
                        "Sản phẩm tương tự",
                        "Xem thêm"
                    };
                    return response;
                }

                // Priority 2: Contains but must match all words (strict matching)
                var searchWords = specificProduct.ToLower().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var strictProducts = allProducts
                    .Where(sp => searchWords.All(word => sp.TenSanPham.ToLower().Contains(word)))
                    .OrderByDescending(sp => sp.LuotXem)
                    .Take(5)
                    .Select(sp => new ProductChatbotViewModel
                    {
                        Id = sp.Id,
                        TenSanPham = sp.TenSanPham,
                        Gia = sp.GiaKhuyenMai ?? sp.Gia ?? 0,
                        GiaGoc = sp.Gia,
                        HinhAnh = sp.HinhAnh,
                        ThuongHieu = sp.tb_ThuongHieu.TenThuongHieu
                    })
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"[SearchByBrand] Strict match found: {strictProducts.Count}");

                if (strictProducts.Any())
                {
                    response.Message = $"Tôi tìm thấy {strictProducts.Count} sản phẩm {specificProduct}:";
                    response.Products = strictProducts;
                    response.Suggestions = new List<string>
                    {
                        $"{specificProduct} giá tốt",
                        "Sản phẩm tương tự",
                        "Xem thêm"
                    };
                    return response;
                }
            }

            // Step 3: Advanced fuzzy search with relevance ranking
            var allProductsForRanking = GetAllProducts()
                .Where(sp => sp.HienThi == true)
                .ToList();

            System.Diagnostics.Debug.WriteLine($"[SearchByBrand] Total products in DB: {allProductsForRanking.Count}");

            // Split search terms
            var searchTerms = message.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            System.Diagnostics.Debug.WriteLine($"[SearchByBrand] Search terms: {string.Join(", ", searchTerms)}");

            // Calculate relevance score for each product
            var rankedProducts = allProductsForRanking
                .Select(sp => new
                {
                    Product = sp,
                    Score = CalculateRelevanceScore(sp.TenSanPham, searchTerms)
                })
                .Where(x => x.Score > 0) // Only products with some relevance
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.Product.LuotXem)
                .Take(5)
                .Select(x => new ProductChatbotViewModel
                {
                    Id = x.Product.Id,
                    TenSanPham = x.Product.TenSanPham,
                    Gia = x.Product.GiaKhuyenMai ?? x.Product.Gia ?? 0,
                    GiaGoc = x.Product.Gia,
                    HinhAnh = x.Product.HinhAnh,
                    ThuongHieu = x.Product.tb_ThuongHieu.TenThuongHieu
                })
                .ToList();

            System.Diagnostics.Debug.WriteLine($"[SearchByBrand] Fuzzy search found: {rankedProducts.Count}");

            if (rankedProducts.Any())
            {
                response.Message = $"Tôi tìm thấy {rankedProducts.Count} sản phẩm phù hợp với '{message}':";
                response.Products = rankedProducts;
                response.Suggestions = new List<string>
                {
                    "Tìm kiếm khác",
                    "Sản phẩm bán chạy",
                    "Xem tất cả"
                };
            }
            else
            {
                // Step 4: Fallback to brand search
                string brand = "";
                if (message.Contains("iphone") || message.Contains("ip")) brand = "iPhone";
                else if (message.Contains("samsung") || message.Contains("ss")) brand = "Samsung";
                else if (message.Contains("xiaomi") || message.Contains("mi")) brand = "Xiaomi";
                else if (message.Contains("oppo")) brand = "OPPO";
                else if (message.Contains("vivo")) brand = "Vivo";
                else if (message.Contains("realme")) brand = "Realme";

                if (!string.IsNullOrEmpty(brand))
                {
                    response.Message = $"Không tìm thấy '{message}'. Đây là sản phẩm {brand}:";
                    response.Products = GetAllProducts()
                        .Where(sp => sp.HienThi == true && 
                               (sp.TenSanPham.Contains(brand) || 
                                sp.tb_ThuongHieu.TenThuongHieu.Contains(brand)))
                        .OrderByDescending(sp => sp.LuotXem)
                        .Take(5)
                        .Select(sp => new ProductChatbotViewModel
                        {
                            Id = sp.Id,
                            TenSanPham = sp.TenSanPham,
                            Gia = sp.GiaKhuyenMai ?? sp.Gia ?? 0,
                            GiaGoc = sp.Gia,
                            HinhAnh = sp.HinhAnh,
                            ThuongHieu = sp.tb_ThuongHieu.TenThuongHieu
                        })
                        .ToList();
                    response.Suggestions = new List<string> { $"{brand} mới", $"{brand} giá rẻ", "Xem thêm" };
                }
                else
                {
                    response.Message = $"Không tìm thấy sản phẩm '{message}'. Đây là sản phẩm phổ biến:";
                    response.Products = GetAllProducts()
                        .Where(sp => sp.HienThi == true)
                        .OrderByDescending(sp => sp.LuotXem)
                        .Take(5)
                        .Select(sp => new ProductChatbotViewModel
                        {
                            Id = sp.Id,
                            TenSanPham = sp.TenSanPham,
                            Gia = sp.GiaKhuyenMai ?? sp.Gia ?? 0,
                            GiaGoc = sp.Gia,
                            HinhAnh = sp.HinhAnh,
                            ThuongHieu = sp.tb_ThuongHieu.TenThuongHieu
                        })
                        .ToList();
                    response.Suggestions = new List<string> { "iPhone", "Samsung", "Xiaomi" };
                }
            }

            return response;
        }

        /// <summary>
        /// Tính điểm relevance giữa product name và search terms
        /// </summary>
        private int CalculateRelevanceScore(string productName, string[] searchTerms)
        {
            if (string.IsNullOrEmpty(productName))
                return 0;

            int score = 0;
            string normalizedProduct = productName.ToLower();
            string searchQuery = string.Join(" ", searchTerms).ToLower();

            // HIGHEST PRIORITY: Exact full match
            if (normalizedProduct == searchQuery)
            {
                score += 1000;
                System.Diagnostics.Debug.WriteLine($"[Relevance] '{productName}' - EXACT MATCH: {score}");
                return score;
            }

            // HIGH PRIORITY: All search terms present as exact words
            int exactWordMatches = 0;
            foreach (var term in searchTerms)
            {
                string normalizedTerm = term.ToLower();
                
                // Check if term exists as a separate word (word boundary)
                if (System.Text.RegularExpressions.Regex.IsMatch(normalizedProduct, 
                    @"\b" + System.Text.RegularExpressions.Regex.Escape(normalizedTerm) + @"\b"))
                {
                    exactWordMatches++;
                    score += 50;
                }
            }

            // Bonus if ALL terms matched as words
            if (exactWordMatches == searchTerms.Length)
            {
                score += 100; // Strong bonus for matching all words
                System.Diagnostics.Debug.WriteLine($"[Relevance] '{productName}' - ALL WORDS MATCH: {score}");
                return score;
            }

            // MEDIUM PRIORITY: Partial matching
            foreach (var term in searchTerms)
            {
                string normalizedTerm = term.ToLower();

                // Starts with term: +30 points
                if (normalizedProduct.StartsWith(normalizedTerm))
                {
                    score += 30;
                }
                // Contains term (exact word boundary): +20 points
                else if (normalizedProduct.Contains(" " + normalizedTerm + " ") ||
                         normalizedProduct.StartsWith(normalizedTerm + " ") ||
                         normalizedProduct.EndsWith(" " + normalizedTerm))
                {
                    score += 20;
                }
                // Contains term (substring): +5 points (reduced from 10)
                else if (normalizedProduct.Contains(normalizedTerm))
                {
                    score += 5;
                }
            }

            System.Diagnostics.Debug.WriteLine($"[Relevance] '{productName}' score: {score}");

            return score;
        }

        /// <summary>
        /// Tính khoảng cách Levenshtein (edit distance)
        /// </summary>
        private int LevenshteinDistance(string source, string target)
        {
            if (string.IsNullOrEmpty(source))
                return string.IsNullOrEmpty(target) ? 0 : target.Length;

            if (string.IsNullOrEmpty(target))
                return source.Length;

            int n = source.Length;
            int m = target.Length;
            int[,] d = new int[n + 1, m + 1];

            for (int i = 0; i <= n; d[i, 0] = i++) { }
            for (int j = 0; j <= m; d[0, j] = j++) { }

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (target[j - 1] == source[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return d[n, m];
        }

        /// <summary>
        /// Trích xuất tên sản phẩm cụ thể từ message
        /// </summary>
        private string ExtractSpecificProductName(string message)
        {
            // Pattern: Brand + Model (e.g., "Xiaomi 15", "iPhone Air", "Samsung S24")
            var patterns = new[]
            {
                // Xiaomi series
                @"xiaomi\s+(\d+\s*(ultra|pro|plus|t)?)",
                @"redmi\s+(note\s+)?\d+\s*(pro\+?|ultra|plus)?",
                @"poco\s+[xmf]\d+\s*(pro|ultra)?",
                
                // iPhone series - UPDATED with more specific patterns
                @"iphone\s+\d+\s*(pro\s*max|pro|plus)",  // iPhone 15 Pro Max, iPhone 15 Pro, iPhone 15 Plus
                @"iphone\s+(air|se|mini|max)",            // iPhone Air, iPhone SE, iPhone Mini, iPhone Max
                @"iphone\s+\d+",                         // iPhone 15, iPhone 14, etc.
                @"iphone\s+[a-z]+\s+\d+",                // iPhone Pro 15, etc.
                
                // Samsung series
                @"samsung\s+(galaxy\s+)?s\d+\s*(ultra|plus)?",
                @"samsung\s+(galaxy\s+)?a\d+\s*",
                @"samsung\s+(galaxy\s+)?m\d+\s*",
                @"samsung\s+(galaxy\s+)?z\s*(flip|fold)\s*\d*",
                
                // OPPO series
                @"oppo\s+(find\s+)?[xn]\d+\s*(pro|ultra)?",
                @"oppo\s+a\d+\s*",
                @"oppo\s+reno\d+\s*(pro)?",
                
                // Vivo series
                @"vivo\s+[vy]\d+\s*(pro)?",
                @"vivo\s+x\d+\s*(pro)?",
                
                // Realme series
                @"realme\s+\d+\s*(pro|plus)?",
                @"realme\s+gt\s*\d*\s*(pro)?",
                
                // Nokia series
                @"nokia\s+\d+(\.\d+)?\s*",
                
                // Pad/Tablet series
                @"(xiaomi|samsung|oppo)\s+pad\s+\d+\s*(pro|ultra)?"
            };

            foreach (var pattern in patterns)
            {
                var match = System.Text.RegularExpressions.Regex.Match(
                    message, 
                    pattern, 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );

                if (match.Success)
                {
                    // Return matched product name
                    string productName = match.Value.Trim();
                    
                    // Normalize spacing
                    productName = System.Text.RegularExpressions.Regex.Replace(productName, @"\s+", " ");
                    
                    // Capitalize properly (e.g., "iphone air" -> "iPhone Air")
                    productName = CapitalizeProductName(productName);
                    
                    System.Diagnostics.Debug.WriteLine($"[ExtractProduct] Matched pattern: {pattern}");
                    System.Diagnostics.Debug.WriteLine($"[ExtractProduct] Extracted: {productName}");
                    
                    return productName;
                }
            }

            return null;
        }

        /// <summary>
        /// Capitalize tên sản phẩm đúng định dạng
        /// </summary>
        private string CapitalizeProductName(string productName)
        {
            if (string.IsNullOrEmpty(productName))
                return productName;

            var parts = productName.Split(' ');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length > 0)
                {
                    // Special handling for known brands
                    string lower = parts[i].ToLower();
                    if (lower == "iphone")
                        parts[i] = "iPhone";
                    else if (lower == "xiaomi")
                        parts[i] = "Xiaomi";
                    else if (lower == "samsung")
                        parts[i] = "Samsung";
                    else if (lower == "oppo")
                        parts[i] = "OPPO";
                    else if (lower == "vivo")
                        parts[i] = "Vivo";
                    else if (lower == "realme")
                        parts[i] = "Realme";
                    else if (lower == "redmi")
                        parts[i] = "Redmi";
                    else if (lower == "poco")
                        parts[i] = "Poco";
                    else
                        // Capitalize first letter
                        parts[i] = char.ToUpper(parts[i][0]) + parts[i].Substring(1).ToLower();
                }
            }
            return string.Join(" ", parts);
        }

        /// <summary>
        /// Tìm sản phẩm theo giá
        /// </summary>
        private ChatbotResponse SearchProductByPrice(string message)
        {
            var response = new ChatbotResponse();

            // Detect price range
            decimal minPrice = 0;
            decimal maxPrice = 0;

            if (message.Contains("dưới") || message.Contains("duoi"))
            {
                // Dưới X triệu
                if (message.Contains("5")) maxPrice = 5000000;
                else if (message.Contains("10")) maxPrice = 10000000;
                else if (message.Contains("15")) maxPrice = 15000000;
                else if (message.Contains("20")) maxPrice = 20000000;
            }
            else if (message.Contains("từ") || message.Contains("tu") || message.Contains("khoảng") || message.Contains("khoang"))
            {
                // Từ X đến Y triệu
                if (message.Contains("5") && message.Contains("10"))
                {
                    minPrice = 5000000;
                    maxPrice = 10000000;
                }
                else if (message.Contains("10") && message.Contains("15"))
                {
                    minPrice = 10000000;
                    maxPrice = 15000000;
                }
                else if (message.Contains("15") && message.Contains("20"))
                {
                    minPrice = 15000000;
                    maxPrice = 20000000;
                }
            }
            else if (message.Contains("trên") || message.Contains("tren"))
            {
                // Trên X triệu
                if (message.Contains("20")) minPrice = 20000000;
                else if (message.Contains("30")) minPrice = 30000000;
            }

            if (maxPrice > 0 || minPrice > 0)
            {
                var query = GetAllProducts().Where(sp => sp.HienThi == true);

                if (minPrice > 0)
                    query = query.Where(sp => (sp.GiaKhuyenMai ?? sp.Gia) >= minPrice);

                if (maxPrice > 0)
                    query = query.Where(sp => (sp.GiaKhuyenMai ?? sp.Gia) <= maxPrice);

                var products = query
                    .OrderBy(sp => sp.GiaKhuyenMai ?? sp.Gia)
                    .Take(5)
                    .Select(sp => new ProductChatbotViewModel
                    {
                        Id = sp.Id,
                        TenSanPham = sp.TenSanPham,
                        Gia = sp.GiaKhuyenMai ?? sp.Gia ?? 0,
                        GiaGoc = sp.Gia,
                        HinhAnh = sp.HinhAnh,
                        ThuongHieu = sp.tb_ThuongHieu.TenThuongHieu
                    })
                    .ToList();

                if (products.Any())
                {
                    string priceRange = "";
                    if (minPrice > 0 && maxPrice > 0)
                        priceRange = $"từ {minPrice / 1000000}tr đến {maxPrice / 1000000}tr";
                    else if (maxPrice > 0)
                        priceRange = $"dưới {maxPrice / 1000000}tr";
                    else if (minPrice > 0)
                        priceRange = $"trên {minPrice / 1000000}tr";

                    response.Message = $"Tôi tìm thấy {products.Count} sản phẩm {priceRange}:";
                    response.Products = products;
                    response.Suggestions = new List<string>
                    {
                        "Sản phẩm dưới 5 triệu",
                        "Sản phẩm từ 10-15 triệu",
                        "Sản phẩm trên 20 triệu"
                    };
                }
                else
                {
                    response.Message = "Không tìm thấy sản phẩm phù hợp. Bạn có thể thử mức giá khác.";
                    response.Suggestions = new List<string> { "Tất cả sản phẩm", "Sản phẩm bán chạy" };
                }
            }
            else
            {
                response.Message = "Bạn có thể cho tôi biết mức giá cụ thể hơn không? Ví dụ: 'dưới 10 triệu' hoặc 'từ 10-15 triệu'";
                response.Suggestions = new List<string>
                {
                    "Dưới 5 triệu",
                    "Từ 10-15 triệu",
                    "Trên 20 triệu"
                };
            }

            return response;
        }

        /// <summary>
        /// Tìm sản phẩm theo thông số
        /// </summary>
        private ChatbotResponse SearchProductBySpec(string message)
        {
            var response = new ChatbotResponse();

            var products = new List<ProductChatbotViewModel>();

            if (message.Contains("pin") && (message.Contains("khủng") || message.Contains("lớn") || message.Contains("lon")))
            {
                // Sản phẩm pin khủng (>= 5000mAh)
                products = GetAllProducts()
                    .Where(sp => sp.HienThi == true)
                    .ToList() // Execute query first
                    .Where(sp => sp.tb_ThongSo.Any(ts => ts.TenThongSo.Contains("pin") && 
                                 (ts.GiaTri.Contains("5000") || ts.GiaTri.Contains("6000"))))
                    .Take(5)
                    .Select(sp => new ProductChatbotViewModel
                    {
                        Id = sp.Id,
                        TenSanPham = sp.TenSanPham,
                        Gia = sp.GiaKhuyenMai ?? sp.Gia ?? 0,
                        GiaGoc = sp.Gia,
                        HinhAnh = sp.HinhAnh,
                        ThuongHieu = sp.tb_ThuongHieu.TenThuongHieu
                    })
                    .ToList();

                response.Message = "Đây là các sản phẩm có pin khủng (>= 5000mAh):";
            }
            else if (message.Contains("camera"))
            {
                // Sản phẩm camera tốt
                products = GetAllProducts()
                    .Where(sp => sp.HienThi == true)
                    .OrderByDescending(sp => sp.LuotXem)
                    .Take(5)
                    .Select(sp => new ProductChatbotViewModel
                    {
                        Id = sp.Id,
                        TenSanPham = sp.TenSanPham,
                        Gia = sp.GiaKhuyenMai ?? sp.Gia ?? 0,
                        GiaGoc = sp.Gia,
                        HinhAnh = sp.HinhAnh,
                        ThuongHieu = sp.tb_ThuongHieu.TenThuongHieu
                    })
                    .ToList();

                response.Message = "Đây là các sản phẩm có camera tốt:";
            }
            else if (message.Contains("ram") || message.Contains("bộ nhớ") || message.Contains("bo nho"))
            {
                // Sản phẩm RAM lớn
                products = GetAllProducts()
                    .Where(sp => sp.HienThi == true)
                    .ToList()
                    .Where(sp => sp.tb_ThongSo.Any(ts => ts.TenThongSo.Contains("RAM") && 
                                 (ts.GiaTri.Contains("8") || ts.GiaTri.Contains("12") || ts.GiaTri.Contains("16"))))
                    .Take(5)
                    .Select(sp => new ProductChatbotViewModel
                    {
                        Id = sp.Id,
                        TenSanPham = sp.TenSanPham,
                        Gia = sp.GiaKhuyenMai ?? sp.Gia ?? 0,
                        GiaGoc = sp.Gia,
                        HinhAnh = sp.HinhAnh,
                        ThuongHieu = sp.tb_ThuongHieu.TenThuongHieu
                    })
                    .ToList();

                response.Message = "Đây là các sản phẩm có RAM lớn (>= 8GB):";
            }

            if (products.Any())
            {
                response.Products = products;
                response.Suggestions = new List<string>
                {
                    "Sản phẩm pin khủng",
                    "Sản phẩm camera tốt",
                    "Sản phẩm RAM lớn"
                };
            }
            else
            {
                response.Message = "Bạn muốn tìm sản phẩm với thông số như thế nào? (Pin khủng, Camera tốt, RAM lớn...)";
                response.Suggestions = new List<string> { "Pin khủng", "Camera tốt", "RAM lớn" };
            }

            return response;
        }

        /// <summary>
        /// Lấy sản phẩm trending
        /// </summary>
        private ChatbotResponse GetTrendingProducts(string message)
        {
            var response = new ChatbotResponse();

            var products = new List<ProductChatbotViewModel>();

            if (message.Contains("mới") || message.Contains("moi"))
            {
                // Sản phẩm mới nhất
                products = GetAllProducts()
                    .Where(sp => sp.HienThi == true && sp.LaSanPhamMoi == true)
                    .OrderByDescending(sp => sp.NgayTao)
                    .Take(5)
                    .Select(sp => new ProductChatbotViewModel
                    {
                        Id = sp.Id,
                        TenSanPham = sp.TenSanPham,
                        Gia = sp.GiaKhuyenMai ?? sp.Gia ?? 0,
                        GiaGoc = sp.Gia,
                        HinhAnh = sp.HinhAnh,
                        ThuongHieu = sp.tb_ThuongHieu.TenThuongHieu
                    })
                    .ToList();

                response.Message = "Đây là các sản phẩm mới nhất:";
            }
            else if (message.Contains("bán chạy") || message.Contains("ban chay") || message.Contains("phổ biến") || message.Contains("pho bien"))
            {
                // Sản phẩm bán chạy
                products = GetAllProducts()
                    .Where(sp => sp.HienThi == true && sp.LaSanPhamBanChay == true)
                    .OrderByDescending(sp => sp.LuotXem)
                    .Take(5)
                    .Select(sp => new ProductChatbotViewModel
                    {
                        Id = sp.Id,
                        TenSanPham = sp.TenSanPham,
                        Gia = sp.GiaKhuyenMai ?? sp.Gia ?? 0,
                        GiaGoc = sp.Gia,
                        HinhAnh = sp.HinhAnh,
                        ThuongHieu = sp.tb_ThuongHieu.TenThuongHieu
                    })
                    .ToList();

                response.Message = "Đây là các sản phẩm bán chạy nhất:";
            }
            else if (message.Contains("hot") || message.Contains("nổi bật") || message.Contains("noi bat"))
            {
                // Sản phẩm hot
                products = GetAllProducts()
                    .Where(sp => sp.HienThi == true && sp.LaSanPhamHot == true)
                    .OrderByDescending(sp => sp.LuotXem)
                    .Take(5)
                    .Select(sp => new ProductChatbotViewModel
                    {
                        Id = sp.Id,
                        TenSanPham = sp.TenSanPham,
                        Gia = sp.GiaKhuyenMai ?? sp.Gia ?? 0,
                        GiaGoc = sp.Gia,
                        HinhAnh = sp.HinhAnh,
                        ThuongHieu = sp.tb_ThuongHieu.TenThuongHieu
                    })
                    .ToList();

                response.Message = "Đây là các sản phẩm hot nhất:";
            }

            if (products.Any())
            {
                response.Products = products;
                response.Suggestions = new List<string>
                {
                    "Sản phẩm mới nhất",
                    "Sản phẩm bán chạy",
                    "Sản phẩm hot"
                };
            }

            return response;
        }

        /// <summary>
        /// Lời chào
        /// </summary>
        private ChatbotResponse GetGreeting()
        {
            return new ChatbotResponse
            {
                Message = "Xin chào! Tôi là trợ lý ảo của TechShop. Tôi có thể giúp bạn tìm kiếm sản phẩm điện thoại. Bạn muốn tìm gì?",
                Suggestions = new List<string>
                {
                    "Sản phẩm iPhone",
                    "Sản phẩm dưới 10 triệu",
                    "Sản phẩm pin khủng",
                    "Sản phẩm mới nhất"
                }
            };
        }

        /// <summary>
        /// Response mặc định
        /// </summary>
        private ChatbotResponse GetDefaultResponse()
        {
            System.Diagnostics.Debug.WriteLine("[Chatbot] Returning default response");

            // Show some products anyway
            var products = GetAllProducts()
                .Where(sp => sp.HienThi == true)
                .OrderByDescending(sp => sp.LuotXem)
                .Take(5)
                .Select(sp => new ProductChatbotViewModel
                {
                    Id = sp.Id,
                    TenSanPham = sp.TenSanPham,
                    Gia = sp.GiaKhuyenMai ?? sp.Gia ?? 0,
                    GiaGoc = sp.Gia,
                    HinhAnh = sp.HinhAnh,
                    ThuongHieu = sp.tb_ThuongHieu.TenThuongHieu
                })
                .ToList();

            return new ChatbotResponse
            {
                Message = "Xin lỗi, tôi chưa hiểu rõ câu hỏi của bạn. Đây là các sản phẩm nổi bật:\n\n" +
                          "Bạn có thể hỏi tôi:\n" +
                          "👉 Tìm sản phẩm theo thương hiệu: 'iPhone', 'Samsung'...\n" +
                          "👉 Tìm theo giá: 'dưới 10 triệu', 'từ 10-15 triệu'...\n" +
                          "👉 Tìm theo tính năng: 'pin khủng', 'camera tốt'...\n" +
                          "👉 Sản phẩm: 'hot', 'mới nhất', 'bán chạy'...",
                Products = products,
                Suggestions = new List<string>
                {
                    "iPhone",
                    "Dưới 10 triệu",
                    "Pin khủng",
                    "Sản phẩm mới"
                }
            };
        }

        /// <summary>
        /// Kiểm tra message có chứa keywords
        /// </summary>
        private bool ContainsKeywords(string message, string[] keywords)
        {
            return keywords.Any(k => message.Contains(k));
        }

        /// <summary>
        /// Kiểm tra xem user có muốn xem danh mục hoặc thương hiệu không
        /// </summary>
        private ChatbotResponse CheckCategoryOrBrandIntent(string message)
        {
            System.Diagnostics.Debug.WriteLine($"[CategoryBrand] Checking intent for: {message}");

            // Check if user wants to see categories
            if (ContainsKeywords(message, new[] { "danh mục", "danh sách", "loại", "category", "xem danh mục" }))
            {
                return GetCategoryList();
            }

            // Check if user wants to see brands
            if (ContainsKeywords(message, new[] { "thương hiệu", "hãng", "brand", "xem thương hiệu", "xem hãng" }) &&
                !ContainsKeywords(message, new[] { "sản phẩm", "điện thoại", "giá", "dưới", "trên" }))
            {
                return GetBrandList();
            }

            // Check for specific category by name
            var category = FindCategoryByName(message);
            if (category != null)
            {
                return GetCategoryDetails(category);
            }

            // Check for specific brand by name
            var brand = FindBrandByName(message);
            if (brand != null)
            {
                return GetBrandDetails(brand);
            }

            return null;
        }

        /// <summary>
        /// Tìm danh mục theo tên
        /// </summary>
        private tb_DanhMuc FindCategoryByName(string message)
        {
            var categories = db.tb_DanhMuc.Collection.Find(Builders<tb_DanhMuc>.Filter.Eq(dm => dm.HienThi, true)).ToList();
            
            foreach (var category in categories)
            {
                string categoryName = category.TenDanhMuc.ToLower();
                string categoryAlias = (category.BiDanh ?? "").ToLower();
                
                // Check exact match or contains
                if (message.Contains(categoryName) || 
                    (!string.IsNullOrEmpty(categoryAlias) && message.Contains(categoryAlias)))
                {
                    System.Diagnostics.Debug.WriteLine($"[CategoryBrand] Found category: {category.TenDanhMuc}");
                    return category;
                }
            }

            return null;
        }

        /// <summary>
        /// Tìm thương hiệu theo tên
        /// </summary>
        private tb_ThuongHieu FindBrandByName(string message)
        {
            var brands = db.tb_ThuongHieu.Collection.Find(Builders<tb_ThuongHieu>.Filter.Eq(th => th.HienThi, true)).ToList();
            
            // Common brand mappings
            var brandMappings = new Dictionary<string, string>
            {
                { "iphone", "Apple" },
                { "apple", "Apple" },
                { "ip", "Apple" },
                { "samsung", "Samsung" },
                { "ss", "Samsung" },
                { "xiaomi", "Xiaomi" },
                { "mi", "Xiaomi" },
                { "oppo", "OPPO" },
                { "vivo", "Vivo" },
                { "realme", "Realme" },
                { "nokia", "Nokia" },
                { "asus", "Asus" },
                { "huawei", "Huawei" }
            };

            foreach (var brand in brands)
            {
                string brandName = brand.TenThuongHieu.ToLower();
                string brandAlias = (brand.BiDanh ?? "").ToLower();
                
                // Check exact match
                if (message.Contains(brandName) || 
                    (!string.IsNullOrEmpty(brandAlias) && message.Contains(brandAlias)))
                {
                    System.Diagnostics.Debug.WriteLine($"[CategoryBrand] Found brand: {brand.TenThuongHieu}");
                    return brand;
                }

                // Check brand mappings
                foreach (var mapping in brandMappings)
                {
                    if (message.Contains(mapping.Key) && brandName.Contains(mapping.Value.ToLower()))
                    {
                        System.Diagnostics.Debug.WriteLine($"[CategoryBrand] Found brand via mapping: {brand.TenThuongHieu}");
                        return brand;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Lấy danh sách tất cả danh mục
        /// </summary>
        private ChatbotResponse GetCategoryList()
        {
            var response = new ChatbotResponse();

            var categories = db.tb_DanhMuc
                .Where(dm => dm.HienThi == true)
                .OrderBy(dm => dm.ThuTu)
                .ToList();

            if (categories.Any())
            {
                response.Message = "👉 Danh sách danh mục sản phẩm:\n\n";
                
                foreach (var cat in categories)
                {
                    int productCount = GetAllProducts().Where(sp => sp.DanhMucId == cat.Id).Count(sp => sp.HienThi == true);
                    response.Message += $"👉 {cat.TenDanhMuc} ({productCount} sản phẩm)\n";
                }

                response.Message += "\nBạn có thể click vào suggestion bên dưới để xem chi tiết:";
                
                response.Suggestions = categories
                    .Take(5)
                    .Select(dm => dm.TenDanhMuc)
                    .ToList();

                // Add category links
                response.CategoryLinks = categories
                    .Select(dm => new CategoryLinkViewModel
                    {
                        Id = dm.Id,
                        Name = dm.TenDanhMuc,
                        Alias = dm.BiDanh,
                        Icon = dm.Icon,
                        ProductCount = GetAllProducts().Where(sp => sp.DanhMucId == dm.Id).Count(sp => sp.HienThi == true)
                    })
                    .ToList();
            }
            else
            {
                response.Message = "Hiện tại chưa có danh mục nào.";
            }

            return response;
        }

        /// <summary>
        /// Lấy danh sách tất cả thương hiệu
        /// </summary>
        private ChatbotResponse GetBrandList()
        {
            var response = new ChatbotResponse();

            var brands = db.tb_ThuongHieu
                .Where(th => th.HienThi == true)
                .OrderBy(th => th.TenThuongHieu)
                .ToList();

            if (brands.Any())
            {
                response.Message = "👉 Danh sách thương hiệu:\n\n";
                
                foreach (var brand in brands)
                {
                    int productCount = GetAllProducts().Where(sp => sp.ThuongHieuId == brand.Id).Count(sp => sp.HienThi == true);
                    response.Message += $"👉 {brand.TenThuongHieu} ({productCount} sản phẩm)\n";
                }

                response.Message += "\nBạn có thể click vào suggestion bên dưới để xem chi tiết:";
                
                response.Suggestions = brands
                    .Take(5)
                    .Select(th => th.TenThuongHieu)
                    .ToList();

                // Add brand links
                response.BrandLinks = brands
                    .Select(th => new BrandLinkViewModel
                    {
                        Id = th.Id,
                        Name = th.TenThuongHieu,
                        Alias = th.BiDanh,
                        Image = th.HinhAnh,
                        ProductCount = GetAllProducts().Where(sp => sp.ThuongHieuId == th.Id).Count(sp => sp.HienThi == true)
                    })
                    .ToList();
            }
            else
            {
                response.Message = "Hiện tại chưa có thương hiệu nào.";
            }

            return response;
        }

        /// <summary>
        /// Lấy chi tiết danh mục và redirect
        /// </summary>
        private ChatbotResponse GetCategoryDetails(tb_DanhMuc category)
        {
            var response = new ChatbotResponse();

            int productCount = GetAllProducts().Where(sp => sp.DanhMucId == category.Id).Count(sp => sp.HienThi == true);
            
            response.Message = $"👉 {category.TenDanhMuc}\n\n";
            
            if (!string.IsNullOrEmpty(category.MoTa))
            {
                response.Message += $"{category.MoTa}\n\n";
            }
            
            response.Message += $"Có {productCount} sản phẩm trong danh mục này.\n\n";
            response.Message += $"👉 Click vào nút 'Xem danh mục' bên dưới để xem tất cả sản phẩm!";

            // Get some products from this category
            var products = GetAllProducts().Where(sp => sp.DanhMucId == category.Id)
                .Where(sp => sp.HienThi == true)
                .OrderByDescending(sp => sp.LuotXem)
                .Take(5)
                .Select(sp => new ProductChatbotViewModel
                {
                    Id = sp.Id,
                    TenSanPham = sp.TenSanPham,
                    Gia = sp.GiaKhuyenMai ?? sp.Gia ?? 0,
                    GiaGoc = sp.Gia,
                    HinhAnh = sp.HinhAnh,
                    ThuongHieu = sp.tb_ThuongHieu.TenThuongHieu
                })
                .ToList();

            response.Products = products;
            
            // Add redirect URL
            response.RedirectUrl = $"/SanPham?danhMucId={category.Id}";
            response.RedirectText = $"Xem danh mục {category.TenDanhMuc}";

            response.Suggestions = new List<string>
            {
                $"Xem danh mục {category.TenDanhMuc}",
                "Danh mục khác",
                "Sản phẩm bán chạy",
                "Quay lại"
            };

            return response;
        }

        /// <summary>
        /// Lấy chi tiết thương hiệu và redirect
        /// </summary>
        private ChatbotResponse GetBrandDetails(tb_ThuongHieu brand)
        {
            var response = new ChatbotResponse();

            int productCount = GetAllProducts().Where(sp => sp.ThuongHieuId == brand.Id).Count(sp => sp.HienThi == true);
            
            response.Message = $"👉 {brand.TenThuongHieu}\n\n";
            response.Message += $"Có {productCount} sản phẩm của thương hiệu này.\n\n";
            response.Message += $"👉 Click vào nút 'Xem thương hiệu' bên dưới để xem tất cả sản phẩm!";

            // Get some products from this brand
            var products = GetAllProducts().Where(sp => sp.ThuongHieuId == brand.Id)
                .Where(sp => sp.HienThi == true)
                .OrderByDescending(sp => sp.LuotXem)
                .Take(5)
                .Select(sp => new ProductChatbotViewModel
                {
                    Id = sp.Id,
                    TenSanPham = sp.TenSanPham,
                    Gia = sp.GiaKhuyenMai ?? sp.Gia ?? 0,
                    GiaGoc = sp.Gia,
                    HinhAnh = sp.HinhAnh,
                    ThuongHieu = sp.tb_ThuongHieu.TenThuongHieu
                })
                .ToList();

            response.Products = products;
            
            // Add redirect URL
            response.RedirectUrl = $"/SanPham?thuongHieuId={brand.Id}";
            response.RedirectText = $"Xem thương hiệu {brand.TenThuongHieu}";

            response.Suggestions = new List<string>
            {
                $"Xem thương hiệu {brand.TenThuongHieu}",
                "Thương hiệu khác",
                "Sản phẩm mới nhất",
                "Quay lại"
            };

            return response;
        }

        private System.Collections.Generic.IEnumerable<tb_SanPham> GetAllProducts()
        {
            return db.tb_SanPham.Collection.Find(MongoDB.Driver.Builders<tb_SanPham>.Filter.Empty).ToList();
        }
    }

    /// <summary>
    /// Response model cho chatbot
    /// </summary>
    public class ChatbotResponse
    {
        public string Message { get; set; }
        public List<ProductChatbotViewModel> Products { get; set; }
        public List<string> Suggestions { get; set; }
        public string RedirectUrl { get; set; }
        public string RedirectText { get; set; }
        public List<CategoryLinkViewModel> CategoryLinks { get; set; }
        public List<BrandLinkViewModel> BrandLinks { get; set; }

        public ChatbotResponse()
        {
            Products = new List<ProductChatbotViewModel>();
            Suggestions = new List<string>();
            CategoryLinks = new List<CategoryLinkViewModel>();
            BrandLinks = new List<BrandLinkViewModel>();
        }
    }

    /// <summary>
    /// ViewModel cho sản phẩm trong chatbot
    /// </summary>
    public class ProductChatbotViewModel
    {
        public int Id { get; set; }
        public string TenSanPham { get; set; }
        public decimal Gia { get; set; }
        public decimal? GiaGoc { get; set; }
        public string HinhAnh { get; set; }
        public string ThuongHieu { get; set; }
    }

    /// <summary>
    /// ViewModel cho danh mục link trong chatbot
    /// </summary>
    public class CategoryLinkViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Alias { get; set; }
        public string Icon { get; set; }
        public int ProductCount { get; set; }
    }

    /// <summary>
    /// ViewModel cho thương hiệu link trong chatbot
    /// </summary>
    public class BrandLinkViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Alias { get; set; }
        public string Image { get; set; }
        public int ProductCount { get; set; }
    }
}
