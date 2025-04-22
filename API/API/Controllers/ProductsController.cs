using API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly string _connectionString;

        public ProductsController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        [HttpGet]
        public ActionResult<IEnumerable<Product>> GetProducts()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var command = new SqlCommand("SELECT * FROM Products", connection);
                var reader = command.ExecuteReader();
                var products = new List<Product>();

                while (reader.Read())
                {
                    products.Add(new Product
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        Description = reader.GetString(reader.GetOrdinal("Description")),
                        Price = reader.GetDecimal(reader.GetOrdinal("Price")),
                        StockQuantity = reader.GetInt32(reader.GetOrdinal("StockQuantity"))
                    });
                }

                return Ok(products);
            }
        }

        
        [HttpGet("{id}")]
        public ActionResult<Product> GetProductById(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                var command = new SqlCommand($"SELECT * FROM Products WHERE Id = {id}", connection);
                var reader = command.ExecuteReader();

                if (reader.Read())
                {
                    var product = new Product
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        Description = reader.GetString(reader.GetOrdinal("Description")),
                        Price = reader.GetDecimal(reader.GetOrdinal("Price")),
                        StockQuantity = reader.GetInt32(reader.GetOrdinal("StockQuantity"))
                    };
                    return Ok(product);
                }

                return NotFound();
            }
        }

        
        [HttpGet("search")]
        public ActionResult<IEnumerable<Product>> SearchProducts(string name)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                var command = new SqlCommand($"SELECT * FROM Products WHERE Name LIKE '%{name}%'", connection);
                var reader = command.ExecuteReader();
                var products = new List<Product>();

                while (reader.Read())
                {
                    products.Add(new Product
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        Description = reader.GetString(reader.GetOrdinal("Description")),
                        Price = reader.GetDecimal(reader.GetOrdinal("Price")),
                        StockQuantity = reader.GetInt32(reader.GetOrdinal("StockQuantity"))
                    });
                }

                return Ok(products);
            }
        }

        
        [HttpPost]
        public ActionResult<Product> CreateProduct([FromBody] Product product)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                var command = new SqlCommand($"INSERT INTO Products (Name, Description, Price, StockQuantity) VALUES ('{product.Name}', '{product.Description}', {product.Price}, {product.StockQuantity})", connection);
                command.ExecuteNonQuery();

                
                command = new SqlCommand("SELECT SCOPE_IDENTITY()", connection);
                var id = Convert.ToInt32(command.ExecuteScalar());
                product.Id = id;

                return CreatedAtAction(nameof(GetProductById), new { id = product.Id }, product);
            }
        }

        
        [HttpPut("{id}")]
        public IActionResult UpdateProduct(int id, [FromBody] Product product)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                var command = new SqlCommand($"UPDATE Products SET Name = '{product.Name}', Description = '{product.Description}', Price = {product.Price}, StockQuantity = {product.StockQuantity} WHERE Id = {id}", connection);
                var rowsAffected = command.ExecuteNonQuery();

                if (rowsAffected == 0)
                {
                    return NotFound();
                }

                return NoContent();
            }
        }

        
        [HttpDelete("{id}")]
        public IActionResult DeleteProduct(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                var command = new SqlCommand($"DELETE FROM Products WHERE Id = {id}", connection);
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