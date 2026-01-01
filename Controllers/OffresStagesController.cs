using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GestionStages.Data;
using GestionStages.Models;

namespace GestionStages.Controllers
{
    public class OffresStagesController : Controller
    {
        private readonly StagesDbContext _context;

        public OffresStagesController(StagesDbContext context)
        {
            _context = context;
        }

        // GET: OffresStages
        public async Task<IActionResult> Index()
        {
            var stagesDbContext = _context.OffresStages.Include(o => o.Entreprise);
            return View(await stagesDbContext.ToListAsync());
        }

        // GET: OffresStages/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var offresStage = await _context.OffresStages
                .Include(o => o.Entreprise)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (offresStage == null)
            {
                return NotFound();
            }

            return View(offresStage);
        }

        // GET: OffresStages/Create
        public IActionResult Create()
        {
            ViewData["EntrepriseId"] = new SelectList(_context.Entreprises, "Id", "Nom");
            return View();
        }

        // POST: OffresStages/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Titre,Description,DureeMois,DateDebutSouhaitee,EntrepriseId")] OffresStage offresStage)
        {
            if (ModelState.IsValid)
            {
                _context.Add(offresStage);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["EntrepriseId"] = new SelectList(_context.Entreprises, "Id", "Nom", offresStage.EntrepriseId);
            return View(offresStage);
        }

        // GET: OffresStages/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var offresStage = await _context.OffresStages.FindAsync(id);
            if (offresStage == null)
            {
                return NotFound();
            }
            ViewData["EntrepriseId"] = new SelectList(_context.Entreprises, "Id", "Nom", offresStage.EntrepriseId);
            return View(offresStage);
        }

        // POST: OffresStages/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Titre,Description,DureeMois,DateDebutSouhaitee,EntrepriseId")] OffresStage offresStage)
        {
            if (id != offresStage.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(offresStage);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OffresStageExists(offresStage.Id))
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
            ViewData["EntrepriseId"] = new SelectList(_context.Entreprises, "Id", "Nom", offresStage.EntrepriseId);
            return View(offresStage);
        }

        // GET: OffresStages/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var offresStage = await _context.OffresStages
                .Include(o => o.Entreprise)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (offresStage == null)
            {
                return NotFound();
            }

            return View(offresStage);
        }

        // POST: OffresStages/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var offresStage = await _context.OffresStages.FindAsync(id);
            if (offresStage != null)
            {
                _context.OffresStages.Remove(offresStage);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool OffresStageExists(int id)
        {
            return _context.OffresStages.Any(e => e.Id == id);
        }
    }
}
