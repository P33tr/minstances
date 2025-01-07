namespace minstances.Models
{
    public class GraphEvent
    {
        public GraphEvent(string type, string message)
        {
            Type = type;
            Message = message;
        }
        public string Type { get; set; }
        public string Message { get; set; }
    }
}
