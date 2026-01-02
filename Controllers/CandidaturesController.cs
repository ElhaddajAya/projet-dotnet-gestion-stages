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

        public CandidaturesController(StagesDbContext context)
        {
            _context = context;
        }

        // GET: Candidatures/Index
        [Authorize(Roles = "Admin,Etudiant,Entreprise")]
        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("Admin"))
            {
                var toutesCandidatures = _context.Candidatures
                    .Include(c => c.Etudiant)
                    .Include(c => c.OffreStage);
                return View(await toutesCandidatures.ToListAsync());
            }

            if (User.IsInRole("Etudiant"))
            {
                var userEmail = User.Identity.Name;
                var etudiant = await _context.Etudiants
                    .FirstOrDefaultAsync(e => e.Email == userEmail);

                if (etudiant == null)
                {
                    return View(await _context.Candidatures.Where(c => false).ToListAsync());
                }

                var candidaturesEtudiant = _context.Candidatures
                    .Include(c => c.Etudiant)
                    .Include(c => c.OffreStage)
                    .Where(c => c.EtudiantId == etudiant.Id);

                return View(await candidaturesEtudiant.ToListAsync());
            }

            if (User.IsInRole("Entreprise"))
            {
                var userEmail = User.Identity.Name;
                var entreprise = await _context.Entreprises
                    .FirstOrDefaultAsync(e => e.EmailContact == userEmail);

                if (entreprise == null)
                {
                    return View(await _context.Candidatures.Where(c => false).ToListAsync());
                }

                var candidaturesRecues = _context.Candidatures
                    .Include(c => c.Etudiant)
                    .Include(c => c.OffreStage)
                    .Where(c => c.OffreStage.EntrepriseId == entreprise.Id);

                return View(await candidaturesRecues.ToListAsync());
            }

            return View(await _context.Candidatures.Include(c => c.Etudiant).Include(c => c.OffreStage).ToListAsync());
        }

        // GET: Candidatures/Details/5
        [Authorize(Roles = "Admin,Etudiant,Entreprise")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var candidature = await _context.Candidatures
                .Include(c => c.Etudiant)
                .Include(c => c.OffreStage)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (candidature == null) return NotFound();

            if (User.IsInRole("Etudiant"))
            {
                var userEmail = User.Identity.Name;
                var etudiant = await _context.Etudiants.FirstOrDefaultAsync(e => e.Email == userEmail);
                if (etudiant == null || candidature.EtudiantId != etudiant.Id) return Forbid();
            }
            else if (User.IsInRole("Entreprise"))
            {
                var userEmail = User.Identity.Name;
                var entreprise = await _context.Entreprises.FirstOrDefaultAsync(e => e.EmailContact == userEmail);
                if (entreprise == null || candidature.OffreStage.EntrepriseId != entreprise.Id) return Forbid();
            }

            return View(candidature);
        }

        // GET: Candidatures/Create
        [Authorize(Roles = "Admin,Etudiant")]
        public async Task<IActionResult> Create(int? offreId)
        {
            if (User.IsInRole("Etudiant"))
            {
                var userEmail = User.Identity.Name;
                var etudiant = await _context.Etudiants.FirstOrDefaultAsync(e => e.Email == userEmail);

                if (etudiant == null)
                {
                    TempData["Error"] = "Veuillez d'abord compléter votre profil étudiant.";
                    return RedirectToAction("Index", "Etudiants");
                }

                ViewData["EtudiantId"] = new SelectList(new[] { etudiant }, "Id", "Nom", etudiant.Id);
            }
            else
            {
                ViewData["EtudiantId"] = new SelectList(_context.Etudiants, "Id", "Nom");
            }

            ViewData["OffreStageId"] = offreId.HasValue
                ? new SelectList(_context.OffresStages, "Id", "Titre", offreId.Value)
                : new SelectList(_context.OffresStages, "Id", "Titre");

            return View();
        }

        // POST: Candidatures/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Etudiant")]
        public async Task<IActionResult> Create([Bind("Id,DateCandidature,Statut,EtudiantId,OffreStageId")] Candidature candidature, IFormFile? cvFile)
        {
            if (User.IsInRole("Etudiant"))
            {
                var userEmail = User.Identity.Name;
                var etudiant = await _context.Etudiants.FirstOrDefaultAsync(e => e.Email == userEmail);
                if (etudiant == null) return Forbid();
                candidature.EtudiantId = etudiant.Id;
            }

            // Définir les valeurs par défaut si elles ne sont pas fournies
            if (candidature.DateCandidature == default(DateTime))
            {
                candidature.DateCandidature = DateTime.Now;
            }

            if (string.IsNullOrEmpty(candidature.Statut))
            {
                candidature.Statut = "En attente";
            }

            if (cvFile != null && cvFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "cv");
                Directory.CreateDirectory(uploadsFolder);
                var uniqueFileName = $"{Guid.NewGuid()}_{cvFile.FileName}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await cvFile.CopyToAsync(stream);
                }
                candidature.CheminCV = $"/uploads/cv/{uniqueFileName}";
            }

            ModelState.Remove("Etudiant");
            ModelState.Remove("OffreStage");

            if (ModelState.IsValid)
            {
                _context.Add(candidature);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Candidature créée avec succès !";
                return RedirectToAction(nameof(Index));
            }

            if (User.IsInRole("Etudiant"))
            {
                var userEmail = User.Identity.Name;
                var etudiant = await _context.Etudiants.FirstOrDefaultAsync(e => e.Email == userEmail);
                if (etudiant != null) ViewData["EtudiantId"] = new SelectList(new[] { etudiant }, "Id", "Nom", candidature.EtudiantId);
            }
            else
            {
                ViewData["EtudiantId"] = new SelectList(_context.Etudiants, "Id", "Nom", candidature.EtudiantId);
            }

            ViewData["OffreStageId"] = new SelectList(_context.OffresStages, "Id", "Titre", candidature.OffreStageId);
            return View(candidature);
        }

        // GET: Candidatures/Edit/5
        [Authorize(Roles = "Admin,Entreprise")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var candidature = await _context.Candidatures.Include(c => c.OffreStage).FirstOrDefaultAsync(c => c.Id == id);
            if (candidature == null) return NotFound();

            if (User.IsInRole("Entreprise"))
            {
                var userEmail = User.Identity.Name;
                var entreprise = await _context.Entreprises.FirstOrDefaultAsync(e => e.EmailContact == userEmail);
                if (entreprise == null || candidature.OffreStage.EntrepriseId != entreprise.Id) return Forbid();
                ViewData["ReadOnly"] = true;
            }

            ViewData["EtudiantId"] = new SelectList(_context.Etudiants, "Id", "Nom", candidature.EtudiantId);
            ViewData["OffreStageId"] = new SelectList(_context.OffresStages, "Id", "Titre", candidature.OffreStageId);
            return View(candidature);
        }

        // POST: Candidatures/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Entreprise")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,DateCandidature,Statut,EtudiantId,OffreStageId")] Candidature candidature)
        {
            if (id != candidature.Id) return NotFound();

            var candidatureOriginale = await _context.Candidatures.Include(c => c.OffreStage).AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            if (candidatureOriginale == null) return NotFound();

            if (User.IsInRole("Entreprise"))
            {
                var userEmail = User.Identity.Name;
                var entreprise = await _context.Entreprises.FirstOrDefaultAsync(e => e.EmailContact == userEmail);
                if (entreprise == null || candidatureOriginale.OffreStage.EntrepriseId != entreprise.Id) return Forbid();

                candidature.EtudiantId = candidatureOriginale.EtudiantId;
                candidature.OffreStageId = candidatureOriginale.OffreStageId;
                candidature.DateCandidature = candidatureOriginale.DateCandidature;
                candidature.CheminCV = candidatureOriginale.CheminCV;
            }

            ModelState.Remove("Etudiant");
            ModelState.Remove("OffreStage");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(candidature);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Candidature modifiée avec succès !";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CandidatureExists(candidature.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["EtudiantId"] = new SelectList(_context.Etudiants, "Id", "Nom", candidature.EtudiantId);
            ViewData["OffreStageId"] = new SelectList(_context.OffresStages, "Id", "Titre", candidature.OffreStageId);
            return View(candidature);
        }

        // GET: Candidatures/Delete/5
        [Authorize(Roles = "Admin,Etudiant")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var candidature = await _context.Candidatures
                .Include(c => c.Etudiant)
                .Include(c => c.OffreStage)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (candidature == null) return NotFound();

            if (User.IsInRole("Etudiant"))
            {
                var userEmail = User.Identity.Name;
                var etudiant = await _context.Etudiants.FirstOrDefaultAsync(e => e.Email == userEmail);
                if (etudiant == null || candidature.EtudiantId != etudiant.Id) return Forbid();
            }

            return View(candidature);
        }

        // POST: Candidatures/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Etudiant")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var candidature = await _context.Candidatures.Include(c => c.Etudiant).FirstOrDefaultAsync(c => c.Id == id);

            if (candidature != null)
            {
                if (User.IsInRole("Etudiant"))
                {
                    var userEmail = User.Identity.Name;
                    var etudiant = await _context.Etudiants.FirstOrDefaultAsync(e => e.Email == userEmail);
                    if (etudiant == null || candidature.EtudiantId != etudiant.Id) return Forbid();
                }

                if (!string.IsNullOrEmpty(candidature.CheminCV))
                {
                    var cvPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", candidature.CheminCV.TrimStart('/'));
                    if (System.IO.File.Exists(cvPath)) System.IO.File.Delete(cvPath);
                }

                _context.Candidatures.Remove(candidature);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Candidature annulée avec succès !";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool CandidatureExists(int id)
        {
            return _context.Candidatures.Any(e => e.Id == id);
        }
    }
}