using ErrorOr;
using Microsoft.AspNetCore.Mvc;
using minstances.Models;
using minstances.Services;
using System.Diagnostics;

namespace minstances.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IInstancesService _instancesService;

        public HomeController(IInstancesService instancesService, ILogger<HomeController> logger)
        {
            _instancesService = instancesService;
            _logger = logger;
        }

        public async Task<IActionResult> IndexAsync()
        {
            InstancesService instancesService = new InstancesService();
            await instancesService.GetAsync();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> InstancesAsync()
        {
            InstancesVm instances = new InstancesVm();

            ErrorOr<InstX> result = await _instancesService.GetAsync();
            if (result.IsError)
            {
                instances.Error = result.Errors[0].Code;
            }
            else
            {
                instances.Instances = result.Value;
            }

            return View("instances", instances);
        }

            public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
