using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace OpenFaaS
{
    [ApiController]
    [Route("/")]
    public class Controller : ControllerBase
    {
        [HttpGet]
        public Task<IActionResult> GetAsync()
        {
            var result = new
            {
                Message = "Hello!"
            };

            return Task.FromResult<IActionResult>( Ok( result ) );
        }
    }
}
