using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace CloakAndLedger.Domain.Models.Entities
{
    [Table("Politicians")]
    internal class Politician
    {
        public int Id { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(60)")]
        public required string FirstName { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(60)")]
        public required string LastName { get; set; }
    }
}
