using API.Dtos;
using API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly string _connectionString;

        public AccountController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }
        [HttpPost("register")]
        public IActionResult Register([FromBody] LoginDto registerDto)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var checkCmd = new SqlCommand("SELECT COUNT(*) FROM Users WHERE Username = @username", connection);
            checkCmd.Parameters.AddWithValue("@username", registerDto.Username);
            int userExists = (int)checkCmd.ExecuteScalar();

            if (userExists > 0)
            {
                return BadRequest("Username is taken");
            }

            var insertCmd = new SqlCommand(
                "INSERT INTO Users (Username, Email, Password) VALUES (@username, @email, @password)", connection);
            insertCmd.Parameters.AddWithValue("@username", registerDto.Username);
            insertCmd.Parameters.AddWithValue("@email", registerDto.Username + "@tu.kielce.pl");
            insertCmd.Parameters.AddWithValue("@password", registerDto.Password); 

            insertCmd.ExecuteNonQuery();

            return Ok("User registered successfully");
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDto loginRequest)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var command = new SqlCommand(
                "SELECT Id, Username, Email FROM Users WHERE Username = @username AND Password = @password",
                connection);
            command.Parameters.AddWithValue("@username", loginRequest.Username);
            command.Parameters.AddWithValue("@password", loginRequest.Password); 

            using var reader = command.ExecuteReader();

            if (!reader.Read())
            {
                return Unauthorized("Invalid username or password");
            }

            var user = new User
            {
                Id = reader.GetInt32(0),
                Username = reader.GetString(1),
                Email = reader.GetString(2),
                Password = null 
            };

            return Ok(user);
        }
    }
}
