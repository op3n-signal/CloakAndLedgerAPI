using CloakAndLedger.Domain.Models.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CloakAndLedger.Domain.Interfaces
{
    public interface IPoliticianService
    {
        Task<bool> CreatePoliticianAsync(Politician politician);
        Task<Politician> GetPoliticianByIdAsync(int id);
        Task<bool> UpdatePoliticianAsync(Politician politician);
        Task<bool> DeletePoliticianAsync(int id);
    }
}
