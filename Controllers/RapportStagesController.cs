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
    public class RapportStagesController : Controller
    {
        private readonly StagesDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public RapportStagesController(StagesDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: RapportStages
        [Authorize(Roles = "Admin,Etudiant")]
        public async Task<IActionResult> Index()
        {
            var rapportsQuery = _context.RapportsStages
                .Include(r => r.Convention)
                    .ThenInclude(c => c.Candidature)
                        .ThenInclude(cand => cand.Etudiant)
                .Include(r => r.Convention)
                    .ThenInclude(c => c.Candidature)
                        .ThenInclude(cand => cand.OffreStage)
                            .ThenInclude(o => o.Entreprise);

            if (User.IsInRole("Admin"))
            {
                return View(await rapportsQuery.ToListAsync());
            }

            if (User.IsInRole("Etudiant"))
            {
                var userEmail = User.Identity.Name;
                var etudiant = await _context.Etudiants
                    .FirstOrDefaultAsync(e => e.Email == userEmail);

                if (etudiant == null)
                {
                    return View(await _context.RapportsStages.Where(r => false).ToListAsync());
                }

                var rapportsEtudiant = rapportsQuery
                    .Where(r => r.Convention.Candidature.EtudiantId == etudiant.Id);

                return View(await rapportsEtudiant.ToListAsync());
            }

            return View(await rapportsQuery.ToListAsync());
        }

        // GET: RapportStages/Details/5
        [Authorize(Roles = "Admin,Etudiant")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var rapportStage = await _context.RapportsStages
                .Include(r => r.Convention)
                    .ThenInclude(c => c.Candidature)
                        .ThenInclude(cand => cand.Etudiant)
                .Include(r => r.Convention)
                    .ThenInclude(c => c.Candidature)
                        .ThenInclude(cand => cand.OffreStage)
                            .ThenInclude(o => o.Entreprise)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (rapportStage == null) return NotFound();

            if (User.IsInRole("Etudiant"))
            {
                var userEmail = User.Identity.Name;
                var etudiant = await _context.Etudiants
                    .FirstOrDefaultAsync(e => e.Email == userEmail);

                if (etudiant == null || rapportStage.Convention.Candidature.EtudiantId != etudiant.Id)
                {
                    return Forbid();
                }
            }

            return View(rapportStage);
        }

        // GET: RapportStages/Create
        [Authorize(Roles = "Admin,Etudiant")]
        public async Task<IActionResult> Create()
        {
            if (User.IsInRole("Etudiant"))
            {
                var userEmail = User.Identity.Name;
                var etudiant = await _context.Etudiants
                    .FirstOrDefaultAsync(e => e.Email == userEmail);

                if (etudiant == null)
                {
                    TempData["Error"] = "Veuillez d'abord compléter votre profil étudiant.";
                    return RedirectToAction("Index", "Etudiants");
                }

                var conventionsEtudiant = await _context.Conventions
                    .Include(c => c.Candidature)
                        .ThenInclude(ca => ca.Etudiant)
                    .Include(c => c.Candidature)
                        .ThenInclude(ca => ca.OffreStage)
                    .Where(c => c.Candidature.EtudiantId == etudiant.Id)
                    .Select(c => new
                    {
                        c.Id,
                        Display = "Convention #" + c.Id + " - " + c.Candidature.OffreStage.Titre
                    })
                    .ToListAsync();

                if (!conventionsEtudiant.Any())
                {
                    TempData["Warning"] = "Vous n'avez aucune convention active. Contactez l'administrateur.";
                    ViewData["ConventionId"] = new SelectList(Enumerable.Empty<SelectListItem>());
                    return View();
                }

                ViewData["ConventionId"] = new SelectList(conventionsEtudiant, "Id", "Display");
            }
            else
            {
                var conventions = await _context.Conventions
                    .Include(c => c.Candidature)
                        .ThenInclude(ca => ca.Etudiant)
                    .Select(c => new
                    {
                        c.Id,
                        Display = "Convention #" + c.Id + " - " + c.Candidature.Etudiant.Nom + " " + c.Candidature.Etudiant.Prenom
                    })
                    .ToListAsync();

                ViewData["ConventionId"] = new SelectList(conventions, "Id", "Display");
            }

            return View();
        }

        // POST: RapportStages/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Etudiant")]
        public async Task<IActionResult> Create([Bind("Id,Titre,DateDepot,ConventionId")] RapportStage rapportStage, IFormFile pdfFile)
        {
            if (User.IsInRole("Etudiant"))
            {
                var userEmail = User.Identity.Name;
                var etudiant = await _context.Etudiants
                    .FirstOrDefaultAsync(e => e.Email == userEmail);

                if (etudiant == null) return Forbid();

                var convention = await _context.Conventions
                    .Include(c => c.Candidature)
                    .FirstOrDefaultAsync(c => c.Id == rapportStage.ConventionId);

                if (convention == null || convention.Candidature.EtudiantId != etudiant.Id)
                {
                    return Forbid();
                }
            }

            ModelState.Remove("Convention");
            ModelState.Remove("NomFichier");

            // Gestion upload fichier PDF
            if (pdfFile != null && pdfFile.Length > 0)
            {
                // Vérifier extension
                var extension = Path.GetExtension(pdfFile.FileName).ToLowerInvariant();
                if (extension != ".pdf")
                {
                    ModelState.AddModelError("pdfFile", "Seuls les fichiers PDF sont acceptés.");
                }
                // Vérifier taille (10 Mo max)
                else if (pdfFile.Length > 10 * 1024 * 1024)
                {
                    ModelState.AddModelError("pdfFile", "La taille du fichier ne doit pas dépasser 10 Mo.");
                }
                else
                {
                    // Créer dossier si nécessaire
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "rapports");
                    Directory.CreateDirectory(uploadsFolder);

                    // Nom unique pour le fichier
                    var uniqueFileName = $"{Guid.NewGuid()}_{pdfFile.FileName}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    // Sauvegarder le fichier
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await pdfFile.CopyToAsync(fileStream);
                    }

                    // Stocker le chemin relatif
                    rapportStage.NomFichier = $"/uploads/rapports/{uniqueFileName}";
                }
            }

            if (ModelState.IsValid)
            {
                _context.Add(rapportStage);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Rapport de stage créé avec succès !";
                return RedirectToAction(nameof(Index));
            }

            // Recharger les listes
            await ReloadConventionsList(rapportStage);
            return View(rapportStage);
        }

        // GET: RapportStages/Edit/5
        [Authorize(Roles = "Admin,Etudiant")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var rapportStage = await _context.RapportsStages
                .Include(r => r.Convention)
                    .ThenInclude(c => c.Candidature)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (rapportStage == null) return NotFound();

            if (User.IsInRole("Etudiant"))
            {
                var userEmail = User.Identity.Name;
                var etudiant = await _context.Etudiants
                    .FirstOrDefaultAsync(e => e.Email == userEmail);

                if (etudiant == null || rapportStage.Convention.Candidature.EtudiantId != etudiant.Id)
                {
                    return Forbid();
                }
            }

            await ReloadConventionsList(rapportStage);
            return View(rapportStage);
        }

        // POST: RapportStages/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Etudiant")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Titre,DateDepot,ConventionId,NomFichier")] RapportStage rapportStage, IFormFile pdfFile)
        {
            if (id != rapportStage.Id) return NotFound();

            var rapportOriginal = await _context.RapportsStages
                .Include(r => r.Convention)
                    .ThenInclude(c => c.Candidature)
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id);

            if (rapportOriginal == null) return NotFound();

            if (User.IsInRole("Etudiant"))
            {
                var userEmail = User.Identity.Name;
                var etudiant = await _context.Etudiants
                    .FirstOrDefaultAsync(e => e.Email == userEmail);

                if (etudiant == null || rapportOriginal.Convention.Candidature.EtudiantId != etudiant.Id)
                {
                    return Forbid();
                }
            }

            ModelState.Remove("Convention");

            // Gestion upload nouveau fichier
            if (pdfFile != null && pdfFile.Length > 0)
            {
                var extension = Path.GetExtension(pdfFile.FileName).ToLowerInvariant();
                if (extension != ".pdf")
                {
                    ModelState.AddModelError("pdfFile", "Seuls les fichiers PDF sont acceptés.");
                }
                else if (pdfFile.Length > 10 * 1024 * 1024)
                {
                    ModelState.AddModelError("pdfFile", "La taille du fichier ne doit pas dépasser 10 Mo.");
                }
                else
                {
                    // Supprimer l'ancien fichier si existe
                    if (!string.IsNullOrEmpty(rapportOriginal.NomFichier))
                    {
                        var oldFilePath = Path.Combine(_environment.WebRootPath, rapportOriginal.NomFichier.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    // Sauvegarder le nouveau
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "rapports");
                    Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = $"{Guid.NewGuid()}_{pdfFile.FileName}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await pdfFile.CopyToAsync(fileStream);
                    }

                    rapportStage.NomFichier = $"/uploads/rapports/{uniqueFileName}";
                }
            }
            else
            {
                // Conserver l'ancien fichier
                rapportStage.NomFichier = rapportOriginal.NomFichier;
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(rapportStage);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Rapport de stage modifié avec succès !";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RapportStageExists(rapportStage.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            await ReloadConventionsList(rapportStage);
            return View(rapportStage);
        }

        // GET: RapportStages/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var rapportStage = await _context.RapportsStages
                .Include(r => r.Convention)
                    .ThenInclude(c => c.Candidature)
                        .ThenInclude(cand => cand.Etudiant)
                .Include(r => r.Convention)
                    .ThenInclude(c => c.Candidature)
                        .ThenInclude(cand => cand.OffreStage)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (rapportStage == null) return NotFound();

            return View(rapportStage);
        }

        // POST: RapportStages/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var rapportStage = await _context.RapportsStages.FindAsync(id);
            if (rapportStage != null)
            {
                // Supprimer le fichier physique
                if (!string.IsNullOrEmpty(rapportStage.NomFichier))
                {
                    var filePath = Path.Combine(_environment.WebRootPath, rapportStage.NomFichier.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                _context.RapportsStages.Remove(rapportStage);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Rapport de stage supprimé avec succès !";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool RapportStageExists(int id)
        {
            return _context.RapportsStages.Any(e => e.Id == id);
        }

        private async Task ReloadConventionsList(RapportStage rapportStage)
        {
            if (User.IsInRole("Etudiant"))
            {
                var userEmail = User.Identity.Name;
                var etudiant = await _context.Etudiants.FirstOrDefaultAsync(e => e.Email == userEmail);

                if (etudiant != null)
                {
                    var conventionsEtudiant = await _context.Conventions
                        .Include(c => c.Candidature)
                            .ThenInclude(ca => ca.OffreStage)
                        .Where(c => c.Candidature.EtudiantId == etudiant.Id)
                        .Select(c => new
                        {
                            c.Id,
                            Display = "Convention #" + c.Id + " - " + c.Candidature.OffreStage.Titre
                        })
                        .ToListAsync();

                    ViewData["ConventionId"] = new SelectList(conventionsEtudiant, "Id", "Display", rapportStage.ConventionId);
                }
            }
            else
            {
                var conventions = await _context.Conventions
                    .Include(c => c.Candidature)
                        .ThenInclude(ca => ca.Etudiant)
                    .Select(c => new
                    {
                        c.Id,
                        Display = "Convention #" + c.Id + " - " + c.Candidature.Etudiant.Nom
                    })
                    .ToListAsync();

                ViewData["ConventionId"] = new SelectList(conventions, "Id", "Display", rapportStage.ConventionId);
            }
        }
    }
}