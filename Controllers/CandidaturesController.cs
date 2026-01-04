using GestionStages.Data;
using GestionStages.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GestionStages.Controllers
{
    [Authorize]
    public class CandidaturesController : Controller
    {
        private readonly StagesDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public CandidaturesController(StagesDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: Candidatures
        // AVEC RECHERCHE ET FILTRAGE
        public async Task<IActionResult> Index(string searchString, string statutFilter)
        {
            // Requête de base avec toutes les relations
            var candidaturesQuery = _context.Candidatures
                .Include(c => c.Etudiant)
                .Include(c => c.OffreStage)
                    .ThenInclude(o => o.Entreprise)
                .AsQueryable();

            // FILTRAGE PAR RÔLE
            if (User.IsInRole("Etudiant"))
            {
                // L'étudiant voit uniquement ses candidatures
                var userEmail = User.Identity.Name;
                var etudiant = await _context.Etudiants
                    .FirstOrDefaultAsync(e => e.Email == userEmail);

                if (etudiant != null)
                {
                    candidaturesQuery = candidaturesQuery.Where(c => c.EtudiantId == etudiant.Id);
                }
            }
            else if (User.IsInRole("Entreprise"))
            {
                // L'entreprise voit les candidatures pour ses offres
                var userEmail = User.Identity.Name;
                var entreprise = await _context.Entreprises
                    .FirstOrDefaultAsync(e => e.EmailContact == userEmail);

                if (entreprise != null)
                {
                    candidaturesQuery = candidaturesQuery.Where(c => c.OffreStage.EntrepriseId == entreprise.Id);
                }
            }
            // Admin voit toutes les candidatures

            // RECHERCHE PAR MOTS-CLÉS
            if (!string.IsNullOrEmpty(searchString))
            {
                if (User.IsInRole("Etudiant"))
                {
                    // Étudiant : chercher dans titre offre et nom entreprise
                    candidaturesQuery = candidaturesQuery.Where(c =>
                        c.OffreStage.Titre.Contains(searchString) ||
                        c.OffreStage.Entreprise.Nom.Contains(searchString)
                    );
                }
                else if (User.IsInRole("Entreprise"))
                {
                    // Entreprise : chercher dans nom étudiant et filière
                    candidaturesQuery = candidaturesQuery.Where(c =>
                        c.Etudiant.Nom.Contains(searchString) ||
                        c.Etudiant.Prenom.Contains(searchString) ||
                        c.Etudiant.Filiere.Contains(searchString)
                    );
                }
                else if (User.IsInRole("Admin"))
                {
                    // Admin : chercher partout
                    candidaturesQuery = candidaturesQuery.Where(c =>
                        c.Etudiant.Nom.Contains(searchString) ||
                        c.Etudiant.Prenom.Contains(searchString) ||
                        c.OffreStage.Titre.Contains(searchString) ||
                        c.OffreStage.Entreprise.Nom.Contains(searchString)
                    );
                }
            }

            // FILTRE PAR STATUT
            if (!string.IsNullOrEmpty(statutFilter))
            {
                candidaturesQuery = candidaturesQuery.Where(c => c.Statut == statutFilter);
            }

            // TRI PAR DATE (plus récentes en premier)
            candidaturesQuery = candidaturesQuery.OrderByDescending(c => c.DateCandidature);

            // PRÉPARER LES DONNÉES POUR LES FILTRES
            // Liste des statuts possibles
            var statuts = new List<string> { "En attente", "Acceptée", "Refusée" };
            ViewBag.Statuts = statuts;

            // Conserver les valeurs des filtres
            ViewBag.CurrentSearch = searchString;
            ViewBag.CurrentStatut = statutFilter;

            return View(await candidaturesQuery.ToListAsync());
        }

        // GET: Candidatures/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var candidature = await _context.Candidatures
                .Include(c => c.Etudiant)
                .Include(c => c.OffreStage)
                    .ThenInclude(o => o.Entreprise)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (candidature == null) return NotFound();

            // Vérifier les permissions
            if (User.IsInRole("Etudiant"))
            {
                var userEmail = User.Identity.Name;
                if (candidature.Etudiant.Email != userEmail)
                {
                    TempData["Error"] = "Accès non autorisé.";
                    return RedirectToAction(nameof(Index));
                }
            }
            else if (User.IsInRole("Entreprise"))
            {
                var userEmail = User.Identity.Name;
                if (candidature.OffreStage.Entreprise.EmailContact != userEmail)
                {
                    TempData["Error"] = "Accès non autorisé.";
                    return RedirectToAction(nameof(Index));
                }
            }

            return View(candidature);
        }

        // GET: Candidatures/Create
        [Authorize(Roles = "Etudiant")]
        public async Task<IActionResult> Create(int? offreId)
        {
            if (offreId == null) return RedirectToAction("Index", "OffresStages");

            var offre = await _context.OffresStages
                .Include(o => o.Entreprise)
                .FirstOrDefaultAsync(o => o.Id == offreId);

            if (offre == null)
            {
                TempData["Error"] = "Offre introuvable.";
                return RedirectToAction("Index", "OffresStages");
            }

            // Vérifier si l'étudiant a déjà postulé
            var userEmail = User.Identity.Name;
            var etudiant = await _context.Etudiants
                .FirstOrDefaultAsync(e => e.Email == userEmail);

            if (etudiant == null)
            {
                TempData["Error"] = "Profil étudiant introuvable. Veuillez compléter votre profil.";
                return RedirectToAction("Index", "Etudiants");
            }

            var existeCandidature = await _context.Candidatures
                .AnyAsync(c => c.EtudiantId == etudiant.Id && c.OffreStageId == offreId);

            if (existeCandidature)
            {
                TempData["Error"] = "Vous avez déjà postulé à cette offre.";
                return RedirectToAction("Details", "OffresStages", new { id = offreId });
            }

            ViewBag.Offre = offre;
            ViewBag.Etudiant = etudiant;

            return View();
        }

        // POST: Candidatures/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Etudiant")]
        public async Task<IActionResult> Create(int offreId, IFormFile cvFile)
        {
            // Vérifier que l'offre existe
            var offre = await _context.OffresStages
                .Include(o => o.Entreprise)
                .FirstOrDefaultAsync(o => o.Id == offreId);

            if (offre == null)
            {
                TempData["Error"] = "Offre introuvable.";
                return RedirectToAction("Index", "OffresStages");
            }

            var userEmail = User.Identity.Name;
            var etudiant = await _context.Etudiants
                .FirstOrDefaultAsync(e => e.Email == userEmail);

            if (etudiant == null)
            {
                TempData["Error"] = "Profil étudiant introuvable.";
                return RedirectToAction("Index", "Etudiants");
            }

            // Validation du CV
            if (cvFile == null || cvFile.Length == 0)
            {
                TempData["Error"] = "Veuillez téléverser votre CV.";
                ViewBag.Offre = offre;
                ViewBag.Etudiant = etudiant;
                return View();
            }

            // Vérifier l'extension
            var extension = Path.GetExtension(cvFile.FileName).ToLowerInvariant();
            if (extension != ".pdf")
            {
                TempData["Error"] = "Seuls les fichiers PDF sont acceptés.";
                ViewBag.Offre = offre;
                ViewBag.Etudiant = etudiant;
                return View();
            }

            // Vérifier la taille (max 10 Mo)
            if (cvFile.Length > 10 * 1024 * 1024)
            {
                TempData["Error"] = "La taille du fichier ne doit pas dépasser 10 Mo.";
                ViewBag.Offre = offre;
                ViewBag.Etudiant = etudiant;
                return View();
            }

            // Enregistrer le fichier
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "cv");
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}_{cvFile.FileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await cvFile.CopyToAsync(stream);
            }

            // Créer la candidature
            var candidature = new Candidature
            {
                EtudiantId = etudiant.Id,
                OffreStageId = offreId,
                DateCandidature = DateTime.Now,
                Statut = "En attente",
                CheminCV = $"/uploads/cv/{uniqueFileName}"
            };

            _context.Add(candidature);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Candidature envoyée avec succès !";
            return RedirectToAction(nameof(Index));
        }

        // POST: Candidatures/Accept/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Entreprise")]
        public async Task<IActionResult> Accept(int id)
        {
            var candidature = await _context.Candidatures
                .Include(c => c.OffreStage)
                    .ThenInclude(o => o.Entreprise)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (candidature == null) return NotFound();

            // Vérifier que c'est bien l'entreprise propriétaire
            var userEmail = User.Identity.Name;
            if (candidature.OffreStage.Entreprise.EmailContact != userEmail)
            {
                TempData["Error"] = "Accès non autorisé.";
                return RedirectToAction(nameof(Index));
            }

            candidature.Statut = "Acceptée";
            await _context.SaveChangesAsync();

            TempData["Success"] = "Candidature acceptée avec succès !";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Candidatures/Reject/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Entreprise")]
        public async Task<IActionResult> Reject(int id)
        {
            var candidature = await _context.Candidatures
                .Include(c => c.OffreStage)
                    .ThenInclude(o => o.Entreprise)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (candidature == null) return NotFound();

            // Vérifier que c'est bien l'entreprise propriétaire
            var userEmail = User.Identity.Name;
            if (candidature.OffreStage.Entreprise.EmailContact != userEmail)
            {
                TempData["Error"] = "Accès non autorisé.";
                return RedirectToAction(nameof(Index));
            }

            candidature.Statut = "Refusée";
            await _context.SaveChangesAsync();

            TempData["Success"] = "Candidature refusée.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Candidatures/Delete/5
        [Authorize(Roles = "Etudiant,Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var candidature = await _context.Candidatures
                .Include(c => c.Etudiant)
                .Include(c => c.OffreStage)
                    .ThenInclude(o => o.Entreprise)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (candidature == null) return NotFound();

            // Vérifier les permissions
            if (User.IsInRole("Etudiant"))
            {
                var userEmail = User.Identity.Name;
                if (candidature.Etudiant.Email != userEmail)
                {
                    TempData["Error"] = "Vous ne pouvez supprimer que vos propres candidatures.";
                    return RedirectToAction(nameof(Index));
                }
            }

            return View(candidature);
        }

        // POST: Candidatures/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Etudiant,Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var candidature = await _context.Candidatures.FindAsync(id);
            if (candidature != null)
            {
                // Supprimer le fichier CV
                if (!string.IsNullOrEmpty(candidature.CheminCV))
                {
                    var cvPath = Path.Combine(_environment.WebRootPath, candidature.CheminCV.TrimStart('/'));
                    if (System.IO.File.Exists(cvPath))
                    {
                        System.IO.File.Delete(cvPath);
                    }
                }

                _context.Candidatures.Remove(candidature);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Candidature supprimée avec succès !";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}