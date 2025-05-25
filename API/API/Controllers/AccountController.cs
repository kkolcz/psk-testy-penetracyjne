using API.Dtos;
using API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

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
                "INSERT INTO Users (Username, Email, Password, UserRole) VALUES (@username, @email, @password, @userRole)", connection);
            insertCmd.Parameters.AddWithValue("@username", registerDto.Username);
            insertCmd.Parameters.AddWithValue("@email", registerDto.Username + "@tu.kielce.pl");
            insertCmd.Parameters.AddWithValue("@password", registerDto.Password); 
            insertCmd.Parameters.AddWithValue("@userRole", registerDto.UserRole); 

            insertCmd.ExecuteNonQuery();

            return Ok();
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

        [HttpPost("login-insecure")]
        public IActionResult LoginInsecure([FromBody] LoginDto loginRequest)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var sql = $"SELECT Id, Username, Email FROM Users WHERE Username = '{loginRequest.Username}' AND Password = '{loginRequest.Password}'";

            var command = new SqlCommand(sql, connection);
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

        [HttpPost("setAvatar")]
        public async Task<IActionResult> SetAvatar(IFormFile avatar, [FromForm] string userId)
        {
            if (avatar == null || avatar.Length == 0)
                return BadRequest("No file uploaded.");

            if (!int.TryParse(userId, out var userIdInt))
                return BadRequest("Invalid userId");

            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            Directory.CreateDirectory(uploadsPath);

            var newFileName = $"{Guid.NewGuid()}{Path.GetExtension(avatar.FileName)}";
            var relativePath = Path.Combine("uploads", newFileName);
            var fullPath = Path.Combine(uploadsPath, newFileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await avatar.CopyToAsync(stream);
            }

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var selectCmd = new SqlCommand(@"
        SELECT TOP 1 FilePath FROM FileUploads 
        WHERE UserId = @userId AND ContentType LIKE 'image/%' 
        ORDER BY UploadedAt DESC", connection);
            selectCmd.Parameters.AddWithValue("@userId", userIdInt);

            string? oldPath = null;
            using (var reader = await selectCmd.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    oldPath = reader.GetString(0);
                }
            }

            var deleteCmd = new SqlCommand("DELETE FROM FileUploads WHERE UserId = @userId", connection);
            deleteCmd.Parameters.AddWithValue("@userId", userIdInt);
            await deleteCmd.ExecuteNonQueryAsync();

            if (!string.IsNullOrEmpty(oldPath))
            {
                var oldFileFullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", oldPath.TrimStart('/'));
                if (System.IO.File.Exists(oldFileFullPath))
                    System.IO.File.Delete(oldFileFullPath);
            }

            var insertCmd = new SqlCommand(@"
        INSERT INTO FileUploads (FileName, FilePath, ContentType, FileSize, UploadedAt, UserId)
        VALUES (@fileName, @filePath, @contentType, @fileSize, @uploadedAt, @userId)", connection);

            insertCmd.Parameters.AddWithValue("@fileName", newFileName);
            insertCmd.Parameters.AddWithValue("@filePath", "/" + relativePath.Replace("\\", "/"));
            insertCmd.Parameters.AddWithValue("@contentType", avatar.ContentType);
            insertCmd.Parameters.AddWithValue("@fileSize", avatar.Length);
            insertCmd.Parameters.AddWithValue("@uploadedAt", DateTime.UtcNow);
            insertCmd.Parameters.AddWithValue("@userId", userIdInt);

            await insertCmd.ExecuteNonQueryAsync();

            return Ok(new { message = "Avatar uploaded", file = "/" + relativePath.Replace("\\", "/") });
        }

        [HttpGet("getAvatar")]
        public IActionResult GetAvatar([FromQuery] int userId)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var command = new SqlCommand(@"
            SELECT TOP 1 FilePath 
            FROM FileUploads 
            WHERE UserId = @userId AND ContentType LIKE 'image/%'
            ORDER BY UploadedAt DESC", connection);
            command.Parameters.AddWithValue("@userId", userId);

            var reader = command.ExecuteReader();
            if (reader.Read())
            {
                var filePath = reader.GetString(0);
                return Ok(new { filePath });
            }

            return Ok(new { filePath = (string?)null });
        }


    }
}
