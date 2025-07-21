using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using System.Text.Json;

namespace EncompassAPI.Services
{
    public class AWSService
    {
        /*
         *	If you need more information about configurations or implementing the sample code, visit the AWS docs:
         *	https://aws.amazon.com/developer/language/net/getting-started
         */

        public static async Task<T> GetSecret<T>(string secretDirectory)
        {
            //This should be the path to the secret or multiple secrets
            string secretName = secretDirectory;
            string region = "us-west-2";

            IAmazonSecretsManager client = new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(region));

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
    }

    public class SecretsManagerRPVToken
    {
        public string RPV_Token { get; set; }
    }
}
