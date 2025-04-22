using API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.IO;
using System.Collections.Generic;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileUploadsController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly string _uploadPath;

        public FileUploadsController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _uploadPath = configuration["FileUploadSettings:StoragePath"];
        }

        [HttpGet]
        public ActionResult<IEnumerable<FileUpload>> GetFiles()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var command = new SqlCommand("SELECT * FROM FileUploads", connection);
                var reader = command.ExecuteReader();
                var files = new List<FileUpload>();

                while (reader.Read())
                {
                    files.Add(new FileUpload
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        FileName = reader.GetString(reader.GetOrdinal("FileName")),
                        FilePath = reader.GetString(reader.GetOrdinal("FilePath")),
                        ContentType = reader.GetString(reader.GetOrdinal("ContentType")),
                        FileSize = reader.GetInt64(reader.GetOrdinal("FileSize")),
                        UploadedAt = reader.GetDateTime(reader.GetOrdinal("UploadedAt")),
                        UserId = reader.IsDBNull(reader.GetOrdinal("UserId")) ? null : (int?)reader.GetInt32(reader.GetOrdinal("UserId"))
                    });
                }

                return Ok(files);
            }
        }

        
        [HttpGet("{id}")]
        public ActionResult<FileUpload> GetFile(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                var command = new SqlCommand($"SELECT * FROM FileUploads WHERE Id = {id}", connection);
                var reader = command.ExecuteReader();

                if (reader.Read())
                {
                    var file = new FileUpload
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        FileName = reader.GetString(reader.GetOrdinal("FileName")),
                        FilePath = reader.GetString(reader.GetOrdinal("FilePath")),
                        ContentType = reader.GetString(reader.GetOrdinal("ContentType")),
                        FileSize = reader.GetInt64(reader.GetOrdinal("FileSize")),
                        UploadedAt = reader.GetDateTime(reader.GetOrdinal("UploadedAt")),
                        UserId = reader.IsDBNull(reader.GetOrdinal("UserId")) ? null : (int?)reader.GetInt32(reader.GetOrdinal("UserId"))
                    };
                    return Ok(file);
                }

                return NotFound();
            }
        }

        
        [HttpPost]
        [DisableRequestSizeLimit]
        public ActionResult<FileUpload> UploadFile(IFormFile file, [FromQuery] int? userId)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is empty");

            try
            {
                
                if (!Directory.Exists(_uploadPath))
                {
                    Directory.CreateDirectory(_uploadPath);
                }

                
                string fileName = file.FileName;
                string filePath = Path.Combine(_uploadPath, fileName);

                
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }

                
                var fileUpload = new FileUpload
                {
                    FileName = fileName,
                    FilePath = filePath,
                    ContentType = file.ContentType,
                    FileSize = file.Length,
                    UploadedAt = DateTime.UtcNow,
                    UserId = userId
                };

                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    
                    var command = new SqlCommand(
                        $"INSERT INTO FileUploads (FileName, FilePath, ContentType, FileSize, UploadedAt, UserId) " +
                        $"VALUES ('{fileUpload.FileName}', '{fileUpload.FilePath}', '{fileUpload.ContentType}', {fileUpload.FileSize}, " +
                        $"'{fileUpload.UploadedAt:yyyy-MM-dd HH:mm:ss}', {(fileUpload.UserId.HasValue ? fileUpload.UserId.ToString() : "NULL")})",
                        connection);
                    command.ExecuteNonQuery();

                    
                    command = new SqlCommand("SELECT SCOPE_IDENTITY()", connection);
                    var id = Convert.ToInt32(command.ExecuteScalar());
                    fileUpload.Id = id;
                }

                return CreatedAtAction(nameof(GetFile), new { id = fileUpload.Id }, fileUpload);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error uploading file: {ex.Message}");
            }
        }

        
        [HttpGet("download/{id}")]
        public IActionResult DownloadFile(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                var command = new SqlCommand($"SELECT * FROM FileUploads WHERE Id = {id}", connection);
                var reader = command.ExecuteReader();

                if (reader.Read())
                {
                    var filePath = reader.GetString(reader.GetOrdinal("FilePath"));
                    var fileName = reader.GetString(reader.GetOrdinal("FileName"));
                    var contentType = reader.GetString(reader.GetOrdinal("ContentType"));

                    
                    if (System.IO.File.Exists(filePath))
                    {
                        var fileBytes = System.IO.File.ReadAllBytes(filePath);
                        return File(fileBytes, contentType, fileName);
                    }
                    else
                    {
                        return NotFound("File not found on server");
                    }
                }

                return NotFound("File record not found");
            }
        }

        
        [HttpDelete("{id}")]
        public IActionResult DeleteFile(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                var command = new SqlCommand($"SELECT FilePath FROM FileUploads WHERE Id = {id}", connection);
                var filePath = command.ExecuteScalar()?.ToString();

                if (filePath == null)
                {
                    return NotFound();
                }

                
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                
                command = new SqlCommand($"DELETE FROM FileUploads WHERE Id = {id}", connection);
                command.ExecuteNonQuery();

                return NoContent();
            }
        }
    }
}