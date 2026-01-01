using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GestionStages.Data
{
    /*
        IdentityDbContext : classe de base fournie par ASP.NET Identity
        Elle va créer automatiquement les tables : AspNetUsers, AspNetRoles, etc.
    */
    public class AuthDbContext : IdentityDbContext
    {
        public AuthDbContext(DbContextOptions<AuthDbContext> options)
            : base(options)
        {
        }
    }
}
