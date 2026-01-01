using System.ComponentModel.DataAnnotations;

namespace GestionStages.Models
{
    public class Entreprise
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Le nom de l'entreprise est obligatoire")]
        public string Nom { get; set; } = string.Empty;

        public string Adresse { get; set; } = string.Empty;
        public string Telephone { get; set; } = string.Empty;
        public string EmailContact { get; set; } = string.Empty;
        public string Secteur { get; set; } = string.Empty;

        // Navigation : offres publiées par cette entreprise
        public virtual List<OffresStage> Offres { get; set; } = new List<OffresStage>();
    }
}
