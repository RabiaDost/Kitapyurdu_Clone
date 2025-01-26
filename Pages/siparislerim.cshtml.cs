using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using System.Data.SqlClient;

namespace Kitapyurdu_Clone.Pages
{
    [Authorize]
    public class siparislerimModel : PageModel
    {
        private readonly string connectionString = "Server=DESKTOP-BT8G78S\\SQLEXPRESS;Database=DbKitapYurdu;Trusted_Connection=True;MultipleActiveResultSets=true";
        public List<Order> Orders { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!User?.Identity?.IsAuthenticated ?? true)
            {
                return RedirectToPage("/login", new { returnUrl = "/siparislerim" });
            }

            Orders = new List<Order>();
            var userName = HttpContext.Session.GetString("UserName");

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Önce kullanýcý ID'sini al
                    string userQuery = "SELECT UserID FROM Users WHERE UserName = @UserName";
                    int userId;
                    using (var command = new SqlCommand(userQuery, connection))
                    {
                        command.Parameters.AddWithValue("@UserName", userName);
                        var result = await command.ExecuteScalarAsync();
                        if (result == null)
                        {
                            return RedirectToPage("/login");
                        }
                        userId = (int)result;
                    }

                    // Sipariþleri al
                    string orderQuery = @"
                        SELECT o.OrderID, o.OrderDate, o.TotalAmount,
                               od.Quantity, od.Price, od.ProductType,
                               CASE 
                                   WHEN od.ProductType = 'Book' THEN b.Title
                                   ELSE m.Title
                               END as ProductTitle,
                               od.BookID, od.DergiID
                        FROM Orders o
                        JOIN OrderDetails od ON o.OrderID = od.OrderID
                        LEFT JOIN Books b ON od.BookID = b.BookID
                        LEFT JOIN Magazines m ON od.DergiID = m.DergiID
                        WHERE o.UserID = @UserID
                        ORDER BY o.OrderDate DESC";

                    using (var command = new SqlCommand(orderQuery, connection))
                    {
                        command.Parameters.AddWithValue("@UserID", userId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            Order currentOrder = null;
                            while (await reader.ReadAsync())
                            {
                                var orderId = reader.GetInt32(reader.GetOrdinal("OrderID"));

                                if (currentOrder == null || currentOrder.OrderID != orderId)
                                {
                                    currentOrder = new Order
                                    {
                                        OrderID = orderId,
                                        OrderDate = reader.GetDateTime(reader.GetOrdinal("OrderDate")),
                                        TotalAmount = reader.GetDecimal(reader.GetOrdinal("TotalAmount")),
                                        OrderDetails = new List<OrderDetail>()
                                    };
                                    Orders.Add(currentOrder);
                                }

                                currentOrder.OrderDetails.Add(new OrderDetail
                                {
                                    ProductTitle = reader.GetString(reader.GetOrdinal("ProductTitle")),
                                    Quantity = reader.GetInt32(reader.GetOrdinal("Quantity")),
                                    Price = reader.GetDecimal(reader.GetOrdinal("Price")),
                                    ProductType = reader.GetString(reader.GetOrdinal("ProductType")),
                                    BookID = reader.IsDBNull(reader.GetOrdinal("BookID")) ? null : reader.GetInt32(reader.GetOrdinal("BookID")),
                                    DergiID = reader.IsDBNull(reader.GetOrdinal("DergiID")) ? null : reader.GetInt32(reader.GetOrdinal("DergiID"))
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Sipariþler yüklenirken bir hata oluþtu: " + ex.Message;
            }

            return Page();
        }
    }

    public class Order
    {
        public int OrderID { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public List<OrderDetail> OrderDetails { get; set; }
    }

    public class OrderDetail
    {
        public string ProductTitle { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string ProductType { get; set; }
        public int? BookID { get; set; }
        public int? DergiID { get; set; }
    }
}
