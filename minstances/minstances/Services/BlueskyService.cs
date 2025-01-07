using FishyFlip;
using FishyFlip.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Debug;
using minstances.Models;
using System.Diagnostics.Metrics;
using System.Text.Json;
using System.Timers;

namespace minstances.Services;

public interface IBlueskyService
{
    event EventHandler<EventArgs> BlueskyEvent;
    void CreateTimerEvent(double interval);
    void StartTimer();
    void StopTimer();
}

public class BlueskyService : IBlueskyService
{   
    private System.Timers.Timer _timer;

    // define the event
    public event EventHandler<EventArgs> BlueskyEvent;

    public event EventHandler<EventArgs> MessageFromBlueskyEvent;

    private int _counter = 0;

    private List<string> _uriList = new List<string>();

    public BlueskyService()
    {

    }

    public void CreateTimerEvent(double interval)
    {
        _timer = new System.Timers.Timer(interval);
        _timer.Elapsed += OnTimedEvent;
        _timer.AutoReset = true;
        _timer.Enabled = true;
    }

    public void StartTimer()
    {
        _timer.Start();

    }

    public void StopTimer()
    {
        _timer.Stop();
    }

    private void OnTimedEvent(object source, ElapsedEventArgs e)
    {
        _counter++;
        // Raise the event
        BlueskyEvent?.Invoke(_counter, e);
    }

    public async Task DoProcessing()
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
        await Task.Delay(TimeSpan.FromSeconds(2));

        await atProtocol.StopSubscriptionAsync();
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
            switch (message.Record.Type)
            {
                case "app.bsky.feed.like":

              
                    var like = (Like)message.Record;
                    var likeMessage = $"<p>{like.Subject.Cid}</p>";
                    likeMessage += $"<p>{like.Subject.Uri}</p>";
                    likeMessage += $"<p>{like.CreatedAt}</p>";
                    likeMessage += $"<p>{like.Type}</p>";
                    Node node = new Node { id = like.Subject.Cid, group = 1 };

                    string jsonString = JsonSerializer.Serialize(node);

                    MessageFromBlueskyEvent?.Invoke(new GraphEvent("node", jsonString), new EventArgs());

                    // only add a new uri if we dont already have it
                    if (!_uriList.Contains(like.Subject.Uri.ToString()))
                    {
                        _uriList.Add(like.Subject.Uri.ToString());
                        Node uriNode = new Node { id = like.Subject.Uri.ToString(), group = 2 };
                        string jsonStringForUriNode = JsonSerializer.Serialize(uriNode);
                        MessageFromBlueskyEvent?.Invoke(new GraphEvent("node", jsonStringForUriNode), new EventArgs());

                        Link link = new Link { source = like.Subject.Cid, target = like.Subject.Uri.ToString() };
                        string jsonStringForLink = JsonSerializer.Serialize(link);
                        
                        MessageFromBlueskyEvent?.Invoke(new GraphEvent("link", jsonStringForLink), new EventArgs());
                    }

                    break;
                case "app.bsky.feed.repost":
                    break;
                case "app.bsky.feed.post":
                    var post = (Post)message.Record;
                    var messageContent = $"<p>{post.Text}</p>";

                    // Check for embed with media
                    //if (post.Embed != null)
                    //{
                    //    var embeded = post.Embed;
                    //    if (embeded.Type == "app.bsky.embed.images")
                    //    {

                    //        Console.WriteLine("Its an image");
                    //        var thing = (ImagesEmbed)embeded;
                    //        foreach (var image in thing.Images)
                    //        {
                    //            //Console.WriteLine(embeded.)
                    //            Console.WriteLine(message.Commit.Repo.Handler);
                    //            Console.WriteLine(message.Commit.Ops[0].Cid);
                    //            Console.WriteLine(message.Commit.Ops[0].Path);
                    //            Console.WriteLine(image.Image.Ref.Link);
                    //            messageContent += $"<img src=\"https://cdn.bsky.app/img/feed_fullsize/plain/{message.Commit.Repo.Handler}/{image.Image.Ref.Link}@jpeg\" />";
                    //        }
                    //    }
                    //}
                    // stop this processing MessageFromBlueskyEvent?.Invoke(messageContent, new EventArgs());                    
                    break;
            }

        }
    }
}
