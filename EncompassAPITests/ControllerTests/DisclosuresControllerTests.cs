using Amazon.Runtime.Internal.Util;
using EncompassAPI.AppLayer;
using EncompassAPI.Controllers;
using EncompassAPI.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EncompassAPITests.ControllerTests
{
    public class DisclosuresControllerTests : TestHelperBase
    {
        DisclosuresController thisController;

        public DisclosuresControllerTests()
        {
            ILogger<FileService> logger = LoggerFactory.CreateLogger<FileService>();
            LoggerService<FileService> loggerService = new LoggerService<FileService>(logger, GetLoggingOptions());

            ILogger<HttpService> httpLogger = LoggerFactory.CreateLogger<HttpService>();
            IHttpService httpService = new HttpService(httpLogger);

            IFileService fileService = new FileService(httpService, loggerService, GetFileOptions());

            IDisclosuresService disclosuresService = new DisclosuresService(fileService);

            thisController = new DisclosuresController(disclosuresService);
        }

        [Fact]
        public async Task Test()
        {
            await thisController.Test();    
        }
    }
}
