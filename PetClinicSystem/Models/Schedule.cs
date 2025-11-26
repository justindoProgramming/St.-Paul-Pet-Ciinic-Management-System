using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetClinicSystem.Models
{
    [Table("schedule")]
    public class Schedule
    {
        [Key]
        [Column("schedule_id")]
        public int ScheduleId { get; set; }

        // Foreign key → pets table
        [Column("pet_id")]
        public int PetId { get; set; }

        [ForeignKey("PetId")]
        public Pet? Pet { get; set; }     // <-- FIXED (nullable)

        // Foreign key → accounts table (staff)
        [Column("staff_id")]
        public int StaffId { get; set; }

        [ForeignKey("StaffId")]
        public Account? Staff { get; set; }   // <-- FIXED (nullable)

        // Service Name
        [Column("service")]
        public string? Service { get; set; }

        [Column("schedule_dateold")]
        public DateTime? ScheduleDateOld { get; set; }

        [Column("schedule_time")]
        public TimeSpan? ScheduleTime { get; set; }

        [Column("status")]
        public string? Status { get; set; }
    }
}
