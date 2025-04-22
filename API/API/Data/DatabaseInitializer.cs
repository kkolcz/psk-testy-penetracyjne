using API.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Linq;

namespace API.Data
{
    public class DatabaseInitializer
    {
        public static void Initialize(ApplicationDbContext context)
        {
            context.Database.EnsureCreated();


            if (!context.Products.Any())
            {
                context.Products.AddRange(
                    new Product { Name = "Product1", Description = "Description1", Price = 10.0M },
                    new Product { Name = "Product2", Description = "Description2", Price = 20.0M }
                );
                context.SaveChanges();
            }


            if (!context.Users.Any())
            {
                context.Users.AddRange(
                    new User { Username = "User1", Email = "user1@example.com", Password = "password1" },
                    new User { Username = "User2", Email = "user2@example.com", Password = "password2" }
                );
                context.SaveChanges();
            }
        }

        public static void UploadFile(string filePath)
        {

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
            var fileName = Path.GetFileName(filePath);
            var destinationPath = Path.Combine(uploadsFolder, fileName);

            File.Copy(filePath, destinationPath, true);
        }
    }
}