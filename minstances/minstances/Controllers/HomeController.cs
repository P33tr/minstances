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
        private readonly IMastodonService _mastodonService;

        public HomeController(IInstancesService instancesService, 
            IMastodonService mastodonService,
            ILogger<HomeController> logger)
        {
            _instancesService = instancesService;
            _mastodonService = mastodonService;
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
        
        [HttpGet]
        public async Task<IActionResult> PostsAsync(string instance)
        {
            StatusVm statuses = new StatusVm();
            List<Models.Status> resultOfCall = new List<Models.Status>();
            ErrorOr<List<Models.Status>> result = await _mastodonService.GetStatusesAsync(instance);
            if (result.IsError)
            {
                statuses.Error = result.Errors[0].Code;
            }
            else
            {
                resultOfCall = result.Value;
                statuses.Statuses = resultOfCall.ToArray();
            }

            return View("Statuses", statuses);
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
