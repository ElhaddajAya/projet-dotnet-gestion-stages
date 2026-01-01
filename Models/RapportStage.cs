namespace GestionStages.Models
{
    public class RapportStage
    {
        public int Id { get; set; }
        public string Titre { get; set; } = string.Empty;
        public string NomFichier { get; set; } = string.Empty;
        public DateTime DateDepot { get; set; } = DateTime.Now;

        // Clé étrangère vers Convention
        public int ConventionId { get; set; }

        // Navigation
        public virtual Convention Convention { get; set; } = null!;
    }
}
