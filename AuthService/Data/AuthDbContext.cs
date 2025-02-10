using AuthService.Models.User;
using AuthService.Models.Register;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace AuthService.Data
{
    public class AuthDbContext(DbContextOptions<AuthDbContext> options)
        : IdentityDbContext<User>(options)
    {
        public DbSet<Client> Clients { get; set; } = null!;
        public DbSet<Manager> Managers { get; set; } = null!;
        public DbSet<Worker> Workers { get; set; } = null!;

        public DbSet<ManagerRegistrationCodes> ManagerRegistrationCodes { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            builder.Entity<User>()
                .HasIndex(u => u.UserName)
                .IsUnique();

            builder.Entity<Client>().ToTable("Clients");
            builder.Entity<Manager>().ToTable("Managers");
            builder.Entity<Worker>().ToTable("Workers");

            // Заполняем тестовыми кодами, при масштабируемости применить другой подход
            builder.Entity<ManagerRegistrationCodes>().HasData(
                new ManagerRegistrationCodes { Code = "mng301" },
                new ManagerRegistrationCodes { Code = "mng302" },
                new ManagerRegistrationCodes { Code = "mng303" },
                new ManagerRegistrationCodes { Code = "mng304" },
                new ManagerRegistrationCodes { Code = "mng305" }
            );
        }
    }
}
