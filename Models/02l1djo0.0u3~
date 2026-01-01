using System.ComponentModel.DataAnnotations;

namespace GestionStages.Models
{
    public class Etudiant
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Le nom est obligatoire")]
        public string Nom { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le prénom est obligatoire")]
        public string Prenom { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'email est obligatoire")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        public string Email { get; set; } = string.Empty;

        public string Telephone { get; set; } = string.Empty;
        public string Filiere { get; set; } = string.Empty;
        public string Niveau { get; set; } = string.Empty;

        // Navigation : un étudiant peut avoir plusieurs candidatures
        public virtual List<Candidature> Candidatures { get; set; } = new List<Candidature>();
    }
}
