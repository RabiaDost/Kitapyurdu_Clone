using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data.SqlClient;

namespace Kitapyurdu_Clone.Pages
{
    public class registerModel : PageModel
    {
        [BindProperty]
        public UserInput Input { get; set; } = new UserInput(); // Default bir nesne oluþturulur
        public string ErrorMessage { get; set; }
        public string SuccessMessage { get; set; }

        public void OnPost()
        {
            // Form doðrulama
            if (string.IsNullOrWhiteSpace(Input.Name) ||
                string.IsNullOrWhiteSpace(Input.Surname) ||
                string.IsNullOrWhiteSpace(Input.Email) ||
                string.IsNullOrWhiteSpace(Input.Password) ||
                string.IsNullOrWhiteSpace(Input.PasswordRepeat))
            {
                ErrorMessage = "Lütfen tüm alanlarý doldurun.";
                return;
            }

            if (Input.Password != Input.PasswordRepeat)
            {
                ErrorMessage = "Þifreler eþleþmiyor. Lütfen kontrol edin.";
                return;
            }

            // Veritabaný baðlantýsý
            string connectionString = "Server=DESKTOP-BT8G78S\\SQLEXPRESS;Database=DbKitapYurdu;Trusted_Connection=True;MultipleActiveResultSets=true";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    string query = "INSERT INTO Users (UserName, UserSurname, Email, UserPassword) VALUES (@UserName, @UserSurname, @Email, @UserPassword)";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@UserName", Input.Name);
                        command.Parameters.AddWithValue("@UserSurname", Input.Surname);
                        command.Parameters.AddWithValue("@Email", Input.Email);
                        command.Parameters.AddWithValue("@UserPassword", Input.Password);

                        int result = command.ExecuteNonQuery();

                        if (result > 0)
                        {
                            SuccessMessage = "Kayýt baþarýlý! Giriþ yapabilirsiniz.";
                            ErrorMessage = null; // Önceki hata mesajýný temizle
                            Input = new UserInput(); // Formu sýfýrlar
                        }
                        else
                        {
                            ErrorMessage = "Kayýt sýrasýnda bir hata oluþtu. Lütfen tekrar deneyin.";
                        }
                    }
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Bir hata oluþtu: {ex.Message}";
                }
            }
        }

        public class UserInput
        {
            public string Name { get; set; }
            public string Surname { get; set; }
            public string Email { get; set; }
            public string Password { get; set; }
            public string PasswordRepeat { get; set; }
        }
    }
}
