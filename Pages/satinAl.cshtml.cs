using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;
using Kitapyurdu_Clone.Pages.Models;
using Microsoft.Extensions.Logging;
using System.Data.SqlClient;

namespace Kitapyurdu_Clone.Pages
{
    [Authorize]
    public class satinAlModel : PageModel
    {
        private readonly ILogger<satinAlModel> _logger;
        private readonly string connectionString = "Server=DESKTOP-BT8G78S\\SQLEXPRESS;Database=DbKitapYurdu;Trusted_Connection=True;MultipleActiveResultSets=true";

        public satinAlModel(ILogger<satinAlModel> logger)
        {
            _logger = logger;
        }

        public decimal TotalPrice { get; set; }
        public List<CartItem> CartItems { get; set; }

        public IActionResult OnGet()
        {
            if (!User?.Identity?.IsAuthenticated ?? true)
            {
                _logger.LogWarning($"Unauthorized access attempt to satinAl page - User: {User?.Identity?.Name}");
                string returnUrl = HttpContext.Request.Path;
                return RedirectToPage("/login", new { returnUrl });
            }

            try
            {
                var cartJson = HttpContext.Session.GetString("Cart");
                if (string.IsNullOrEmpty(cartJson))
                {
                    _logger.LogWarning("Cart is empty, redirecting to sepetim");
                    return RedirectToPage("/sepetim");
                }

                CartItems = JsonSerializer.Deserialize<List<CartItem>>(cartJson);
                if (CartItems == null || !CartItems.Any())
                {
                    _logger.LogWarning("Cart is empty after deserialization");
                    return RedirectToPage("/sepetim");
                }

                TotalPrice = CartItems.Sum(item => item.Price * item.Quantity);
                _logger.LogInformation($"Cart loaded successfully - Items: {CartItems.Count}, Total Price: {TotalPrice}");

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in satinAl page: {ex.Message}");
                TempData["ErrorMessage"] = "Sepet bilgileri yüklenirken bir hata oluþtu.";
                return RedirectToPage("/sepetim");
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!User?.Identity?.IsAuthenticated ?? true)
            {
                string returnUrl = HttpContext.Request.Path;
                return RedirectToPage("/login", new { returnUrl });
            }

            try
            {
                var cartJson = HttpContext.Session.GetString("Cart");
                if (string.IsNullOrEmpty(cartJson))
                {
                    return RedirectToPage("/sepetim");
                }

                CartItems = JsonSerializer.Deserialize<List<CartItem>>(cartJson);
                if (CartItems == null || CartItems.Count == 0)
                {
                    return RedirectToPage("/sepetim");
                }

                TotalPrice = CartItems.Sum(item => item.Price * item.Quantity);

                // Kullanýcý ID'sini al
                var userName = HttpContext.Session.GetString("UserName");
                int userId;

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Kullanýcý ID'sini bul
                            string userQuery = "SELECT UserID FROM Users WHERE UserName = @UserName";
                            using (var command = new SqlCommand(userQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@UserName", userName);
                                var result = await command.ExecuteScalarAsync();
                                if (result == null)
                                {
                                    throw new Exception("Kullanýcý bulunamadý");
                                }
                                userId = (int)result;
                            }

                            // Sipariþi kaydet
                            string orderQuery = "INSERT INTO Orders (UserID, OrderDate, TotalAmount) VALUES (@UserID, @OrderDate, @TotalAmount); SELECT SCOPE_IDENTITY();";
                            using (var command = new SqlCommand(orderQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@UserID", userId);
                                command.Parameters.AddWithValue("@OrderDate", DateTime.Now);
                                command.Parameters.AddWithValue("@TotalAmount", TotalPrice);

                                // Sipariþ ID'sini al
                                var orderId = Convert.ToInt32(await command.ExecuteScalarAsync());
                                _logger.LogInformation($"Order created - OrderID: {orderId}, UserID: {userId}, Total: {TotalPrice}");

                                // Sipariþ detaylarýný kaydet
                                foreach (var item in CartItems)
                                {
                                    // Stok kontrolü
                                    string table = item.Type == "Book" ? "Books" : "Magazines";
                                    string idColumn = item.Type == "Book" ? "BookID" : "DergiID";

                                    string checkStockQuery = $"SELECT Stock FROM {table} WHERE {idColumn} = @ProductID";
                                    using (var stockCheckCommand = new SqlCommand(checkStockQuery, connection, transaction))
                                    {
                                        stockCheckCommand.Parameters.AddWithValue("@ProductID", item.Id);
                                        var currentStock = (int)await stockCheckCommand.ExecuteScalarAsync();
                                        if (currentStock < item.Quantity)
                                        {
                                            throw new Exception($"Yetersiz stok! Ürün: {item.Title}, Mevcut stok: {currentStock}");
                                        }
                                    }

                                    string detailQuery = @"
                                        INSERT INTO OrderDetails 
                                        (OrderID, BookID, DergiID, ProductType, Quantity, Price) 
                                        VALUES 
                                        (@OrderID, @BookID, @DergiID, @ProductType, @Quantity, @Price)";

                                    using (var detailCommand = new SqlCommand(detailQuery, connection, transaction))
                                    {
                                        detailCommand.Parameters.AddWithValue("@OrderID", orderId);

                                        if (string.IsNullOrEmpty(item.Type))
                                        {
                                            throw new Exception("Ürün tipi belirtilmemiþ!");
                                        }

                                        if (item.Type == "Book")
                                        {
                                            if (item.Id <= 0)
                                            {
                                                throw new Exception("Geçersiz kitap ID'si!");
                                            }
                                            detailCommand.Parameters.AddWithValue("@BookID", item.Id);
                                            detailCommand.Parameters.AddWithValue("@DergiID", DBNull.Value);
                                            detailCommand.Parameters.AddWithValue("@ProductType", "Book");
                                        }
                                        else if (item.Type == "Magazine")
                                        {
                                            if (item.Id <= 0)
                                            {
                                                throw new Exception("Geçersiz dergi ID'si!");
                                            }
                                            detailCommand.Parameters.AddWithValue("@BookID", DBNull.Value);
                                            detailCommand.Parameters.AddWithValue("@DergiID", item.Id);
                                            detailCommand.Parameters.AddWithValue("@ProductType", "Magazine");
                                        }
                                        else
                                        {
                                            throw new Exception($"Geçersiz ürün tipi: {item.Type}");
                                        }

                                        detailCommand.Parameters.AddWithValue("@Quantity", item.Quantity);
                                        detailCommand.Parameters.AddWithValue("@Price", item.Price);
                                        await detailCommand.ExecuteNonQueryAsync();

                                        _logger.LogInformation($"OrderDetail added - OrderID: {orderId}, ProductType: {item.Type}, ProductID: {item.Id}, Quantity: {item.Quantity}");
                                    }

                                    // Stok güncelleme
                                    string updateStockQuery = $"UPDATE {table} SET Stock = Stock - @Quantity WHERE {idColumn} = @ProductID";
                                    using (var stockCommand = new SqlCommand(updateStockQuery, connection, transaction))
                                    {
                                        stockCommand.Parameters.AddWithValue("@Quantity", item.Quantity);
                                        stockCommand.Parameters.AddWithValue("@ProductID", item.Id);
                                        await stockCommand.ExecuteNonQueryAsync();

                                        _logger.LogInformation($"Stock updated - Table: {table}, ProductID: {item.Id}, Reduced: {item.Quantity}");
                                    }
                                }
                            }

                            transaction.Commit();
                            _logger.LogInformation("Transaction committed successfully");

                            // Sepeti temizle
                            HttpContext.Session.Remove("Cart");

                            TempData["SuccessMessage"] = "Sipariþiniz baþarýyla tamamlandý!";
                            return RedirectToPage("/MainPage");
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            _logger.LogError($"Error processing order: {ex.Message}");
                            TempData["ErrorMessage"] = "Sipariþ iþlenirken bir hata oluþtu: " + ex.Message;
                            return Page();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing order: {ex.Message}");
                TempData["ErrorMessage"] = "Sipariþ iþlenirken bir hata oluþtu: " + ex.Message;
                return Page();
            }
        }
    }
}
