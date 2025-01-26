using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Kitapyurdu_Clone.Pages
{
    public class loginModel : PageModel
    {
        public string LoginError { get; set; }
        public string ReturnUrl { get; set; }

        public void OnGet(string returnUrl = null)
        {
            ReturnUrl = returnUrl ?? HttpContext.Session.GetString("ReturnUrl") ?? "/MainPage";
            HttpContext.Session.SetString("ReturnUrl", ReturnUrl);
        }

        public async Task<IActionResult> OnPost()
        {
            string email = Request.Form["email"];
            string password = Request.Form["password"];
            string returnUrl = HttpContext.Session.GetString("ReturnUrl") ?? "/MainPage";

            try
            {
                string connectionString = "Server=DESKTOP-BT8G78S\\SQLEXPRESS;Database=DbKitapYurdu;Trusted_Connection=True;MultipleActiveResultSets=true";
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string sql = "SELECT UserName, Email FROM users WHERE email = @Email AND UserPassword = @Password";
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@Email", email);
                        command.Parameters.AddWithValue("@Password", password);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (reader.Read())
                            {
                                var userName = reader["UserName"].ToString();
                                var userEmail = reader["Email"].ToString();

                                // Oturum bilgilerini ayarla
                                var claims = new List<Claim>
                                {
                                    new Claim(ClaimTypes.Name, userName),
                                    new Claim(ClaimTypes.Email, userEmail),
                                    new Claim(ClaimTypes.Role, "User")
                                };

                                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                                var authProperties = new AuthenticationProperties
                                {
                                    IsPersistent = Request.Form["remember-me"] == "on"
                                };

                                await HttpContext.SignInAsync(
                                    CookieAuthenticationDefaults.AuthenticationScheme,
                                    new ClaimsPrincipal(claimsIdentity),
                                    authProperties);

                                // Session'a kullanýcý bilgilerini kaydet
                                HttpContext.Session.SetString("UserName", userName);
                                HttpContext.Session.SetString("UserEmail", userEmail);

                                // ReturnUrl'i kontrol et ve yönlendir
                                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                                {
                                    return Redirect(returnUrl);
                                }
                                return RedirectToPage("/MainPage");
                            }
                            else
                            {
                                LoginError = "E-posta veya þifre hatalý!";
                                return Page();
                            }
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                LoginError = "Veritabaný hatasý: " + ex.Message;
                return Page();
            }
            catch (Exception ex)
            {
                LoginError = "Beklenmeyen bir hata oluþtu: " + ex.Message;
                return Page();
            }
        }

        public async Task<IActionResult> OnPostLogout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnGetLogout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToPage("/MainPage");
        }
    }
}
