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
using FishyFlip.Models;
using FishyFlip;
using Microsoft.Extensions.Logging.Debug;
using System.Text.Json;
using Google.Protobuf.Collections;


namespace minstances.Controllers;

public partial class GraphController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IMinstancesRepository _minstancesRepository;
    private readonly IInstancesService _instancesService;
    private readonly IMastodonService _mastodonService;
    // A thread-safe collection to store connected clients

    private static ConcurrentDictionary<string, SseClient> clients = new ConcurrentDictionary<string, SseClient>();

    public GraphController(
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

    //// SSE endpoint method
    [HttpGet("/get-graph-data")]
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
        await DoProcessing();
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

    private async Task<List<Instance>> GetInstances()
    {
        InstancesVm instances = new InstancesVm();

        ErrorOr<InstX> result = await _instancesService.ListAsync("active_users", "desc");
        if (result.IsError)
        {
            throw new Exception("Failed in GetInstances.");
        }
        else
        {
            return result.Value.instances.ToList();
        }
        
    }

    //// Method to broadcast messages to all connected clients
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
        try
        {

            return View();

        }
        finally
        {
            //Response.OnCompleted(async () => { await DoProcessing(); });
        }
    }

    public async Task<IActionResult> DoProcessing()
    {
        Thread.Sleep(1000);
        List<Node> nodes = new List<Node>();

        List<Instance> instances = await GetInstances();


        List<Link> links = new List<Link>();
        //links.Add(new Link { source = "node1", target = "node2" });
        //links.Add(new Link { source = "node4", target = "node2" });
        //links.Add(new Link { source = "node5", target = "node2" });
            
        foreach(var instance in instances)
        {
            nodes.Add(new Node { id = instance.name, group = 1, img=instance.thumbnail });
        }

        GraphData graphData = new GraphData(nodes, links);

        string jsonString = JsonSerializer.Serialize(graphData);

        await BroadcastMessageAsync(jsonString);



        return View();
    }

    //async Task HandleMessageAsync(SubscribeRepoMessage message)
    //{
    //    if (message.Commit is null)
    //    {
    //        return;
    //    }

    //    var orgId = message.Commit.Repo;

    //    if (orgId is null)
    //    {
    //        return;
    //    }

    //    if (message.Record is not null)
    //    {
    //        Console.WriteLine($"Record: {message.Record.Type}");
    //        switch(message.Record.Type)
    //        {
    //            case "app.bsky.feed.like":
    //            case "app.bsky.feed.repost":
    //                break;
    //            case "app.bsky.feed.post":
    //                await BroadcastMessageAsync($"<p>{((Post)message.Record).Text}</p>");
    //                break;
    //        }

    //    }
    //}


    //// Method to render the message as HTML
    //private Tuple<string, List<string>> RenderMessageHtml(Status status)
    //{
    //    if (status.content.Contains("img"))
    //    {
    //        //there is an image
    //        Console.WriteLine(status.content);
    //    }
    //    StringBuilder statusDisplayBuilder = new("<div class=\"status-box\">");
    //    statusDisplayBuilder.Append($"<p><img class=\"avatar\" src=\"{status.account.avatar}\" width=\"40\" height=\"40\"/>");
    //    statusDisplayBuilder.Append($"<span> {status.account.display_name} </span> ");
    //    statusDisplayBuilder.Append($" <div>Created: {status.created_at}</div>");

    //    // the content is hrml so lets parse it and get the hashtags
    //    var contentDocument = new HtmlAgilityPack.HtmlDocument();
    //    contentDocument.LoadHtml(status.content);

    //    //   //a[@class='mention hashtag']
    //    List<string> links = new List<string>();
    //    var nodes = contentDocument.DocumentNode.SelectNodes("//a[@class='mention hashtag']");
    //    if (nodes != null)
    //    {

    //        foreach (HtmlNode? node in nodes)
    //        {
    //            links.Add(node.InnerText);
    //        }
    //    }
    //    //var hashLinks =contentDocument.DocumentNode.Descendants("a")
    //    //    .Select(y => y.Descendants()
    //    //        .Where(x => x.InnerText.Contains("#")))   
    //    //        .ToList();
    //    //List<string> links = new List<string>();
    //    //foreach (var hashLink in hashLinks)
    //    //{
    //    //    links.Add(hashLink.ToString());
    //    //}

    //    statusDisplayBuilder.Append($" <div>{status.content}</div>");
    //    foreach(var mediaItem in status.media_attachments)
    //    {
    //        statusDisplayBuilder.Append($"<span>{mediaItem.description}</span>");
    //        statusDisplayBuilder.Append($"<img src='{mediaItem.preview_url}'/>");
    //    }
    //    statusDisplayBuilder.Append(" </p></div>");
    //    return new Tuple<string, List<string>>(statusDisplayBuilder.ToString(), links);
    //}
}


// GET: Home/ListInstances