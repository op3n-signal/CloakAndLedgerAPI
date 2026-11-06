using CloakAndLedger.Domain.Interfaces;
using CloakAndLedger.Domain.Models.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CloakAndLedger.Infrastructure.Repos
{
    public class PoliticianRepo : IPoliticianRepo
    {
        private readonly IDataRepo _dataRepo;

        public PoliticianRepo(IDataRepo dataRepo)
        {
            _dataRepo = dataRepo;
        }

        public async Task<int> CreatePoliticianAsync(Politician politician)
        {
            return await _dataRepo.Execute("INSERT INTO politics.Politicians (first_name, last_name) VALUES (@FirstName, @LastName); SELECT CAST(SCOPE_IDENTITY() as int);", politician);
        }
        public async Task<int> DeletePoliticianAsync(int id)
        {
            return await _dataRepo.Execute("DELETE FROM Politicians WHERE Id = @Id", new { Id = id });
        }
        public async Task<Politician> GetPoliticianByIdAsync(int id)
        {
            return await _dataRepo.SelectSingle<Politician>("SELECT * FROM Politicians WHERE Id = @Id", new { Id = id });
        }
        public async Task<int> UpdatePoliticianAsync(Politician politician)
        {
            return await _dataRepo.Execute("UPDATE Politicians SET first_name = @FirstName, last_name = @LastName WHERE Id = @Id", politician);
        }
    }
}
