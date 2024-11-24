using ErrorOr;
using Microsoft.EntityFrameworkCore;
using minstances.Models;

namespace minstances.Data
{
    public class MinstancesRepository : IMinstancesRepository
    {
        private  DbContext _context {  get; set; }
        public MinstancesRepository(MinstancesContext minstancesContext) 
        {
            _context = minstancesContext;
        }
        public async Task<ErrorOr<Search>> CreateSearch(string searchTerm)
        {
            Search search = new Search();   
            search.SearchTerm = searchTerm;
            search.Created = DateTime.Now;
            _context.Add(search);
            await _context.SaveChangesAsync();
            return search;
        }
    }
}
