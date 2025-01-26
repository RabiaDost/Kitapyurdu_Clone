using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data.SqlClient;

namespace Kitapyurdu_Clone.Pages
{
    public class registerModel : PageModel
    {
        [BindProperty]
        public UserInput Input { get; set; } = new UserInput(); // Default bir nesne olu�turulur
        public string ErrorMessage { get; set; }
        public string SuccessMessage { get; set; }

        public void OnPost()
        {
            // Form do�rulama
            if (string.IsNullOrWhiteSpace(Input.Name) ||
                string.IsNullOrWhiteSpace(Input.Surname) ||
                string.IsNullOrWhiteSpace(Input.Email) ||
                string.IsNullOrWhiteSpace(Input.Password) ||
                string.IsNullOrWhiteSpace(Input.PasswordRepeat))
            {
                ErrorMessage = "L�tfen t�m alanlar� doldurun.";
                return;
            }

            if (Input.Password != Input.PasswordRepeat)
            {
                ErrorMessage = "�ifreler e�le�miyor. L�tfen kontrol edin.";
                return;
            }

            // Veritaban� ba�lant�s�
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
                            SuccessMessage = "Kay�t ba�ar�l�! Giri� yapabilirsiniz.";
                            ErrorMessage = null; // �nceki hata mesaj�n� temizle
                            Input = new UserInput(); // Formu s�f�rlar
                        }
                        else
                        {
                            ErrorMessage = "Kay�t s�ras�nda bir hata olu�tu. L�tfen tekrar deneyin.";
                        }
                    }
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Bir hata olu�tu: {ex.Message}";
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
