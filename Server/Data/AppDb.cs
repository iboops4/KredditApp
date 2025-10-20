using Microsoft.EntityFrameworkCore;
using Server.Models;
using System.Collections.Generic;

namespace Server.Data;

public class AppDb : DbContext
{
    // EF CLI kræver en parameterløs constructor
    public AppDb() { }

    public AppDb(DbContextOptions<AppDb> opt) : base(opt) { }

    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Comment> Comments => Set<Comment>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Hvis der ikke allerede er konfigureret (fx fra Program.cs)
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite("Data Source=redditlite.db");
        }
    }
}
