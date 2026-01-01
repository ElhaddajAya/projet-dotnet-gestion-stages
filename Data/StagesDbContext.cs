using GestionStages.Models;
using Microsoft.EntityFrameworkCore;

namespace GestionStages.Data
{
    /*
        DbSet<T> : chaque DbSet représente une table dans la base de données
        Etudiants → table des étudiants
        Entreprises → table des entreprises
        etc.
    */
    public class StagesDbContext : DbContext
    {
        public StagesDbContext(DbContextOptions<StagesDbContext> options)
            : base(options)
        {
        }

        // Nos tables de données
        public DbSet<Etudiant> Etudiants { get; set; }
        public DbSet<Entreprise> Entreprises { get; set; }
        public DbSet<OffresStage> OffresStages { get; set; }
        public DbSet<Candidature> Candidatures { get; set; }
        public DbSet<Convention> Conventions { get; set; }
        public DbSet<RapportStage> RapportsStages { get; set; }
    }
}
