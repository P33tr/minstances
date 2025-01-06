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
    private readonly ILogger<HomeController> _logger;
    private readonly IMinstancesRepository _minstancesRepository;
    private readonly IInstancesService _instancesService;
    private readonly IMastodonService _mastodonService;
    // A thread-safe collection to store connected clients

    // Method to broadcast messages to all connected clients

    private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    private static ConcurrentDictionary<string, SseClient> clients = new ConcurrentDictionary<string, SseClient>();

    public BskyEventController(
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
        finally
        {
            // Remove the client when the connection is closed
            clients.TryRemove(clientId, out _);
        }
    }



    public static async Task BroadcastMessageAsync(string message)
    {
        var data = $"event: newMessage\n";
        data += $"data: {message}\n\n";
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

    public async Task<IActionResult> IndexAsync()
    {
        try
        {
            double interval = 1000;
            BlueskyService blueskyService = new BlueskyService(interval);
            blueskyService.BlueskyEvent += OnBlueskyEventGeneratedAsync;

            blueskyService.StartTimer();
            Console.WriteLine("Timer started");

            return View();

        }
        finally
        {
           // Response.OnCompleted(async () => { await DoProcessing(); });
        }
    }

    private  async void OnBlueskyEventGeneratedAsync(object? sender, EventArgs e)
    {
        string senderAsString = ((int)sender).ToString();


        Debug.WriteLine($"Bluesky event generated {senderAsString}");

        await BroadcastMessageAsync($"<p>Bluesky event generated {senderAsString}</p>");

    }

    public async Task<IActionResult> DoProcessing()
    {
        var debugLog = new DebugLoggerProvider();
        var atProtocolBuilder = new ATWebSocketProtocolBuilder();
            // Defaults to bsky.network.
            //.WithInstanceUrl(new Uri("https://drasticactions.ninja"))
            //.WithLogger(debugLog.CreateLogger("FishyFlipDebug"));
        var atProtocol = atProtocolBuilder.Build();

        atProtocol.OnSubscribedRepoMessage += (sender, args) =>
        {
            Task.Run(() => HandleMessageAsync(args.Message));
        };

        await atProtocol.StartSubscribeReposAsync();

        // Delay for 20 seconds
        await Task.Delay(TimeSpan.FromSeconds(10));

        await atProtocol.StopSubscriptionAsync();



        return View();
    }

    async Task HandleMessageAsync(SubscribeRepoMessage message)
    {
        if (message.Commit is null)
        {
            return;
        }

        var orgId = message.Commit.Repo;

        if (orgId is null)
        {
            return;
        }

        if (message.Record is not null)
        {
            Console.WriteLine($"Record: {message.Record.Type}");
            switch(message.Record.Type)
            {
                case "app.bsky.feed.like":
                    Console.WriteLine("Like");
                    var like = (Like)message.Record;
                    break;
                case "app.bsky.feed.repost":
                    break;
                case "app.bsky.feed.post":
                    var post = (Post)message.Record;
                    var messageContent = $"<p>{post.Text}</p>";

                    // Check for embed with media
                    if (post.Embed != null)
                    {
                        var embeded = post.Embed;
                        if(embeded.Type == "app.bsky.embed.images")
                        {
                            
                            Console.WriteLine("Its an image");
                            var thing = (ImagesEmbed)embeded;
                            foreach (var image in thing.Images)
                            {
                                //Console.WriteLine(embeded.)
                                Console.WriteLine(message.Commit.Repo.Handler);
                                Console.WriteLine(message.Commit.Ops[0].Cid);
                                Console.WriteLine(message.Commit.Ops[0].Path);
                                Console.WriteLine(image.Image.Ref.Link);
                              //  messageContent += $"<img src=\"https://cdn.bsky.app/img/feed_fullsize/plain/{message.Commit.Repo.Handler}/{image.Image.Ref.Link}@jpeg\" />";
                            }

                            
                        }

                    }

                    await BroadcastMessageAsync(messageContent);
                    break;
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