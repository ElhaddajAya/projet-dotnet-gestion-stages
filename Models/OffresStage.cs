using System.ComponentModel.DataAnnotations;

namespace GestionStages.Models
{
    public class OffresStage
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Le titre est obligatoire")]
        public string Titre { get; set; } = string.Empty;

        [Required(ErrorMessage = "La description est obligatoire")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "La durée est obligatoire")]
        public int DureeMois { get; set; }

        public DateTime? DateDebutSouhaitee { get; set; }

        // Clé étrangère
        public int EntrepriseId { get; set; }

        // Navigation
        public virtual Entreprise Entreprise { get; set; } = null!;
        public virtual List<Candidature> Candidatures { get; set; } = new List<Candidature>();
    }

}
