using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Shared
{
    public class SecretService
    {
        public const string Region = "us-east-1";
        /*
         *	If you need more information about configurations or implementing the sample code, visit the AWS docs:
         *	https://aws.amazon.com/developer/language/net/getting-started
         */

        public static async Task<T> GetSecret<T>(string secretDirectory, string region = null)
        {
            //This should be the path to the secret or multiple secrets
            string secretName = secretDirectory;

            IAmazonSecretsManager client = new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(region ?? Region));

            GetSecretValueRequest request = new GetSecretValueRequest
            {
                SecretId = secretName,
                VersionStage = "AWSCURRENT", // VersionStage defaults to AWSCURRENT if unspecified.
            };

            GetSecretValueResponse response = await client.GetSecretValueAsync(request);

            if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception($"Error while fetching secret. Status Code: {response.HttpStatusCode}");
            }

            return JsonSerializer.Deserialize<T>(response.SecretString);
        }

        public static async Task<string> CreateSecret(string secretName, string secretString)
        {
            IAmazonSecretsManager client = new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(Region));

            CreateSecretRequest request = new CreateSecretRequest
            {
                Name = secretName,
                SecretString = secretString
            };

            CreateSecretResponse response = await client.CreateSecretAsync(request);

            if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception($"Error while fetching secret. Status Code: {response.HttpStatusCode}");
            }

            return response.ARN;
        }
    }
}
