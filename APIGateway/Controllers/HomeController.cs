using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace APIGateway.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HomeController : ControllerBase
    {
        [Obsolete]
        public readonly IHostingEnvironment _hostingEnv;

        [Obsolete]
        public HomeController(IHostingEnvironment hostingEnv)
        {
            _hostingEnv = hostingEnv;
        }
        [HttpGet]
        [Obsolete]
        public string Get()
        {
            return "Catalog Start Successfully. Environment - " + _hostingEnv.EnvironmentName + "";
        }
    }
}
