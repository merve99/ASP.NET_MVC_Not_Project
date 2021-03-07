using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using MyEvernote.Entities;
using MyEvernote.WebApp.Models;
using MyEvernote.BusinessLayer;
using MyEvernote.WebApp.Filters;
using System.Web.Helpers;
using System.IO;

namespace MyEvernote.WebApp.Controllers
{
    [Exc]
    public class NoteController : Controller
    {
        private NoteManager noteManager = new NoteManager();
        private CategoryManager categoryManager = new CategoryManager();
        private LikedManager likedManager = new LikedManager();

        [Auth]
        public ActionResult Index()
        {
            var notes = noteManager.ListQueryable().Include("Category").Include("Owner").Where(
                x => x.Owner.Id == CurrentSession.User.Id).OrderByDescending(
                x => x.ModifiedOn);

            return View(notes.ToList());
        }


        [Auth]
        public ActionResult MyLikedNotes()
        {
            var notes = likedManager.ListQueryable().Include("LikedUser").Include("Note").Where(
                x => x.LikedUser.Id == CurrentSession.User.Id).Select(
                x => x.Note).Include("Category").Include("Owner").OrderByDescending(
                x => x.ModifiedOn);

            return View("Index", notes.ToList());
        }

        [Auth]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Note note = noteManager.Find(x => x.Id == id);
            if (note == null)
            {
                return HttpNotFound();
            }
            return View(note);
        }

        [Auth]
        public ActionResult Create()
        {
            ViewBag.CategoryId = new SelectList(CacheHelper.GetCategoriesFromCache(), "Id", "Title");
            return View();
        }

        [Auth]
        [HttpPost]
        [ValidateInput(false)]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Note note, HttpPostedFileBase NoteImage)
        {
            ModelState.Remove("CreatedOn");
            ModelState.Remove("ModifiedOn");
            ModelState.Remove("ModifiedUsername");

            if (ModelState.IsValid)
            {
                if (NoteImage != null)
                {
                    WebImage img = new WebImage(NoteImage.InputStream);
                    FileInfo imginfo = new FileInfo(NoteImage.FileName);

                    string noteimgname =NoteImage.FileName + imginfo.Extension;
                    img.Resize(350, 200);
                    img.Save("~/Images/" + noteimgname);

                    note.NoteImageFilename = "/Images/" + noteimgname;
                }
                note.Owner = CurrentSession.User;
                noteManager.Insert(note);
                return RedirectToAction("Index");
            }

            ViewBag.CategoryId = new SelectList(CacheHelper.GetCategoriesFromCache(), "Id", "Title", note.CategoryId);
            return View(note);
        }

        [Auth]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Note note = noteManager.Find(x => x.Id == id);
            if (note == null)
            {
                return HttpNotFound();
            }
            ViewBag.CategoryId = new SelectList(CacheHelper.GetCategoriesFromCache(), "Id", "Title", note.CategoryId);
            return View(note);
        }

        [Auth]
        [HttpPost]
        [ValidateInput(false)]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Note note, HttpPostedFileBase NoteImage)
        {
            ModelState.Remove("CreatedOn");
            ModelState.Remove("ModifiedOn");
            ModelState.Remove("ModifiedUsername");

            if (ModelState.IsValid)
            {
                Note db_note = noteManager.Find(x => x.Id == note.Id);
                if (NoteImage != null)
                {
                    if (System.IO.File.Exists(Server.MapPath(db_note.NoteImageFilename)))
                    {
                        System.IO.File.Delete(Server.MapPath(db_note.NoteImageFilename));
                    }
                    WebImage img = new WebImage(NoteImage.InputStream);
                    FileInfo imginfo = new FileInfo(NoteImage.FileName);

                    string noteimgname = NoteImage.FileName + imginfo.Extension;
                    img.Resize(350, 200);
                    img.Save("~/Images/" + noteimgname);
                    note.NoteImageFilename = "/Images/" + noteimgname;
                    db_note.NoteImageFilename = note.NoteImageFilename;

                }
                db_note.IsDraft = note.IsDraft;
                db_note.CategoryId = note.CategoryId;
                db_note.Text = note.Text;
                db_note.Title = note.Title;
                db_note.ModifiedOn = DateTime.Now;
                noteManager.Update(db_note);

                return RedirectToAction("Index");
            }
            ViewBag.CategoryId = new SelectList(CacheHelper.GetCategoriesFromCache(), "Id", "Title", note.CategoryId);
            return View(note);
        }

        [Auth]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Note note = noteManager.Find(x => x.Id == id);
            if (note == null)
            {
                return HttpNotFound();
            }
            return View(note);
        }

        [Auth]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Note note = noteManager.Find(x => x.Id == id);
            if (System.IO.File.Exists(Server.MapPath(note.NoteImageFilename)))
            {
                System.IO.File.Delete(Server.MapPath(note.NoteImageFilename));
            }
            noteManager.Delete(note);
            return RedirectToAction("Index");
        }


        [HttpPost]
        public ActionResult GetLiked(int[] ids)
        {
            if(CurrentSession.User != null)
            {
                int userid = CurrentSession.User.Id;
                List<int> likedNoteIds = new List<int>();

                if (ids != null)
                {
                    likedNoteIds = likedManager.List(
                        x => x.LikedUser.Id == userid && ids.Contains(x.Note.Id)).Select(
                        x => x.Note.Id).ToList();
                }
                else
                {
                    likedNoteIds = likedManager.List(
                        x => x.LikedUser.Id == userid).Select(
                        x => x.Note.Id).ToList();
                }

                return Json(new { result = likedNoteIds });
            }
            else
            {
                return Json(new { result = new List<int>() });
            }
        }

        [HttpPost]
        public ActionResult SetLikeState(int noteid, bool liked)
        {
            int res = 0;

            if(CurrentSession.User == null)
                return Json(new { hasError = true, errorMessage = "Beğenme işlemi için giriş yapmalısınız.", result = 0 });

            Liked like =
                likedManager.Find(x => x.Note.Id == noteid && x.LikedUser.Id == CurrentSession.User.Id);

            Note note = noteManager.Find(x => x.Id == noteid);

            if (like != null && liked == false)
            {
                res = likedManager.Delete(like);
            }
            else if (like == null && liked == true)
            {
                res = likedManager.Insert(new Liked()
                {
                    LikedUser = CurrentSession.User,
                    Note = note
                });
            }

            if (res > 0)
            {
                if (liked)
                {
                    note.LikeCount++;
                }
                else
                {
                    note.LikeCount--;
                }

                res = noteManager.Update(note);

                return Json(new { hasError = false, errorMessage = string.Empty, result = note.LikeCount });
            }

            return Json(new { hasError = true, errorMessage = "Beğenme işlemi gerçekleştirilemedi.", result = note.LikeCount });
        }


        public ActionResult GetNoteText(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Note note = noteManager.Find(x => x.Id == id);

            if (note == null)
            {
                return HttpNotFound();
            }

            return PartialView("_PartialNoteText", note);
        }
    }
}
