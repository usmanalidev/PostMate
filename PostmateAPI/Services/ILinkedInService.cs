using PostmateAPI.Models;

namespace PostmateAPI.Services
{
    public interface ILinkedInService
    {
        Task<bool> PostToLinkedInAsync(Post post);
    }
}
