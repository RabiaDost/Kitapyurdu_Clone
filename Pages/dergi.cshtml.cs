using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace Kitapyurdu_Clone.Pages
{
    public class dergiModel : PageModel
    {
        public List<Magazine> Magazines { get; set; }

        public void OnGet()
        {
            Magazines = new List<Magazine>();
            string connectionString = "Server=DESKTOP-BT8G78S\\SQLEXPRESS;Database=DbKitapYurdu;Trusted_Connection=True;MultipleActiveResultSets=true"; 

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT DergiID, Title, Price, imagePath FROM Magazines";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Magazines.Add(new Magazine
                            {
                                DergiID = reader.GetInt32(0),
                                Title = reader.GetString(1),
                                Price = reader.GetDecimal(2),
                                ImagePath = reader.IsDBNull(3) ? "/images/placeholder.png" : reader.GetString(3)
                            });
                        }
                    }
                }
            }
        }

        public class Magazine
        {
            public int DergiID { get; set; }
            public string Title { get; set; }
            public decimal Price { get; set; }
            public string ImagePath { get; set; }
        }
    }
}
