using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetClinicSystem.Models
{
    [Table("drugs")]   // MySQL table name
    public class Drug
    {
        [Key]
        [Column("product_id")]
        public int DrugId { get; set; }

        [Column("name")]
        public string DrugName { get; set; } = string.Empty;

        [Column("quantity")]
        public int Quantity { get; set; }

        [Column("expiration_date")]
        public DateTime ExpiryDate { get; set; }

        [Column("unit_price")]
        public decimal UnitPrice { get; set; }

        [Column("dosage_type")]
        public string DosageType { get; set; } = string.Empty;
    }
}
