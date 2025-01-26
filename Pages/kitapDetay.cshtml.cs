using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data.SqlClient;
using System.Text.Json;
using Kitapyurdu_Clone.Pages.Models;
using Microsoft.Extensions.Configuration;
using Dapper;

namespace Kitapyurdu_Clone.Pages
{
    public class kitapDetayModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public kitapDetayModel(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
        }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }
        public Books Book { get; set; }
        public List<Review> Reviews { get; set; }

        [BindProperty]
        public string Comment { get; set; }

        [BindProperty]
        public int Rating { get; set; }

        public void OnGet()
        {
            if (Id <= 0)
            {
                Book = null;
                return;
            }

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // Kitap bilgilerini al
                string bookQuery = "SELECT BookID, Title, Price, bookImage, Stock, Rating FROM Books WHERE BookID = @BookID";
                using (SqlCommand command = new SqlCommand(bookQuery, connection))
                {
                    command.Parameters.AddWithValue("@BookID", Id);
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            Book = new Books
                            {
                                BookID = reader.GetInt32(0),
                                Title = reader.GetString(1),
                                Price = reader.GetDecimal(2),
                                ImagePath = reader.IsDBNull(3) ? "/images/placeholder.png" : reader.GetString(3),
                                Stock = reader.GetInt32(4),
                                Rating = reader.IsDBNull(5) ? null : reader.GetDecimal(5)
                            };
                        }
                        else
                        {
                            Book = null;
                        }
                    }
                }

                // Yorumları al
                if (Book != null)
                {
                    Reviews = new List<Review>();
                    string query = @"
                        SELECT 
                            r.ReviewID,
                            r.BookID,
                            r.UserID,
                            dbo.GetFullName(u.UserName, u.UserSurname) as UserName,
                            r.Rating,
                            r.Comment,
                            r.CreatedAt
                        FROM Reviews r
                        JOIN Users u ON r.UserID = u.UserID
                        WHERE r.BookID = @BookID";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@BookID", Id);
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Reviews.Add(new Review
                                {
                                    ReviewID = reader.GetInt32(0),
                                    BookID = reader.GetInt32(1),
                                    UserID = reader.GetInt32(2),
                                    UserName = reader.GetString(3),
                                    Rating = reader.GetInt32(4),
                                    Comment = reader.GetString(5),
                                    CreatedAt = reader.GetDateTime(6)
                                });
                            }
                        }
                    }
                }
            }
        }

        public async Task<IActionResult> OnPostAddToCartAsync([FromBody] AddToCartRequest request)
        {
            if (request == null || request.BookId <= 0)
            {
                return new JsonResult(new { success = false, message = "Geçersiz istek" });
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Kitabn stok durumunu ve bilgilerini kontrol et
                    string query = "SELECT Stock, Title, Price, bookImage FROM Books WHERE BookID = @BookID";
                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@BookID", request.BookId);
                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var currentStock = reader.GetInt32(0);
                                if (currentStock <= 0)
                                {
                                    return new JsonResult(new { success = false, message = "Ürün stokta yok" });
                                }

                                // Session'dan sepeti al
                                var cartJson = HttpContext.Session.GetString("Cart");
                                var cart = string.IsNullOrEmpty(cartJson)
                                    ? new List<CartItem>()
                                    : JsonSerializer.Deserialize<List<CartItem>>(cartJson);

                                // �r�n sepette var m� kontrol et
                                var existingItem = cart.FirstOrDefault(x => x.Id == request.BookId && x.Type == "Book");
                                if (existingItem != null)
                                {
                                    if (existingItem.Quantity >= currentStock)
                                    {
                                        return new JsonResult(new { success = false, message = "Stok miktarı yetersiz" });
                                    }
                                    existingItem.Quantity++;
                                }
                                else
                                {
                                    cart.Add(new CartItem
                                    {
                                        Id = request.BookId,
                                        Type = "Book",
                                        Title = reader.GetString(1),
                                        Price = reader.GetDecimal(2),
                                        ImagePath = reader.GetString(3),
                                        Quantity = 1
                                    });
                                }

                                // Sepeti session'a kaydet
                                HttpContext.Session.SetString("Cart", JsonSerializer.Serialize(cart));

                                return new JsonResult(new
                                {
                                    success = true,
                                    message = "Ürün sepete eklendi",
                                    cartItemCount = cart.Sum(x => x.Quantity)
                                });
                            }
                            return new JsonResult(new { success = false, message = "Ürün bulunamadı" });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hata: {ex}");
                return new JsonResult(new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }

        public async Task<IActionResult> OnPostAsync(int bookId)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Login");
            }

            if (string.IsNullOrEmpty(Comment))
            {
                return Page();
            }

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Önce kullanıcının ID'sini alalım
                var userIdCommand = new SqlCommand(
                    "SELECT UserID FROM Users WHERE UserName = @UserName",
                    connection);
                userIdCommand.Parameters.AddWithValue("@UserName", User.Identity.Name);
                var userId = (int)await userIdCommand.ExecuteScalarAsync();

                // Yorumu ekleyelim
                var sql = @"INSERT INTO Reviews (BookID, UserID, Rating, Comment, CreatedAt) 
                           VALUES (@BookID, @UserID, @Rating, @Comment, @CreatedAt)";

                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@BookID", bookId);
                    command.Parameters.AddWithValue("@UserID", userId);
                    command.Parameters.AddWithValue("@Rating", Rating);
                    command.Parameters.AddWithValue("@Comment", Comment);
                    command.Parameters.AddWithValue("@CreatedAt", DateTime.Now);

                    await command.ExecuteNonQueryAsync();
                }
            }

            return RedirectToPage(new { id = bookId });
        }

        public class Books
        {
            public int BookID { get; set; }
            public string Title { get; set; }
            public decimal Price { get; set; }
            public string ImagePath { get; set; }
            public int Stock { get; set; }
            public decimal? Rating { get; set; }
        }

        public class Review
        {
            public int ReviewID { get; set; }
            public int BookID { get; set; }
            public int UserID { get; set; }
            public int Rating { get; set; }
            public string Comment { get; set; }
            public DateTime CreatedAt { get; set; }
            public string UserName { get; set; }
        }

        public class AddToCartRequest
        {
            public int BookId { get; set; }
        }
    }
}
