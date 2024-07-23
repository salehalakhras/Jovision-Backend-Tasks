using Microsoft.EntityFrameworkCore;

namespace Task_47.Models
{
    public class FormContext : DbContext
    {
        public FormContext(DbContextOptions<FormContext> options)
            : base(options)
        {
        }

        public DbSet<Form> FormData { get; set; } = null!;
    }
}
