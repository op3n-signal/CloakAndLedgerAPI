using CloakAndLedger.Domain.Models.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CloakAndLedger.Domain.Interfaces
{
    public interface IPoliticianRepo
    {
        Task<int> CreatePoliticianAsync(Politician politician);
        Task<Politician> GetPoliticianByIdAsync(int id);
        Task<int> UpdatePoliticianAsync(Politician politician);
        Task<int> DeletePoliticianAsync(int id);
    }
}
