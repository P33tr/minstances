using System.Collections.Concurrent;
using ErrorOr;
using Microsoft.AspNetCore.Mvc;
using minstances.Models;
using minstances.Services;
using System.Diagnostics;
using System.Text;
using minstances.Data;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Linq.Expressions;
using HtmlAgilityPack;


namespace minstances.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IMinstancesRepository _minstancesRepository;
        private readonly IInstancesService _instancesService;
        private readonly IMastodonService _mastodonService;
        // A thread-safe collection to store connected clients

        private static ConcurrentDictionary<string, SseClient> clients = new ConcurrentDictionary<string, SseClient>();

        public HomeController(
            IMinstancesRepository minstancesRepository,
            IInstancesService instancesService,
            IMastodonService mastodonService,
            ILogger<HomeController> logger)
        {
            _minstancesRepository = minstancesRepository;
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

        public static async Task BroadcastTagAsync(string tag)
        {

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append($"<li><a href='#'>{tag}</a></li>");
            var data = $"event: newTag\n";
            data += $"data: {stringBuilder.ToString()}\n\n";
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

        // Method to send instance total count
        public static async Task InstanceCountTotalAsync(string count)
        {
            var data = $"event: instanceCount\n";
            data += $"data: {count}\n\n";
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

        public async Task<IActionResult> SearchInstances(string searchTerm, string dateRange)
        {
            try
            {
                // record the search in the db
                _ = await _minstancesRepository.CreateSearch(searchTerm);
                // I dont care about the result for now;

                InstanceStatusVm instanceStatusVm = new InstanceStatusVm();
                return View("InstanceStatuses", instanceStatusVm);

            }
            finally
            {
                List<string> contentStart = new List<string>();
                Response.OnCompleted(async () => { await DoProcessing(searchTerm, dateRange, contentStart); });
            }
        }

        private async Task DoProcessing(string searchTerm, string dateRange, List<string> contentStart)
        {

            // get the list of instances with the most active users
            InstancesVm instances = new InstancesVm();

            DateTime startDate; 
            switch (dateRange)
            {
                case "Today": startDate = DateTime.Today; break;
                case "LastWeek": startDate = DateTime.Today.AddDays(-7); break;
                case "LastMonth": startDate = DateTime.Today.AddMonths(-1); break;
                default:
                    startDate = DateTime.MinValue; break;
            }
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
                    instanceTaskList.Add(Task.Run(() => _mastodonService.SearchAsync(instance.name, searchTerm)));
                }

                // broadcast the instance count.
                int totalInstances = instanceTaskList.Count();
                await InstanceCountTotalAsync($"Instance Count {totalInstances}/{instanceTaskList.Count().ToString()}");

                InstanceStatusVm instanceStatusVm = new InstanceStatusVm();
                while (instanceTaskList.Any())
                {
                    var completedTask = await Task.WhenAny(instanceTaskList);
                    instanceTaskList.Remove(completedTask);
                    ErrorOr<List<Models.Status>> result2 = await completedTask;
                    await InstanceCountTotalAsync($"Instance Count {totalInstances}/{instanceTaskList.Count().ToString()}");
                    Task.Run(() => HandleResult(instanceStatusVm, result2, contentStart, startDate));
                }
            }

        }

        private async void HandleResult(InstanceStatusVm vm, ErrorOr<List<Status>> result2, List<string> contentStart, DateTime startDate)
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
                    if (contentStart.Contains(s.content.Substring(0, 10)))
                    {
                        continue;
                    }
                    else
                    {
                        if (s.created_at.CompareTo(startDate) == 1)
                        {
                            contentStart.Add(s.content.Substring(0, 10));
                            Tuple<string, List<string>> messageHtml = RenderMessageHtml(s);
                            await BroadcastMessageAsync(messageHtml.Item1);
                            foreach (string aTag in messageHtml.Item2)
                            {
                                await BroadcastTagAsync(aTag);
                            }
                        }
                    }
                }

            }
        }
        // Method to render the message as HTML
        private Tuple<string, List<string>> RenderMessageHtml(Status status)
        {
            if (status.content.Contains("img"))
            {
                //there is an image
                Console.WriteLine(status.content);
            }
            StringBuilder statusDisplayBuilder = new("<div class=\"status-box\">");
            statusDisplayBuilder.Append($"<p><img class=\"avatar\" src=\"{status.account.avatar}\" width=\"40\" height=\"40\"/>");
            statusDisplayBuilder.Append($"<span> {status.account.display_name} </span> ");
            statusDisplayBuilder.Append($" <div>Created: {status.created_at}</div>");

            // the content is hrml so lets parse it and get the hashtags
            var contentDocument = new HtmlAgilityPack.HtmlDocument();
            contentDocument.LoadHtml(status.content);

            //   //a[@class='mention hashtag']
            List<string> links = new List<string>();
            var nodes = contentDocument.DocumentNode.SelectNodes("//a[@class='mention hashtag']");
            if (nodes != null)
            {

                foreach (HtmlNode? node in nodes)
                {
                    links.Add(node.InnerText);
                }
            }
            //var hashLinks =contentDocument.DocumentNode.Descendants("a")
            //    .Select(y => y.Descendants()
            //        .Where(x => x.InnerText.Contains("#")))   
            //        .ToList();
            //List<string> links = new List<string>();
            //foreach (var hashLink in hashLinks)
            //{
            //    links.Add(hashLink.ToString());
            //}

            statusDisplayBuilder.Append($" <div>{status.content}</div>");
            foreach(var mediaItem in status.media_attachments)
            {
                statusDisplayBuilder.Append($"<span>{mediaItem.description}</span>");
                statusDisplayBuilder.Append($"<img src='{mediaItem.preview_url}'/>");
            }
            statusDisplayBuilder.Append(" </p></div>");
            return new Tuple<string, List<string>>(statusDisplayBuilder.ToString(), links);
        }
    }


    // GET: Home/ListInstances
}