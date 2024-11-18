using System.Collections.Concurrent;
using ErrorOr;
using Microsoft.AspNetCore.Mvc;
using minstances.Models;
using minstances.Services;
using System.Diagnostics;
using System.Text;


namespace minstances.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IInstancesService _instancesService;
    private readonly IMastodonService _mastodonService;
    // A thread-safe collection to store connected clients
        
    private static ConcurrentDictionary<string, SseClient> clients = new ConcurrentDictionary<string, SseClient>();

    public HomeController(IInstancesService instancesService, 
        IMastodonService mastodonService,
        ILogger<HomeController> logger)
    {
        _instancesService = instancesService;
        _mastodonService = mastodonService;
        _logger = logger;
    }
    
    // SSE endpoint method
    [HttpGet("/sse")]
    public async Task Sse()
    {
        var clientId = Guid.NewGuid().ToString();

        // Set headers for SSE
        Response.Headers.TryAdd("Cache-Control", "no-cache");
        Response.Headers.TryAdd("Content-Type", "text/event-stream");
        Response.Headers.TryAdd("Connection", "keep-alive");

        // Create a new client
        var client = new SseClient
        {
            ClientId = clientId,
            Response = Response
        };

        // Add the client to the collection
        clients.TryAdd(clientId, client);

        try
        {
            // Keep the connection open indefinitely
            await Task.Delay(Timeout.Infinite, HttpContext.RequestAborted);
        }
        finally
        {
            // Remove the client when the connection is closed
            clients.TryRemove(clientId, out _);
        }
    }
    
    // Method to broadcast messages to all connected clients
    public static async Task BroadcastMessageAsync(string message)
    {
        var data = $"event: newMessage\n";
        data += $"data: {message}\n\n";
        var bytes = Encoding.UTF8.GetBytes(data);

        foreach (var client in clients.Values)
        {
            try
            {
                await client.Response.Body.WriteAsync(bytes, 0, bytes.Length);
                await client.Response.Body.FlushAsync();
            }
            catch
            {
                // Handle exceptions (e.g., client disconnected)
            }
        }
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

    private async void HandleResult(InstanceStatusVm vm,  ErrorOr<List<Status>> result2)
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
            // broadcast update here
            // Broadcast the new message

            foreach (var s in instanceStatus.Statuses)
            {
                            var messageHtml = RenderMessageHtml(s);
                            await BroadcastMessageAsync(messageHtml);
            }

        }
    }
    // Method to render the message as HTML
    private string RenderMessageHtml(Status status)
    {
        return $"<div class=\"message\"><strong>{status.id}:</strong> {status.content}</div>";
    }
}
// Helper class to represent an SSE client
public class SseClient
{
    public string ClientId { get; set; }
    public HttpResponse Response { get; set; }
}


// GET: Home/ListInstances