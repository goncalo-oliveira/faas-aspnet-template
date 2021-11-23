using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace OpenFaaS
{
    [ApiController]
    [Route("/")]
    public class Function : ControllerBase
    {
        [HttpGet]
        [HttpPost]
        public Task<IActionResult> ExecuteAsync()
        {
            var result = new
            {
                Message = "Hello!"
            };

            return Task.FromResult<IActionResult>( Ok( result ) );
        }
    }
}
