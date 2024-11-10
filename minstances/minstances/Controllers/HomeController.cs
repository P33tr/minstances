using ErrorOr;
using Microsoft.AspNetCore.Mvc;
using minstances.Models;
using minstances.Services;
using System.Diagnostics;
using System.Threading;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.SignalR;
using minstances.Hubs;

namespace minstances.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IInstancesService _instancesService;
        private readonly IMastodonService _mastodonService;
        
        private readonly IHubContext<StatusHub> _hub;

        public HomeController(IHubContext<StatusHub> hub,IInstancesService instancesService, 
            IMastodonService mastodonService,
            ILogger<HomeController> logger)
        {
            _hub = hub;
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
        public async Task<IActionResult> Signal()
        {
            for (int i = 0; i < 100; i++)
            {

                _hub.Clients.All.SendAsync("ReceiveMessage", "dinky do");
            }

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
        public async Task<IActionResult> ListInstancesAsync()
        {
            InstancesVm instances = new InstancesVm();

            ErrorOr<InstX> result = await _instancesService.ListAsync("active_users", "desc");
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

        public async Task<IActionResult> SearchInstances(string searchTerm)
        {
            try
            {
                InstanceStatusVm instanceStatusVm = new InstanceStatusVm();
                return View("InstanceStatuses", instanceStatusVm);
            }
            finally
            {
                Response.OnCompleted(async () => { await DoProcessing(searchTerm); });
            }
        }

        private async Task DoProcessing(string searchTerm)
        {

        // get the list of instances with the most active users
            InstancesVm instances = new InstancesVm();

            ErrorOr<InstX> result = await _instancesService.ListAsync("active_users", "desc");
            if (result.IsError)
            {
                instances.Error = result.Errors[0].Code;
                Response.Redirect("Index");
            }
            else
            {
                instances.Instances = result.Value;

                // here is an array of tasks we want to perform
                var instanceTaskList = new List<Task<ErrorOr<List<Models.Status>>>>();
                
                foreach (var instance in instances.Instances.instances)
                {
                    instanceTaskList.Add(Task.Run(() =>_mastodonService.SearchAsync(instance.name, searchTerm)));
                }
                InstanceStatusVm instanceStatusVm = new InstanceStatusVm();
                while (instanceTaskList.Any())
                {
                    var completedTask = await Task.WhenAny(instanceTaskList);
                    instanceTaskList.Remove(completedTask);
                    ErrorOr<List<Models.Status>> result2 = await completedTask;
                    Task.Run(() => HandleResult(instanceStatusVm, result2));
                }
            }

        }

        private void HandleResult(InstanceStatusVm vm,  ErrorOr<List<Status>> result2)
        {
            InstanceStatuss instanceStatus = new InstanceStatuss();
            List<Models.Status> resultOfCall = new List<Models.Status>();
            if (result2.IsError)
            {
                vm.Error = result2.Errors[0].Code;
            }
            else
            {
                resultOfCall = result2.Value;
                instanceStatus.Statuses = resultOfCall.ToArray();
                //vm.InstanceStatuses.Add(instanceStatus);
                _hub.Clients.All.SendAsync("ReceiveMessage", instanceStatus);
            }
        }
    }
    // GET: Home/ListInstances

    
}
