using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Text.Json.Serialization;

namespace CloakAndLedger.Domain.Models.Entities
{
    public class Politician
    {
        public int Id { get; set; }

        [JsonPropertyName("first_name")]
        public required string FirstName { get; set; }

        [JsonPropertyName("last_name")]
        public required string LastName { get; set; }

        public char? Party { get; set; }

        [JsonPropertyName("image_url")]
        public string? ImageUrl { get; set; }
    }
}
