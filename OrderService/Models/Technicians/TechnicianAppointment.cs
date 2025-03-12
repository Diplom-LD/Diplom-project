using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OrderService.Models.Users;

namespace OrderService.Models.Technicians
{
    public class TechnicianAppointment
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid(); 

        [ForeignKey("Technician")]
        public Guid TechnicianId { get; set; }
        public Technician Technician { get; set; } = null!;

        public DateTime Date { get; set; }

        [ForeignKey("Order")]
        public Guid OrderId { get; set; } 
    }
}
