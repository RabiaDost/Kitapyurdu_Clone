using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data.SqlClient;
using System.Text.Json;
using Kitapyurdu_Clone.Pages.Models;
using Microsoft.Extensions.Logging;

namespace Kitapyurdu_Clone.Pages
{
    public class sepetimModel : PageModel
    {
        private readonly string connectionString = "Server=DESKTOP-BT8G78S\\SQLEXPRESS;Database=DbKitapYurdu;Trusted_Connection=True;MultipleActiveResultSets=true";
        private readonly ILogger<sepetimModel> _logger;

        public List<CartItem> CartItems { get; set; }
        public decimal TotalPrice => CartItems?.Sum(item => item.Price * item.Quantity) ?? 0;

        public sepetimModel(ILogger<sepetimModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            var cartJson = HttpContext.Session.GetString("Cart");
            CartItems = string.IsNullOrEmpty(cartJson)
                ? new List<CartItem>()
                : JsonSerializer.Deserialize<List<CartItem>>(cartJson);
        }

        public async Task<IActionResult> OnPostUpdateQuantityAsync([FromBody] UpdateQuantityRequest request)
        {
            if (request == null)
            {
                return new JsonResult(new { success = false, message = "Geçersiz istek" });
            }

            try
            {
                var cartJson = HttpContext.Session.GetString("Cart");
                var cart = string.IsNullOrEmpty(cartJson)
                    ? new List<CartItem>()
                    : JsonSerializer.Deserialize<List<CartItem>>(cartJson);

                var item = cart.FirstOrDefault(x => x.Id == request.ItemId && x.Type == request.ItemType);
                if (item == null)
                {
                    return new JsonResult(new { success = false, message = "Ürün bulunamadý" });
                }

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string table = request.ItemType == "Book" ? "Books" : "Magazines";
                    string idColumn = request.ItemType == "Book" ? "BookID" : "DergiID";

                    string query = $"SELECT Stock FROM {table} WHERE {idColumn} = @ItemId";
                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@ItemId", request.ItemId);
                        var currentStock = (int)await cmd.ExecuteScalarAsync();

                        var newQuantity = item.Quantity + request.Change;
                        if (newQuantity <= 0)
                        {
                            cart.Remove(item);
                        }
                        else if (newQuantity <= currentStock)
                        {
                            item.Quantity = newQuantity;
                        }
                        else
                        {
                            return new JsonResult(new { success = false, message = "Stok miktarý yetersiz" });
                        }
                    }
                }

                HttpContext.Session.SetString("Cart", JsonSerializer.Serialize(cart));
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = "Bir hata oluþtu: " + ex.Message });
            }
        }

        public IActionResult OnPostRemoveItem([FromBody] RemoveItemRequest request)
        {
            if (request == null)
            {
                return new JsonResult(new { success = false, message = "Geçersiz istek" });
            }

            try
            {
                var cartJson = HttpContext.Session.GetString("Cart");
                var cart = string.IsNullOrEmpty(cartJson)
                    ? new List<CartItem>()
                    : JsonSerializer.Deserialize<List<CartItem>>(cartJson);

                var item = cart.FirstOrDefault(x => x.Id == request.ItemId && x.Type == request.ItemType);
                if (item != null)
                {
                    cart.Remove(item);
                    HttpContext.Session.SetString("Cart", JsonSerializer.Serialize(cart));
                }

                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = "Bir hata oluþtu: " + ex.Message });
            }
        }

        public JsonResult OnPostCheckAuth()
        {
            var isAuthenticated = User?.Identity?.IsAuthenticated ?? false;
            var userName = User?.Identity?.Name;

            _logger.LogInformation($"CheckAuth called - IsAuthenticated: {isAuthenticated}, UserName: {userName}");

            // Session'dan kullanýcý bilgilerini kontrol et
            var sessionUserName = HttpContext.Session.GetString("UserName");
            var isSessionValid = !string.IsNullOrEmpty(sessionUserName);

            _logger.LogInformation($"Session check - SessionUserName: {sessionUserName}, IsSessionValid: {isSessionValid}");

            // Hem Identity hem de Session kontrolü yap
            isAuthenticated = isAuthenticated && isSessionValid;

            return new JsonResult(new
            {
                isAuthenticated,
                userName = sessionUserName,
                message = isAuthenticated ? "Authenticated" : "Not authenticated"
            });
        }

        public class UpdateQuantityRequest
        {
            public int ItemId { get; set; }
            public string ItemType { get; set; }
            public int Change { get; set; }
        }

        public class RemoveItemRequest
        {
            public int ItemId { get; set; }
            public string ItemType { get; set; }
        }
    }
}
