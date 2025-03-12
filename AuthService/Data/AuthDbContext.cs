using AuthService.Models.User;
using AuthService.Models.Register;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System;
using Microsoft.AspNetCore.Identity;

namespace AuthService.Data
{
    public class AuthDbContext(DbContextOptions<AuthDbContext> options)
        : IdentityDbContext<User, IdentityRole<Guid>, Guid>(options)
    {
        public DbSet<Client> Clients { get; set; } = null!;
        public DbSet<Manager> Managers { get; set; } = null!;
        public DbSet<Worker> Workers { get; set; } = null!;

        public DbSet<ManagerRegistrationCodes> ManagerRegistrationCodes { get; set; } = null!;
        public DbSet<WorkerRegistrationCodes> WorkerRegistrationCodes { get; set; } = null!;

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

            // Заполняем тестовыми кодами с фиксированными GUID
            builder.Entity<ManagerRegistrationCodes>().HasData(
                new ManagerRegistrationCodes { Id = Guid.Parse("f47ac10b-58cc-4372-a567-0e02b2c3d479"), Code = "mng301" },
                new ManagerRegistrationCodes { Id = Guid.Parse("34ac2c0a-3f3d-4a4b-809a-233983ebfd23"), Code = "mng302" },
                new ManagerRegistrationCodes { Id = Guid.Parse("95a1e9bd-df7e-4b79-b814-91234f382147"), Code = "mng303" },
                new ManagerRegistrationCodes { Id = Guid.Parse("85c47ef2-92b9-4b56-bc1c-768923b5e5b4"), Code = "mng304" },
                new ManagerRegistrationCodes { Id = Guid.Parse("0f77b8fc-1291-42e7-9ae0-6c2b9a944af1"), Code = "mng305" }
            );

            builder.Entity<WorkerRegistrationCodes>().HasData(
                new WorkerRegistrationCodes { Id = Guid.Parse("c2a4e71f-8f38-46b1-98aa-98585df4f236"), Code = "wrk101" },
                new WorkerRegistrationCodes { Id = Guid.Parse("aa8a5e9e-93e6-4d55-a52e-6590f1f99126"), Code = "wrk102" },
                new WorkerRegistrationCodes { Id = Guid.Parse("731adc3a-ff9c-44b1-b60d-9082a02f4cd7"), Code = "wrk103" },
                new WorkerRegistrationCodes { Id = Guid.Parse("cb76f06e-d445-4c1c-85b6-029d2c6f8bd2"), Code = "wrk104" },
                new WorkerRegistrationCodes { Id = Guid.Parse("5d7a27d9-4b1c-47d4-b8f6-7dd1a0e7ef5b"), Code = "wrk105" }
            );
        }
    }
}
