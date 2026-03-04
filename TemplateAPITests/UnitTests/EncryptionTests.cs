using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace TemplateTests.UnitTests
{
    public class EncryptionTests
    {
        [Fact]
        public void TestEncryption()
        {
            var salt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
            var signing = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            var encryption = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        }
    }
}
