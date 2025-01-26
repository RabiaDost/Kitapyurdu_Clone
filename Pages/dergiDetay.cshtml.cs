using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using Kitapyurdu_Clone.Pages.Models;

namespace Kitapyurdu_Clone.Pages
{
    public class dergiDetayModel : PageModel
    {
        private readonly string connectionString = "Server=DESKTOP-BT8G78S\\SQLEXPRESS;Database=DbKitapYurdu;Trusted_Connection=True;MultipleActiveResultSets=true";

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }
        public Magazines Magazine { get; set; }

        public void OnGet()
        {
            if (Id <= 0)
            {
                Magazine = null;
                return;
            }

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT DergiID, Title, Price, imagePath, Stock FROM Magazines WHERE DergiID = @DergiID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DergiID", Id);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            Magazine = new Magazines
                            {
                                DergiID = reader.GetInt32(0),
                                Title = reader.GetString(1),
                                Price = reader.GetDecimal(2),
                                ImagePath = reader.IsDBNull(3) ? "/images/placeholder.png" : reader.GetString(3),
                                Stock = reader.GetInt32(4)
                            };
                        }
                        else
                        {
                            Magazine = null;
                        }
                    }
                }
            }
        }

        public async Task<IActionResult> OnPostAddToCartAsync([FromBody] AddToCartRequest request)
        {
            if (request == null || request.DergiId <= 0)
            {
                return new JsonResult(new { success = false, message = "Geçersiz istek" });
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string query = "SELECT Stock, Title, Price, imagePath FROM Magazines WHERE DergiID = @DergiID";
                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@DergiID", request.DergiId);
                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var currentStock = reader.GetInt32(0);
                                if (currentStock <= 0)
                                {
                                    return new JsonResult(new { success = false, message = "Ürün stokta yok" });
                                }

                                var cartJson = HttpContext.Session.GetString("Cart");
                                var cart = string.IsNullOrEmpty(cartJson)
                                    ? new List<CartItem>()
                                    : JsonSerializer.Deserialize<List<CartItem>>(cartJson);

                                var existingItem = cart.FirstOrDefault(x => x.Id == request.DergiId && x.Type == "Magazine");
                                if (existingItem != null)
                                {
                                    if (existingItem.Quantity >= currentStock)
                                    {
                                        return new JsonResult(new { success = false, message = "Stok miktarý yetersiz" });
                                    }
                                    existingItem.Quantity++;
                                }
                                else
                                {
                                    cart.Add(new CartItem
                                    {
                                        Id = request.DergiId,
                                        Type = "Magazine",
                                        Title = reader.GetString(1),
                                        Price = reader.GetDecimal(2),
                                        ImagePath = reader.GetString(3),
                                        Quantity = 1
                                    });
                                }

                                HttpContext.Session.SetString("Cart", JsonSerializer.Serialize(cart));

                                return new JsonResult(new
                                {
                                    success = true,
                                    message = "Ürün sepete eklendi",
                                    cartItemCount = cart.Sum(x => x.Quantity)
                                });
                            }
                            return new JsonResult(new { success = false, message = "Ürün bulunamadý" });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hata: {ex}"); 
                return new JsonResult(new { success = false, message = "Bir hata oluþtu: " + ex.Message });
            }
        }

        public class Magazines
        {
            public int DergiID { get; set; }
            public string Title { get; set; }
            public decimal Price { get; set; }
            public string ImagePath { get; set; }
            public int Stock { get; set; }
        }

        public class AddToCartRequest
        {
            public int DergiId { get; set; }
        }
    }
}
