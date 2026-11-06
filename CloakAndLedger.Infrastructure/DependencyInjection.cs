using CloakAndLedger.Domain.Interfaces;
using CloakAndLedger.Infrastructure.Db;
using CloakAndLedger.Infrastructure.Repos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using Dapper;

namespace CloakAndLedger.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("PostgressConnection");
            services.AddSingleton<DapperDbContext>();
            services.AddScoped<IDataRepo, DataRepo>();
            services.AddScoped<IPoliticianRepo, PoliticianRepo>();

            DefaultTypeMap.MatchNamesWithUnderscores = true;
            return services; // Returns it so you can chain methods if you want
        }
    }
}
