using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetClinicSystem.Models
{
    [Table("drugs")]
    public class Drug
    {
        [Key]
        [Column("product_id")]
        public int DrugId { get; set; }

        [Column("name")]
        [Required]
        public string DrugName { get; set; } = string.Empty;

        [Column("quantity")]
        [Range(0, int.MaxValue)]
        public int Quantity { get; set; }

        [Column("expiration_date")]
        public DateTime ExpiryDate { get; set; }

        [Column("unit_price")]
        [Range(0, double.MaxValue)]
        public decimal UnitPrice { get; set; }

        [Column("dosage_type")]
        public string DosageType { get; set; } = string.Empty;

        // NEW COLUMNS
        [Column("base_price")]
        public decimal? BasePrice { get; set; }

        [Column("restock_notes")]
        public string? RestockNotes { get; set; }

        [Column("date_added")]
        public DateTime? DateAdded { get; set; }
    }
}
