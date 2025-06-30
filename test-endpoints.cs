using System.Text;
using System.Text.Json;

class Program
{
    private static readonly HttpClient httpClient = new HttpClient();
    
    static async Task Main(string[] args)
    {
        var baseUrl = "https://nanacaring-backend.onrender.com";
        var endpoints = new[]
        {
            "/admin/login",
            "/admin/auth/login", 
            "/admin/auth/admin-login",
            "/api/admin/login",
            "/api/auth/admin-login",
            "/auth/admin-login",
            "/login"
        };

        var loginData = new
        {
            email = "test@example.com",
            password = "test123"
        };

        var json = JsonSerializer.Serialize(loginData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        Console.WriteLine("Testing login endpoints...\n");

        foreach (var endpoint in endpoints)
        {
            try
            {
                var url = $"{baseUrl}{endpoint}";
                Console.WriteLine($"Testing: {url}");
                
                var response = await httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                Console.WriteLine($"Status: {response.StatusCode}");
                Console.WriteLine($"Response: {responseContent}");
                Console.WriteLine(new string('-', 50));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine(new string('-', 50));
            }
        }
    }
}
