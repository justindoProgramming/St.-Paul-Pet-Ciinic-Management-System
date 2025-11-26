using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetClinicSystem.Models
{
    public class Grooming
    {
        [Key]
        public int GroomingId { get; set; }

        [ForeignKey("Pet")]
        public int PetId { get; set; }
        public Pet Pet { get; set; }

        [ForeignKey("Staff")]
        public int StaffId { get; set; }
        public Account Staff { get; set; }

        public string Service { get; set; }
        public DateTime? Date { get; set; }

        public string Notes { get; set; }
    }
}
