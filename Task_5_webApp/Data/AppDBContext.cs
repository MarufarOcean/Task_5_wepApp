using Microsoft.EntityFrameworkCore;
using System;
using Task_5_webApp.Models;

namespace Task_5_webApp.Data
{
    public class AppDBContext: DbContext
    {
        public DbSet<User> Users { get; set; }

        public AppDBContext(DbContextOptions<AppDBContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var user = modelBuilder.Entity<User>();
            user.Property(u => u.Email).IsRequired().HasMaxLength(255);
            user.HasIndex(u => u.Email).IsUnique(); // IMPORTANT: maps unique index (still create SQL index)
        }


    }
}
