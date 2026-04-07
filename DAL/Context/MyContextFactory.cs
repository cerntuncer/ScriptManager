using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DAL.Context;

/// <summary>Design-time migrations; çalışma anında connection string kullanılmaz.</summary>
public class MyContextFactory : IDesignTimeDbContextFactory<MyContext>
{
    public MyContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MyContext>();
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=ScriptManagerDesign;Trusted_Connection=True;TrustServerCertificate=True");
        return new MyContext(optionsBuilder.Options);
    }
}
