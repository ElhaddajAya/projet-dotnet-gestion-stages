namespace GestionStages.Models
{
    public class Convention
    {
        public int Id { get; set; }
        public DateTime DateSignature { get; set; }
        public DateTime DateDebut { get; set; }
        public DateTime DateFin { get; set; }
        public string Statut { get; set; } = "Signée"; // Signée, En cours, Terminée

        // Clé étrangère vers Candidature
        public int CandidatureId { get; set; }

        // Navigation
        public virtual Candidature Candidature { get; set; } = null!;

        // Un rapport peut être lié
        public virtual RapportStage? Rapport { get; set; }
    }
}
