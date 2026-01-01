using GestionStages.Data;
using GestionStages.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GestionStages.Controllers
{
    [Authorize] // Tous les utilisateurs connectés
    public class CandidaturesController : Controller
    {
        private readonly StagesDbContext _context;

        public CandidaturesController(StagesDbContext context)
        {
            _context = context;
        }

        // GET: Candidatures - Admin, Etudiant et Entreprise peuvent voir
        [Authorize(Roles = "Admin,Etudiant,Entreprise")]
        public async Task<IActionResult> Index()
        {
            var stagesDbContext = _context.Candidatures.Include(c => c.Etudiant).Include(c => c.OffreStage);
            return View(await stagesDbContext.ToListAsync());
        }

        // GET: Candidatures/Details/5 - Admin, Etudiant et Entreprise peuvent voir les détails
        [Authorize(Roles = "Admin,Etudiant,Entreprise")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var candidature = await _context.Candidatures
                .Include(c => c.Etudiant)
                .Include(c => c.OffreStage)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (candidature == null)
            {
                return NotFound();
            }

            return View(candidature);
        }

        // GET: Candidatures/Create - Seulement Admin et Etudiant peuvent créer
        [Authorize(Roles = "Admin,Etudiant")]
        public IActionResult Create()
        {
            ViewData["EtudiantId"] = new SelectList(_context.Etudiants, "Id", "Nom");
            ViewData["OffreStageId"] = new SelectList(_context.OffresStages, "Id", "Titre");
            return View();
        }

        // POST: Candidatures/Create - Seulement Admin et Etudiant peuvent créer
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Etudiant")]
        public async Task<IActionResult> Create([Bind("Id,DateCandidature,Statut,EtudiantId,OffreStageId")] Candidature candidature)
        {
            if (ModelState.IsValid)
            {
                _context.Add(candidature);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["EtudiantId"] = new SelectList(_context.Etudiants, "Id", "Nom", candidature.EtudiantId);
            ViewData["OffreStageId"] = new SelectList(_context.OffresStages, "Id", "Titre", candidature.OffreStageId);
            return View(candidature);
        }

        // GET: Candidatures/Edit/5 - Admin, Etudiant et Entreprise peuvent modifier (Entreprise pour changer le statut)
        [Authorize(Roles = "Admin,Etudiant,Entreprise")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var candidature = await _context.Candidatures.FindAsync(id);
            if (candidature == null)
            {
                return NotFound();
            }
            ViewData["EtudiantId"] = new SelectList(_context.Etudiants, "Id", "Nom", candidature.EtudiantId);
            ViewData["OffreStageId"] = new SelectList(_context.OffresStages, "Id", "Titre", candidature.OffreStageId);
            return View(candidature);
        }

        // POST: Candidatures/Edit/5 - Admin, Etudiant et Entreprise peuvent modifier
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Etudiant,Entreprise")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,DateCandidature,Statut,EtudiantId,OffreStageId")] Candidature candidature)
        {
            if (id != candidature.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(candidature);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CandidatureExists(candidature.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["EtudiantId"] = new SelectList(_context.Etudiants, "Id", "Nom", candidature.EtudiantId);
            ViewData["OffreStageId"] = new SelectList(_context.OffresStages, "Id", "Titre", candidature.OffreStageId);
            return View(candidature);
        }

        // GET: Candidatures/Delete/5 - Seulement Admin peut supprimer
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var candidature = await _context.Candidatures
                .Include(c => c.Etudiant)
                .Include(c => c.OffreStage)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (candidature == null)
            {
                return NotFound();
            }

            return View(candidature);
        }

        // POST: Candidatures/Delete/5 - Seulement Admin peut supprimer
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var candidature = await _context.Candidatures.FindAsync(id);
            if (candidature != null)
            {
                _context.Candidatures.Remove(candidature);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CandidatureExists(int id)
        {
            return _context.Candidatures.Any(e => e.Id == id);
        }
    }
}
