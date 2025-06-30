using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace TransactionService.Infrastructure.Helpers
{
    public static class SqlExceptionHelper
    {
        public static bool IsPkOrUniqueViolation(DbUpdateException ex)
        {
            // 2601 = unique index, 2627 = PK duplicate
            return ex.InnerException is SqlException sql && 
                   (sql.Number == 2601 || sql.Number == 2627);
        }
    }
}
