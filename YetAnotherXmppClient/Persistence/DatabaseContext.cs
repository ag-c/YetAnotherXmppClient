using Microsoft.EntityFrameworkCore;
using YetAnotherXmppClient.Persistence.Model;

namespace YetAnotherXmppClient.Persistence
{
    public class DatabaseContext : DbContext
    {
        public DbSet<Avatar> Avatars { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=data.db");
        }
    }
}
