using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Data
{
    /// <summary>
    /// Database context for the API
    /// </summary>
    /// <param name="options"></param>
    public class AppDb(DbContextOptions options) : DbContext(options)
    {
        // define table mapping for Todo model
        public DbSet<Todo> Todos => Set<Todo>();

    }
}