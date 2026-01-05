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
    [Authorize(Roles = "Admin")]
    public class ConventionsController : Controller
    {
        private readonly StagesDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ConventionsController(StagesDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: Conventions - AVEC RECHERCHE ET FILTRAGE
        public async Task<IActionResult> Index(string searchString, string statutFilter)
        {
            // Requête de base avec toutes les relations
            var conventionsQuery = _context.Conventions
                .Include(c => c.Candidature)
                    .ThenInclude(cand => cand.Etudiant)
                .Include(c => c.Candidature)
                    .ThenInclude(cand => cand.OffreStage)
                        .ThenInclude(o => o.Entreprise)
                .AsQueryable();

            // RECHERCHE PAR MOTS-CLÉS (Nom étudiant, entreprise, offre)
            if (!string.IsNullOrEmpty(searchString))
            {
                conventionsQuery = conventionsQuery.Where(c =>
                    c.Candidature.Etudiant.Nom.Contains(searchString) ||
                    c.Candidature.Etudiant.Prenom.Contains(searchString) ||
                    c.Candidature.OffreStage.Titre.Contains(searchString) ||
                    c.Candidature.OffreStage.Entreprise.Nom.Contains(searchString)
                );
            }

            // FILTRE PAR STATUT
            if (!string.IsNullOrEmpty(statutFilter))
            {
                conventionsQuery = conventionsQuery.Where(c => c.Statut == statutFilter);
            }

            // TRI PAR DATE DE SIGNATURE (plus récentes en premier)
            conventionsQuery = conventionsQuery.OrderByDescending(c => c.DateSignature);

            // PRÉPARER LES DONNÉES POUR LES FILTRES
            // Liste des statuts possibles
            var statuts = new List<string> { "Signée", "En cours", "Terminée" };
            ViewBag.Statuts = statuts;

            // Conserver les valeurs des filtres
            ViewBag.CurrentSearch = searchString;
            ViewBag.CurrentStatut = statutFilter;

            return View(await conventionsQuery.ToListAsync());
        }

        // GET: Conventions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var convention = await _context.Conventions
                .Include(c => c.Candidature)
                    .ThenInclude(cand => cand.Etudiant)
                .Include(c => c.Candidature)
                    .ThenInclude(cand => cand.OffreStage)
                        .ThenInclude(o => o.Entreprise)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (convention == null) return NotFound();

            return View(convention);
        }

        // GET: Conventions/Create
        public async Task<IActionResult> Create()
        {
            var candidatures = await _context.Candidatures
                .Include(c => c.Etudiant)
                .Include(c => c.OffreStage)
                .Where(c => c.Statut == "Acceptée")
                .Select(c => new
                {
                    c.Id,
                    Display = c.Etudiant.Nom + " " + c.Etudiant.Prenom + " - " + c.OffreStage.Titre
                })
                .ToListAsync();

            ViewData["CandidatureId"] = new SelectList(candidatures, "Id", "Display");
            return View();
        }

        // POST: Conventions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,DateSignature,DateDebut,DateFin,Statut,CandidatureId")] Convention convention, IFormFile? pdfFile)
        {
            ModelState.Remove("Candidature");
            ModelState.Remove("Rapport");
            ModelState.Remove("CheminFichierPDF");

            // Gestion upload fichier PDF
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
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "conventions");
                    Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = $"{Guid.NewGuid()}_{pdfFile.FileName}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await pdfFile.CopyToAsync(fileStream);
                    }

                    convention.CheminFichierPDF = $"/uploads/conventions/{uniqueFileName}";
                }
            }

            if (ModelState.IsValid)
            {
                _context.Add(convention);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Convention créée avec succès !";
                return RedirectToAction(nameof(Index));
            }

            var candidatures = await _context.Candidatures
                .Include(c => c.Etudiant)
                .Include(c => c.OffreStage)
                .Where(c => c.Statut == "Acceptée")
                .Select(c => new
                {
                    c.Id,
                    Display = c.Etudiant.Nom + " " + c.Etudiant.Prenom + " - " + c.OffreStage.Titre
                })
                .ToListAsync();

            ViewData["CandidatureId"] = new SelectList(candidatures, "Id", "Display", convention.CandidatureId);
            return View(convention);
        }

        // GET: Conventions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var convention = await _context.Conventions.FindAsync(id);
            if (convention == null) return NotFound();

            var candidatures = await _context.Candidatures
                .Include(c => c.Etudiant)
                .Include(c => c.OffreStage)
                .Select(c => new
                {
                    c.Id,
                    Display = c.Etudiant.Nom + " " + c.Etudiant.Prenom + " - " + c.OffreStage.Titre
                })
                .ToListAsync();

            ViewData["CandidatureId"] = new SelectList(candidatures, "Id", "Display", convention.CandidatureId);
            return View(convention);
        }

        // POST: Conventions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,DateSignature,DateDebut,DateFin,Statut,CandidatureId,CheminFichierPDF")] Convention convention, IFormFile? pdfFile)
        {
            if (id != convention.Id) return NotFound();

            var conventionOriginale = await _context.Conventions
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (conventionOriginale == null) return NotFound();

            ModelState.Remove("Candidature");
            ModelState.Remove("Rapport");

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
                    if (!string.IsNullOrEmpty(conventionOriginale.CheminFichierPDF))
                    {
                        var oldFilePath = Path.Combine(_environment.WebRootPath, conventionOriginale.CheminFichierPDF.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    // Sauvegarder le nouveau
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "conventions");
                    Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = $"{Guid.NewGuid()}_{pdfFile.FileName}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await pdfFile.CopyToAsync(fileStream);
                    }

                    convention.CheminFichierPDF = $"/uploads/conventions/{uniqueFileName}";
                }
            }
            else
            {
                // Conserver l'ancien fichier
                convention.CheminFichierPDF = conventionOriginale.CheminFichierPDF;
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(convention);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Convention modifiée avec succès !";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ConventionExists(convention.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            var candidatures = await _context.Candidatures
                .Include(c => c.Etudiant)
                .Include(c => c.OffreStage)
                .Select(c => new
                {
                    c.Id,
                    Display = c.Etudiant.Nom + " " + c.Etudiant.Prenom + " - " + c.OffreStage.Titre
                })
                .ToListAsync();

            ViewData["CandidatureId"] = new SelectList(candidatures, "Id", "Display", convention.CandidatureId);
            return View(convention);
        }

        // GET: Conventions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var convention = await _context.Conventions
                .Include(c => c.Candidature)
                    .ThenInclude(cand => cand.Etudiant)
                .Include(c => c.Candidature)
                    .ThenInclude(cand => cand.OffreStage)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (convention == null) return NotFound();

            return View(convention);
        }

        // POST: Conventions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var convention = await _context.Conventions.FindAsync(id);
            if (convention != null)
            {
                // Supprimer le fichier physique
                if (!string.IsNullOrEmpty(convention.CheminFichierPDF))
                {
                    var filePath = Path.Combine(_environment.WebRootPath, convention.CheminFichierPDF.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                _context.Conventions.Remove(convention);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Convention supprimée avec succès !";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ConventionExists(int id)
        {
            return _context.Conventions.Any(e => e.Id == id);
        }
    }
}