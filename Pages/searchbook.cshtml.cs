using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data.SqlClient;
using System.Diagnostics;

namespace Kitapyurdu_Clone.Pages;
public class searchbookModel : PageModel
{

    public List<SearchBook> Books { get; set; } = new List<SearchBook>();

    public void OnGet()
    {

        string connectionString = "Server=DESKTOP-BT8G78S\\SQLEXPRESS;Database=DbKitapYurdu;Trusted_Connection=True;MultipleActiveResultSets=true";

        string searchTerm = Request.Query["search"];

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // SQL sorgusu 
                    string query = @"
                        SELECT BookID, Title, Author, Price, Description, bookImage
                        FROM Books
                        WHERE Title LIKE @Search OR Author LIKE @Search";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        
                        command.Parameters.AddWithValue("@Search", $"%{searchTerm}%");

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Books.Add(new SearchBook
                                {
                                    Id = reader["BookID"].ToString(),
                                    Title = reader["Title"].ToString(),
                                    Author = reader["Author"].ToString(),
                                    Price = reader["Price"].ToString(),
                                    Description = reader["Description"].ToString(),
                                    BookImage = reader["bookImage"].ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                Debug.WriteLine($"SQL Hatasý: {sqlEx.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Genel Hata: {ex.Message}");
            }
        }

        ViewData["Books"] = Books;
    }
}
public class SearchBook

{

    public string Id { get; set; }

    public string Title { get; set; }

    public string Author { get; set; }

    public string Price { get; set; }

    public string Description { get; set; }

    public string BookImage { get; set; }

}