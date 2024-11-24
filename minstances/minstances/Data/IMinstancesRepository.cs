using ErrorOr;
using minstances.Models;

namespace minstances.Data
{
    public interface IMinstancesRepository
    {
        Task<ErrorOr<Search>> CreateSearch(string searchTerm);
    }
}
