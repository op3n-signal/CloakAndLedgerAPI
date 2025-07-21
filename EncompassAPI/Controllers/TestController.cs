using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using EncompassAPI.Utility.Attributes;

namespace EncompassAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AuthKeyRequired] //TODO This attribute can be place on the controller, like it is, or place on individual methods.
    public class TestController : ControllerBase
    {
        [HttpGet("Test")]
        public ActionResult Test()
        {
            throw new NotImplementedException();    
        }
    }
}
