using EnRoute.Domain.Models;
using EnRoute.Domain;

namespace EnRoute.API.Controllers
{
    public class CellsController : ODataControllerBase<Cell>
    {
        public CellsController(ApplicationDbContext appDbContext) : base(appDbContext)
        {
        }
    }
}
