using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace CloakAndLedger.Domain.Models.Entities
{
    public class Politician
    {
        public int Id { get; set; }

        public required string FirstName { get; set; }

        public required string LastName { get; set; }
    }
}
