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
using Microsoft.Extensions.Hosting;


namespace minstances.Controllers;

public class BskyEventController : Controller
{
    private readonly BlueskyService _blueskyService;

    private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    private static ConcurrentDictionary<string, SseClient> clients = new ConcurrentDictionary<string, SseClient>();

    public BskyEventController(IBlueskyService blueskyService)
    {
        _blueskyService = (BlueskyService)blueskyService;
    }

    // SSE endpoint method
    [HttpGet("/bsky-eventsse")]
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
        catch(TaskCanceledException)
        {
            // Handle the cancellation token
        }
        finally
        {
            // Remove the client when the connection is closed
            clients.TryRemove(clientId, out _);
        }
    }



    public static async Task BroadcastMessageAsync(GraphEvent graphEvent)
    {
        string data = string.Empty;
        switch (graphEvent.Type)
        {
            case "node":
                data = $"event: newNode\n";
                break;
            case "link":
                data = $"event: newLink\n";
                break;
        }
        data += $"data: {graphEvent.Message}\n\n";
        var bytes = Encoding.UTF8.GetBytes(data);

        foreach (var client in clients.Values)
        {
            try
            {
                await _semaphore.WaitAsync();
                try
                {
                    await client.Response.Body.WriteAsync(bytes, 0, bytes.Length);
                    await client.Response.Body.FlushAsync();
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch
            {
                // Handle exceptions (e.g., client disconnected)
            }
        }
    }
    public async Task<IActionResult> CreateTimer()
    {
        try
        {
            double interval = 1000;
            
            _blueskyService.CreateTimerEvent(interval);
            _blueskyService.BlueskyEvent += OnBlueskyEventGeneratedAsync;

            _blueskyService.StartTimer();
            Console.WriteLine("Timer started");
            return View("Index");
        }
        finally
        {
            // Response.OnCompleted(async () => { await DoProcessing(); });
        }
    }

    public async Task<IActionResult> ProcessLikes()
    {
        _blueskyService.MessageFromBlueskyEvent += OnMessageFromBlueskyEventAsync;
       await  _blueskyService.DoProcessing();
        return NoContent();

    }
    public async Task<IActionResult> IndexAsync()
    {
        try
        {
            return View();
        }
        finally
        {
           // Response.OnCompleted(async () => { await DoProcessing(); });
        }
    }

    private async void OnBlueskyEventGeneratedAsync(object? sender, EventArgs e)
    {
        GraphEvent graphEvent = (GraphEvent)sender;

        await BroadcastMessageAsync(graphEvent);

    }
    private  async void OnMessageFromBlueskyEventAsync(object? sender, EventArgs e)
    {
        GraphEvent graphEvent = (GraphEvent)sender;

        await BroadcastMessageAsync(graphEvent);

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