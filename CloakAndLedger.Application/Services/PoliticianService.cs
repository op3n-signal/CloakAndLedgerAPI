using CloakAndLedger.Domain.Interfaces;
using CloakAndLedger.Domain.Models.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CloakAndLedger.Application.Services
{
    internal class PoliticianService : IPoliticianService
    {
        private readonly IPoliticianRepo _politicianRepo;

        public PoliticianService(IPoliticianRepo politicianRepo)
        {
            _politicianRepo = politicianRepo;
        }

        public async Task<bool> CreatePoliticianAsync(Politician politician)
        {
            var result = await _politicianRepo.CreatePoliticianAsync(politician);
            return result > 0;
        }

        public async Task<bool> DeletePoliticianAsync(int id)
        {
            var result = await _politicianRepo.DeletePoliticianAsync(id);
            return result > 0;
        }

        public async Task<Politician> GetPoliticianByIdAsync(int id)
        {
            return await _politicianRepo.GetPoliticianByIdAsync(id);
        }

        public async Task<bool> UpdatePoliticianAsync(Politician politician)
        {
            var result = await _politicianRepo.UpdatePoliticianAsync(politician);
            return result > 0;
        }
    }
}
