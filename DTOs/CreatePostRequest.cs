namespace PostmateAPI.DTOs
{
    public class CreatePostRequest
    {
        public string Topic { get; set; } = string.Empty;
        public string PostType { get; set; } = "educational"; // educational, listicle, storytelling, thought-leadership, interview, difference
    }
}
