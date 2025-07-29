using EncompassAPI.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared;
using Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace EncompassTokenGenerator
{
    /// <summary>
    /// Generates the encompass token
    /// </summary>
    internal class EncompassTokenGenerator
    {
        static void Main(string[] args)
        {
            var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
               ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            string path = string.Format(SettingsController.EncompassCredentialsSecretPath, environment, orgId);

            SecretService.GetSecret<EncompassCredentials>();
        }
    }
}
