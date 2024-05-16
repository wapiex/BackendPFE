using Microsoft.EntityFrameworkCore;
using testloggg.models;

namespace testloggg.data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {
        public DbSet<Login> Logins { get; set; }
    }

}
