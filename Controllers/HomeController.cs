using GestionStages.Data;
using GestionStages.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace GestionStages.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly StagesDbContext _context;

        public HomeController(ILogger<HomeController> logger, StagesDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Admin"))
                {
                    // Statistiques globales
                    ViewBag.NbEtudiants = await _context.Etudiants.CountAsync();
                    ViewBag.NbEntreprises = await _context.Entreprises.CountAsync();
                    ViewBag.NbOffres = await _context.OffresStages.CountAsync();
                    ViewBag.NbCandidatures = await _context.Candidatures.CountAsync();
                    ViewBag.NbCandidaturesEnAttente = await _context.Candidatures.CountAsync(c => c.Statut == "En attente");
                    ViewBag.NbCandidaturesAcceptees = await _context.Candidatures.CountAsync(c => c.Statut == "Acceptée");
                    ViewBag.NbConventions = await _context.Conventions.CountAsync();
                    ViewBag.NbConventionsEnCours = await _context.Conventions.CountAsync(c => c.Statut == "En cours");
                    ViewBag.NbRapports = await _context.RapportsStages.CountAsync();

                    // Données pour les graphiques (passées en JSON sécurisé)
                    var offresParSecteur = await _context.OffresStages
                        .Include(o => o.Entreprise)
                        .GroupBy(o => o.Entreprise.Secteur ?? "Non spécifié")
                        .Select(g => new { Secteur = g.Key, Count = g.Count() }) // Compter les offres par secteur
                        .OrderByDescending(g => g.Count)
                        .Take(8)
                        .ToListAsync();

                    ViewBag.OffresParSecteurJson = System.Text.Json.JsonSerializer.Serialize(offresParSecteur);

                    // Évolution des candidatures par mois (12 derniers mois)
                    var candidaturesGroupées = await _context.Candidatures
                        .GroupBy(c => new { c.DateCandidature.Year, c.DateCandidature.Month })
                        .Select(g => new // compter les candidatures par mois
                        {
                            g.Key.Year,
                            g.Key.Month,
                            Count = g.Count()
                        })
                        .OrderByDescending(g => g.Year).ThenByDescending(g => g.Month)
                        .Take(12)
                        .ToListAsync();

                    // Formater le label "YYYY-MM" en C# (client-side)
                    var candidaturesParMoisFormatees = candidaturesGroupées
                        .OrderBy(g => g.Year).ThenBy(g => g.Month) // Remettre dans l'ordre chronologique
                        .Select(g => new
                        {
                            Mois = $"{g.Year}-{g.Month:D2}",
                            Count = g.Count
                        })
                        .ToList();

                    ViewBag.CandidaturesParMoisJson = System.Text.Json.JsonSerializer.Serialize(candidaturesParMoisFormatees);
                
                }
                else if (User.IsInRole("Etudiant"))
                {
                    // Statistiques pour Étudiant
                    var userEmail = User.Identity.Name;
                    var etudiant = await _context.Etudiants
                        .FirstOrDefaultAsync(e => e.Email == userEmail);

                    if (etudiant != null)
                    {
                        ViewBag.NomComplet = $"{etudiant.Prenom} {etudiant.Nom}";
                        ViewBag.NbCandidatures = await _context.Candidatures
                            .Where(c => c.EtudiantId == etudiant.Id)
                            .CountAsync();
                        ViewBag.NbEnAttente = await _context.Candidatures
                            .Where(c => c.EtudiantId == etudiant.Id && c.Statut == "En attente")
                            .CountAsync();
                        ViewBag.NbAcceptees = await _context.Candidatures
                            .Where(c => c.EtudiantId == etudiant.Id && c.Statut == "Acceptée")
                            .CountAsync();
                    }
                    else
                    {
                        ViewBag.NomComplet = "Étudiant";
                        ViewBag.NbCandidatures = 0;
                        ViewBag.NbEnAttente = 0;
                        ViewBag.NbAcceptees = 0;
                    }
                }
                else if (User.IsInRole("Entreprise"))
                {
                    // Statistiques pour Entreprise
                    var userEmail = User.Identity.Name;
                    var entreprise = await _context.Entreprises
                        .FirstOrDefaultAsync(e => e.EmailContact == userEmail);

                    if (entreprise != null)
                    {
                        ViewBag.NomEntreprise = entreprise.Nom;
                        ViewBag.NbOffres = await _context.OffresStages
                            .Where(o => o.EntrepriseId == entreprise.Id)
                            .CountAsync();
                        ViewBag.NbCandidatures = await _context.Candidatures
                            .Where(c => c.OffreStage.EntrepriseId == entreprise.Id)
                            .CountAsync();
                        ViewBag.NbNouvelles = await _context.Candidatures
                            .Where(c => c.OffreStage.EntrepriseId == entreprise.Id && c.Statut == "En attente")
                            .CountAsync();
                    }
                    else
                    {
                        ViewBag.NomEntreprise = "Entreprise";
                        ViewBag.NbOffres = 0;
                        ViewBag.NbCandidatures = 0;
                        ViewBag.NbNouvelles = 0;
                    }
                }
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}