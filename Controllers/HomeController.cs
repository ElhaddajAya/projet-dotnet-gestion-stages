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
                    // Statistiques pour Admin
                    ViewBag.NbEtudiants = await _context.Etudiants.CountAsync();
                    ViewBag.NbEntreprises = await _context.Entreprises.CountAsync();
                    ViewBag.NbOffres = await _context.OffresStages.CountAsync();
                    ViewBag.NbCandidatures = await _context.Candidatures.CountAsync();
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