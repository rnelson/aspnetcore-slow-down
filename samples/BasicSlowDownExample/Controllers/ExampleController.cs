using Microsoft.AspNetCore.Mvc;

namespace BasicSlowDownExample.Controllers;

[Controller]
[Route("[controller]")]
public class ExampleController : Controller
{
    private readonly Random _random = new();
    
    [HttpGet("number")]
    public IActionResult Number()
    {
        return Ok(_random.NextInt64());
    }
}
