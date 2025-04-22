using API.Models;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly JwtTokenService _jwtTokenService;

        public UsersController(IConfiguration configuration, JwtTokenService jwtTokenService)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _jwtTokenService = jwtTokenService;
        }

        [HttpGet]
        [Authorize]
        public ActionResult<IEnumerable<User>> GetUsers()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var command = new SqlCommand("SELECT * FROM Users", connection);
                var reader = command.ExecuteReader();
                var users = new List<User>();

                while (reader.Read())
                {
                    users.Add(new User
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        Username = reader.GetString(reader.GetOrdinal("Username")),
                        Email = reader.GetString(reader.GetOrdinal("Email")),
                        Password = reader.GetString(reader.GetOrdinal("Password"))
                    });
                }

                return Ok(users);
            }
        }

        [HttpGet("{id}")]
        [Authorize]
        public ActionResult<User> GetUserById(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                var command = new SqlCommand($"SELECT * FROM Users WHERE Id = {id}", connection);
                var reader = command.ExecuteReader();

                if (reader.Read())
                {
                    var user = new User
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        Username = reader.GetString(reader.GetOrdinal("Username")),
                        Email = reader.GetString(reader.GetOrdinal("Email")),
                        Password = reader.GetString(reader.GetOrdinal("Password"))
                    };
                    return Ok(user);
                }

                return NotFound();
            }
        }

        [HttpPost]
        public ActionResult<AuthenticationResponse> CreateUser([FromBody] User user)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                var command = new SqlCommand($"INSERT INTO Users (Username, Email, Password) VALUES ('{user.Username}', '{user.Email}', '{user.Password}')", connection);
                command.ExecuteNonQuery();
                
                command = new SqlCommand("SELECT SCOPE_IDENTITY()", connection);
                var id = Convert.ToInt32(command.ExecuteScalar());
                user.Id = id;
                
                var token = _jwtTokenService.GenerateToken(user);
                
                var response = new AuthenticationResponse
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    Token = token
                };
                
                return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, response);
            }
        }

        [HttpPost("login")]
        public ActionResult<AuthenticationResponse> Login([FromBody] LoginRequest request)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                var command = new SqlCommand($"SELECT * FROM Users WHERE Username = '{request.Username}' AND Password = '{request.Password}'", connection);
                var reader = command.ExecuteReader();

                if (reader.Read())
                {
                    var user = new User
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        Username = reader.GetString(reader.GetOrdinal("Username")),
                        Email = reader.GetString(reader.GetOrdinal("Email")),
                        Password = reader.GetString(reader.GetOrdinal("Password"))
                    };
                    
                    var token = _jwtTokenService.GenerateToken(user);
                    
                    var response = new AuthenticationResponse
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        Token = token
                    };
                    
                    return Ok(response);
                }

                return Unauthorized();
            }
        }
        
        [HttpPut("{id}")]
        [Authorize]
        public IActionResult UpdateUser(int id, [FromBody] User user)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                var command = new SqlCommand($"UPDATE Users SET Username = '{user.Username}', Email = '{user.Email}', Password = '{user.Password}' WHERE Id = {id}", connection);
                var rowsAffected = command.ExecuteNonQuery();

                if (rowsAffected == 0)
                {
                    return NotFound();
                }

                return NoContent();
            }
        }
        
        [HttpDelete("{id}")]
        [Authorize]
        public IActionResult DeleteUser(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                var command = new SqlCommand($"DELETE FROM Users WHERE Id = {id}", connection);
                var rowsAffected = command.ExecuteNonQuery();

                if (rowsAffected == 0)
                {
                    return NotFound();
                }

                return NoContent();
            }
        }
    }
}