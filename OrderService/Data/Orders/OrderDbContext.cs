using Microsoft.EntityFrameworkCore;
using OrderService.Models.Orders;
using OrderService.Models.Users;
using OrderService.Models.Technicians;

namespace OrderService.Data.Orders
{
    public class OrderDbContext(DbContextOptions<OrderDbContext> options) : DbContext(options)
    {
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderTechnician> OrderTechnicians { get; set; }
        public DbSet<OrderEquipment> OrderEquipments { get; set; }
        public DbSet<TechnicianRoute> TechnicianRoutes { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<Manager> Managers { get; set; }
        public DbSet<Technician> Technicians { get; set; } 
        public DbSet<TechnicianAppointment> Appointments { get; set; } 
        public DbSet<User> Users { get; set; } 

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Много-ко-многим: Order ↔ Technicians
            modelBuilder.Entity<OrderTechnician>()
                .HasKey(ot => new { ot.OrderID, ot.TechnicianID });

            modelBuilder.Entity<OrderTechnician>()
                .HasOne(ot => ot.Order)
                .WithMany(o => o.AssignedTechnicians)
                .HasForeignKey(ot => ot.OrderID)
                .OnDelete(DeleteBehavior.Cascade);

            // Один ко многим: Order ↔ OrderEquipments
            modelBuilder.Entity<Order>()
                .HasMany(o => o.Equipment)
                .WithOne()
                .HasForeignKey(e => e.OrderID)
                .OnDelete(DeleteBehavior.Cascade);

            // Один ко многим: Client ↔ Orders
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Client)
                .WithMany(c => c.Orders)
                .HasForeignKey(o => o.ClientID)
                .OnDelete(DeleteBehavior.Restrict);

            // Один ко многим: Manager ↔ Orders
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Manager)
                .WithMany(m => m.ManagedOrders)
                .HasForeignKey(o => o.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Один ко многим: Order ↔ TechnicianRoute
            modelBuilder.Entity<TechnicianRoute>()
                .HasOne(tr => tr.Order)
                .WithMany(o => o.TechnicianRoutes)
                .HasForeignKey(tr => tr.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // ✅ Добавлена связь Technician ↔ TechnicianAppointment
            modelBuilder.Entity<TechnicianAppointment>()
                .HasOne(a => a.Technician)
                .WithMany(t => t.Appointments)
                .HasForeignKey(a => a.TechnicianId)
                .OnDelete(DeleteBehavior.Cascade);

            base.OnModelCreating(modelBuilder);
        }
    }
}
