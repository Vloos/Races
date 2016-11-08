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
    public class comentariosController : Controller
    {
        private epiEntities1 db = new epiEntities1();

        // GET: comentarios
        public async Task<ActionResult> Index()
        {
            var comentarios = db.comentarios.Include(c => c.noticia).Include(c => c.usuario);
            return View(await comentarios.ToListAsync());
        }

        // GET: comentarios/Details/5
        public async Task<ActionResult> Details(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            comentario comentario = await db.comentarios.FindAsync(id);
            if (comentario == null)
            {
                return HttpNotFound();
            }
            return View(comentario);
        }

        // GET: comentarios/Create
        public ActionResult Create()
        {
            ViewBag.noticia_id = new SelectList(db.noticias, "id", "titulo");
            ViewBag.usuario_id = new SelectList(db.usuarios, "id", "nombre");
            return View();
        }

        // POST: comentarios/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "id,usuario_id,noticia_id,contenido,fecha")] comentario comentario)
        {
            if (ModelState.IsValid)
            {
                db.comentarios.Add(comentario);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            ViewBag.noticia_id = new SelectList(db.noticias, "id", "titulo", comentario.noticia_id);
            ViewBag.usuario_id = new SelectList(db.usuarios, "id", "nombre", comentario.usuario_id);
            return View(comentario);
        }

        // GET: comentarios/Edit/5
        public async Task<ActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            comentario comentario = await db.comentarios.FindAsync(id);
            if (comentario == null)
            {
                return HttpNotFound();
            }
            ViewBag.noticia_id = new SelectList(db.noticias, "id", "titulo", comentario.noticia_id);
            ViewBag.usuario_id = new SelectList(db.usuarios, "id", "nombre", comentario.usuario_id);
            return View(comentario);
        }

        // POST: comentarios/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "id,usuario_id,noticia_id,contenido,fecha")] comentario comentario)
        {
            if (ModelState.IsValid)
            {
                db.Entry(comentario).State = System.Data.Entity.EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            ViewBag.noticia_id = new SelectList(db.noticias, "id", "titulo", comentario.noticia_id);
            ViewBag.usuario_id = new SelectList(db.usuarios, "id", "nombre", comentario.usuario_id);
            return View(comentario);
        }

        // GET: comentarios/Delete/5
        public async Task<ActionResult> Delete(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            comentario comentario = await db.comentarios.FindAsync(id);
            if (comentario == null)
            {
                return HttpNotFound();
            }
            return View(comentario);
        }

        // POST: comentarios/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(long id)
        {
            comentario comentario = await db.comentarios.FindAsync(id);
            db.comentarios.Remove(comentario);
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
