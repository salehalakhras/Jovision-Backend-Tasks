using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Task_44.Models
{
    public class PersonContext : DbContext
    {
        public PersonContext(DbContextOptions<PersonContext> options)
            : base(options)
        {
        }

        public DbSet<Person> Person { get; set; } = null!;
    }
}
