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

        // GET: Candidatures - AVEC RECHERCHE, FILTRAGE PAR STATUT ET PAGINATION (10 par page)
        public async Task<IActionResult> Index(
            string searchString,
            string statutFilter,
            int pageNumber = 1)  // Ajout du paramètre page
        {
            const int pageSize = 10;

            // Conserver les filtres pour la vue et la pagination
            ViewBag.CurrentSearch = searchString;
            ViewBag.CurrentStatut = statutFilter;

            var statuts = await _context.Candidatures
                .Select(c => c.Statut)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();

            ViewBag.Statuts = statuts;

            // Requête de base avec toutes les relations
            var candidaturesQuery = _context.Candidatures
                .Include(c => c.Etudiant)
                .Include(c => c.OffreStage)
                    .ThenInclude(o => o.Entreprise)
                .AsQueryable();

            // FILTRAGE PAR RÔLE (inchangé - on garde tout ton code existant ici)
            if (User.IsInRole("Etudiant"))
            {
                var userEmail = User.Identity.Name;
                candidaturesQuery = candidaturesQuery.Where(c => c.Etudiant.Email == userEmail);
            }
            else if (User.IsInRole("Entreprise"))
            {
                var userEmail = User.Identity.Name;
                var entreprise = await _context.Entreprises
                    .FirstOrDefaultAsync(e => e.EmailContact == userEmail);
                if (entreprise != null)
                {
                    candidaturesQuery = candidaturesQuery.Where(c => c.OffreStage.EntrepriseId == entreprise.Id);
                }
            }
            // Admin voit tout

            // RECHERCHE
            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.ToLower();
                candidaturesQuery = candidaturesQuery.Where(c =>
                    c.Etudiant.Nom.ToLower().Contains(searchString) ||
                    c.Etudiant.Prenom.ToLower().Contains(searchString) ||
                    c.OffreStage.Titre.ToLower().Contains(searchString) ||
                    c.OffreStage.Entreprise.Nom.ToLower().Contains(searchString));
            }

            // FILTRE PAR STATUT
            if (!string.IsNullOrEmpty(statutFilter))
            {
                candidaturesQuery = candidaturesQuery.Where(c => c.Statut == statutFilter);
            }

            // TRI par date (plus récent en haut)
            candidaturesQuery = candidaturesQuery.OrderByDescending(c => c.DateCandidature);

            // COMPTAGE TOTAL POUR PAGINATION
            var totalCandidatures = await candidaturesQuery.CountAsync();

            // PAGINATION
            var candidatures = await candidaturesQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Infos pour la vue
            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCandidatures = totalCandidatures;
            ViewBag.HasPreviousPage = pageNumber > 1;
            ViewBag.HasNextPage = pageNumber < Math.Ceiling((double)totalCandidatures / pageSize);

            return View(candidatures);
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

        // GET: Candidatures/ExportExcel
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExportExcel()
        {
            var candidatures = await _context.Candidatures
                .Include(c => c.Etudiant)
                .Include(c => c.OffreStage)
                    .ThenInclude(o => o.Entreprise)
                .Select(c => new
                {
                    Étudiant = c.Etudiant.Nom + " " + c.Etudiant.Prenom,
                    Email_Étudiant = c.Etudiant.Email,
                    Offre = c.OffreStage.Titre,
                    Entreprise = c.OffreStage.Entreprise.Nom,
                    Date_Candidature = c.DateCandidature.ToString("dd/MM/yyyy"),
                    Statut = c.Statut
                })
                .ToListAsync();

            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Candidatures");

            // En-tête
            worksheet.Cell(1, 1).Value = "Liste des candidatures";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 16;
            worksheet.Row(1).Height = 30;

            // Colonnes
            var range = worksheet.Cell(3, 1).InsertTable(candidatures);
            range.Theme = ClosedXML.Excel.XLTableTheme.TableStyleMedium9;

            // Ajuster la largeur
            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            return File(
                content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Candidatures_{DateTime.Now:yyyy-MM-dd}.xlsx");
        }
    }
}