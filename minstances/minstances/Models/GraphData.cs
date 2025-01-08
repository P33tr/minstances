namespace minstances.Models;

public class GraphData
{
    public GraphData()
    {
        Nodes = new List<Node>();
        Links = new List<Link>();
    }

    public GraphData(List<Node> nodes, List<Link> links)
    {
        Nodes = nodes;
        Links = links;
    }

    public List<Node> Nodes { get; set; }
    public List<Link> Links { get; set; }
}
