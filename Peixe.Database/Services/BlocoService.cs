using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Peixe.Database.Context;

namespace Peixe.Database.Services
{
    public class BlocoService(EPFDbContext context) : IBlocoService
    {
        private readonly EPFDbContext _context = context;

        public async Task<string> ListarBloco(uint idBloco, uint IdCiclo)
        {
            try
            {
                return await _context.Blocos
                    .Where(x => x.Id == idBloco && x.IdCiclo == IdCiclo)
                    .Select(x => x.Descricao)
                    .FirstOrDefaultAsync() ?? string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }
}
