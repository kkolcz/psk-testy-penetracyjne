using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Data.SqlClient;
using System;
using API.Models;
using API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
var key = Encoding.ASCII.GetBytes(jwtSettings.Secret);


builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddScoped<JwtTokenService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = long.MaxValue; 
});

var app = builder.Build();



app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");
    c.RoutePrefix = string.Empty;
});


app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

InitializeDatabase(app.Configuration.GetConnectionString("DefaultConnection"));

app.Run();

void InitializeDatabase(string connectionString)
{
    try
    {
        var masterConnectionString = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = "master"
        }.ConnectionString;

        using (var connection = new SqlConnection(masterConnectionString))
        {
            connection.Open();
            using (var command = new SqlCommand(
            "IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'TestyPen') " +
            "CREATE DATABASE TestyPen", connection))
            {
                command.ExecuteNonQuery();
            }
        }

        using (var connection = new SqlConnection(connectionString))
        {
            connection.Open();

            using (var command = new SqlCommand(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
                CREATE TABLE Users (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    Username NVARCHAR(100) NOT NULL,
                    Email NVARCHAR(100) NOT NULL,
                    Password NVARCHAR(100) NOT NULL,
                    UserRole NVARCHAR(100) NOT NULL
                )", connection))
            {
                command.ExecuteNonQuery();
            }

            using (var command = new SqlCommand(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Products')
                CREATE TABLE Products (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    Name NVARCHAR(100) NOT NULL,
                    Description NVARCHAR(MAX),
                    Price DECIMAL(10,2) NOT NULL,
                    StockQuantity INT NOT NULL
                )", connection))
            {
                command.ExecuteNonQuery();
            }

            using (var command = new SqlCommand(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'FileUploads')
                CREATE TABLE FileUploads (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    FileName NVARCHAR(255) NOT NULL,
                    FilePath NVARCHAR(500) NOT NULL,
                    ContentType NVARCHAR(100) NOT NULL,
                    FileSize BIGINT NOT NULL,
                    UploadedAt DATETIME NOT NULL,
                    UserId INT NULL,
                    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE SET NULL
                )", connection))
            {
                command.ExecuteNonQuery();
            }

            using (var checkCommand = new SqlCommand("SELECT COUNT(*) FROM Users", connection))
            {
                var count = (int)checkCommand.ExecuteScalar();
                if (count == 0)
                {
                    using (var command = new SqlCommand(@"
                        INSERT INTO Users (Username, Email, Password) VALUES
                        ('admin', 'admin@example.com', 'admin123'),
                        ('user1', 'user1@example.com', 'password123')", connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
            }

            using (var checkCommand = new SqlCommand("SELECT COUNT(*) FROM Products", connection))
            {
                var count = (int)checkCommand.ExecuteScalar();
                if (count == 0)
                {
                    using (var command = new SqlCommand(@"
                        INSERT INTO Products (Name, Description, Price, StockQuantity) VALUES
                        ('Laptop', 'High performance laptop', 999.99, 50),
                        ('Smartphone', 'Latest model smartphone', 499.99, 100),
                        ('Headphones', 'Noise cancelling headphones', 149.99, 200)", connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database initialization failed: {ex.Message}");
    }
}
