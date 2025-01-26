using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data.SqlClient;
using System.Collections.Generic;

namespace Kitapyurdu_Clone.Pages
{
    public class CategoryDetailModel : PageModel
    {
        public List<Book> Books { get; set; } = new List<Book>();
        public string CategoryName { get; private set; }

        public void OnGet(int categoryId)
        {
            string connectionString = "Server=DESKTOP-BT8G78S\\SQLEXPRESS;Database=DbKitapYurdu;Trusted_Connection=True;MultipleActiveResultSets=true";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Kategorinin adýný almak için sorgu
                string categoryQuery = "SELECT CategoryName FROM Categories WHERE CategoryID = @CategoryID";
                using (SqlCommand categoryCommand = new SqlCommand(categoryQuery, connection))
                {
                    categoryCommand.Parameters.AddWithValue("@CategoryID", categoryId);
                    CategoryName = categoryCommand.ExecuteScalar()?.ToString();
                }

                // Kitaplarý almak için sorgu
                string bookQuery = @"
                    SELECT Title, Author, Price, bookImage
                    FROM Books 
                    WHERE CategoryID = @CategoryID";

                using (SqlCommand bookCommand = new SqlCommand(bookQuery, connection))
                {
                    bookCommand.Parameters.AddWithValue("@CategoryID", categoryId);
                    using (SqlDataReader reader = bookCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Books.Add(new Book
                            {
                                Title = reader["Title"].ToString(),
                                Author = reader["Author"].ToString(),
                                Price = Convert.ToDecimal(reader["Price"]),
                                bookImage = reader["bookImage"].ToString()
                            });
                        }
                    }
                }
            }
        }

        public class Book
        {
            public string Title { get; set; }
            public string Author { get; set; }
            public decimal Price { get; set; }
            public string bookImage { get; set; }
        }
    }
}
