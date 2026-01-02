namespace GestionStages.Models
{
    public class Candidature
    {
        public int Id { get; set; }
        public DateTime DateCandidature { get; set; } = DateTime.Now;
        public string Statut { get; set; } = "En attente"; // En attente, Acceptée, Refusée
        public string? CheminCV { get; set; } // Stocke le chemin du fichier CV

        // Foreign Keys
        public int EtudiantId { get; set; }
        public int OffreStageId { get; set; }

        // Navigation Properties
        public virtual Etudiant Etudiant { get; set; } = null!;
        public virtual OffresStage OffreStage { get; set; } = null!;

        // Une candidature peut mener à une convention
        public virtual Convention? Convention { get; set; }
    }
}
