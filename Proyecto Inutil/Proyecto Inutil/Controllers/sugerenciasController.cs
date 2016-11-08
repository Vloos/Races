using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Proyecto_Inutil.Models;

namespace Proyecto_Inutil.Controllers
{
    public class sugerenciasController : Controller
    {
        private epiEntities1 db = new epiEntities1();

        // GET: sugerencias
        public async Task<ActionResult> Index()
        {
            var sugerencias = db.sugerencias.Include(s => s.usuario);
            return View(await sugerencias.ToListAsync());
        }

        // GET: sugerencias/Details/5
        public async Task<ActionResult> Details(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            sugerencia sugerencia = await db.sugerencias.FindAsync(id);
            if (sugerencia == null)
            {
                return HttpNotFound();
            }
            return View(sugerencia);
        }

        // GET: sugerencias/Create
        public ActionResult Create()
        {
            ViewBag.usuario_id = new SelectList(db.usuarios, "id", "nombre");
            return View();
        }

        // POST: sugerencias/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "id,tipo,usuario_id,fecha,contenido")] sugerencia sugerencia)
        {
            if (ModelState.IsValid)
            {
                db.sugerencias.Add(sugerencia);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            ViewBag.usuario_id = new SelectList(db.usuarios, "id", "nombre", sugerencia.usuario_id);
            return View(sugerencia);
        }

        // GET: sugerencias/Edit/5
        public async Task<ActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            sugerencia sugerencia = await db.sugerencias.FindAsync(id);
            if (sugerencia == null)
            {
                return HttpNotFound();
            }
            ViewBag.usuario_id = new SelectList(db.usuarios, "id", "nombre", sugerencia.usuario_id);
            return View(sugerencia);
        }

        // POST: sugerencias/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "id,tipo,usuario_id,fecha,contenido")] sugerencia sugerencia)
        {
            if (ModelState.IsValid)
            {
                db.Entry(sugerencia).State = System.Data.Entity.EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            ViewBag.usuario_id = new SelectList(db.usuarios, "id", "nombre", sugerencia.usuario_id);
            return View(sugerencia);
        }

        // GET: sugerencias/Delete/5
        public async Task<ActionResult> Delete(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            sugerencia sugerencia = await db.sugerencias.FindAsync(id);
            if (sugerencia == null)
            {
                return HttpNotFound();
            }
            return View(sugerencia);
        }

        // POST: sugerencias/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(long id)
        {
            sugerencia sugerencia = await db.sugerencias.FindAsync(id);
            db.sugerencias.Remove(sugerencia);
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
