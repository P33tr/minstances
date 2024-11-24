using Microsoft.EntityFrameworkCore;
using minstances.Models;

namespace minstances.Data
{
    public class MinstancesContext : DbContext
    {
        public DbSet<Search> Searches { get; set; }

        public MinstancesContext(DbContextOptions<MinstancesContext> options) : base(options)
        {
        }
    }
}
