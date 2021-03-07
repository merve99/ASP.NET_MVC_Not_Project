using MyEvernote.BusinessLayer;
using MyEvernote.BusinessLayer.Results;
using MyEvernote.Entities;
using MyEvernote.Entities.Messages;
using MyEvernote.Entities.ValueObjects;
using MyEvernote.WebApp.Filters;
using MyEvernote.WebApp.Models;
using MyEvernote.WebApp.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace MyEvernote.WebApp.Controllers
{
    [Exc]
    public class HomeController : Controller
    {
        private NoteManager noteManager = new NoteManager();
        private CategoryManager categoryManager = new CategoryManager();
        private EvernoteUserManager evernoteUserManager = new EvernoteUserManager();

  

        public ActionResult Index()
        {
            return View(noteManager.ListQueryable().Where(x => x.IsDraft == false).OrderByDescending(x => x.ModifiedOn).ToList());
        }
        public ActionResult Endex()
        {
            return View(noteManager.ListQueryable().Where(x => x.IsDraft == false).OrderByDescending(x => x.ModifiedOn).ToList());
        }
        public ActionResult ByCategory(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            List<Note> notes = noteManager.ListQueryable().Where(
                x => x.IsDraft == false && x.CategoryId == id).OrderByDescending(
                x => x.ModifiedOn).ToList();

            return View("Endex", notes);
        }

        public ActionResult MostLiked()
        {
            return View("Endex", noteManager.ListQueryable().OrderByDescending(x => x.LikeCount).ToList());
        }

        public ActionResult About()
        {
            return View();
        }


        [Auth]
        public ActionResult ShowProfile()
        {
            BusinessLayerResult<EvernoteUser> res =
                evernoteUserManager.GetUserById(CurrentSession.User.Id);
            
            if (res.Errors.Count > 0)
            {
                ErrorViewModel errorNotifyObj = new ErrorViewModel()
                {
                    Title = "Hata Oluştu",
                    Items = res.Errors
                };

                return View("Error", errorNotifyObj);
            }

            return View(res.Result);
        }
        public ActionResult UserNoteTask(int id)
        {
            List<Note> notes = noteManager.ListQueryable().Where(
                x => x.IsDraft == true && x.Owner.Id == id).OrderByDescending(
                x => x.ModifiedOn).ToList();

            return View("Endex", notes);
        }

        public ActionResult UserLikes(int id)
        {
            List<Note> notes = noteManager.ListQueryable().Where(
                x => x.IsDraft == false && x.Owner.Id == id).OrderByDescending(
                x => x.ModifiedOn).ToList();

            return View(notes);
        }
        private LikedManager likedManager = new LikedManager();
        [Auth]
        public ActionResult MyLikedNotes()
        {
            var notes = likedManager.ListQueryable().Include("LikedUser").Include("Note").Where(
                x => x.LikedUser.Id == CurrentSession.User.Id).Select(
                x => x.Note).Include("Category").Include("Owner").OrderByDescending(
                x => x.ModifiedOn);

            return View("UserLikes", notes.ToList());
        }
        [Auth]
        public ActionResult EditProfile()
        {
            BusinessLayerResult<EvernoteUser> res = evernoteUserManager.GetUserById(CurrentSession.User.Id);

            if (res.Errors.Count > 0)
            {
                ErrorViewModel errorNotifyObj = new ErrorViewModel()
                {
                    Title = "Hata Oluştu",
                    Items = res.Errors
                };

                return View("Error", errorNotifyObj);
            }

            return View(res.Result);
        }

        [Auth]
        [HttpPost]
        public ActionResult EditProfile(EvernoteUser model, HttpPostedFileBase ProfileImage)
        {
            ModelState.Remove("ModifiedUsername");

            if (ModelState.IsValid)
            {
                if (ProfileImage != null &&
                    (ProfileImage.ContentType == "image/jpeg" ||
                    ProfileImage.ContentType == "image/jpg" ||
                    ProfileImage.ContentType == "image/png"))
                {
                    string filename = $"user_{model.Id}.{ProfileImage.ContentType.Split('/')[1]}";

                    ProfileImage.SaveAs(Server.MapPath($"~/images/{filename}"));
                    model.ProfileImageFilename = filename;
                }

                BusinessLayerResult<EvernoteUser> res = evernoteUserManager.UpdateProfile(model);

                if (res.Errors.Count > 0)
                {
                    ErrorViewModel errorNotifyObj = new ErrorViewModel()
                    {
                        Items = res.Errors,
                        Title = "Profil Güncellenemedi.",
                        RedirectingUrl = "/Home/EditProfile"
                    };

                    return View("Error", errorNotifyObj);
                }

                CurrentSession.Set<EvernoteUser>("login", res.Result);

                return RedirectToAction("ShowProfile");
            }

            return View(model);
        }

        [Auth]
        public ActionResult DeleteProfile()
        {
            BusinessLayerResult<EvernoteUser> res =
                evernoteUserManager.RemoveUserById(CurrentSession.User.Id);

            if (res.Errors.Count > 0)
            {
                ErrorViewModel errorNotifyObj = new ErrorViewModel()
                {
                    Items = res.Errors,
                    Title = "Profil Silinemedi.",
                    RedirectingUrl = "/Home/ShowProfile"
                };

                return View("Error", errorNotifyObj);
            }

            Session.Clear();

            return RedirectToAction("Index");
        }



        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                BusinessLayerResult<EvernoteUser> res = evernoteUserManager.LoginUser(model);

                if (res.Errors.Count > 0)
                {
                   res.Errors.ForEach(x => ModelState.AddModelError("", x.Message));

                    return View(model);
                }

                CurrentSession.Set<EvernoteUser>("login", res.Result); 
                return RedirectToAction("Index"); 
            }

            return View(model);
        }
        
        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                BusinessLayerResult<EvernoteUser> res = evernoteUserManager.RegisterUser(model);

                if (res.Errors.Count > 0)
                {
                    res.Errors.ForEach(x => ModelState.AddModelError("", x.Message));
                    return View(model);
                }
                OkViewModel notifyObj = new OkViewModel()
                {
                    Title = "Kayıt Başarılı",
                    RedirectingUrl = "/Home/Login",
                };

                notifyObj.Items.Add("Lütfen e-posta adresinize gönderdiğimiz aktivasyon link'ine tıklayarak hesabınızı aktive ediniz. Hesabınızı aktive etmeden not ekleyemez ve beğenme yapamazsınız.");

                return View("Ok", notifyObj);
            }

            return View(model);
        }
        public ActionResult ForgotPass()
        {
            return View();
        }

        [HttpPost]
        public ActionResult ForgotPass(ForgotPassViewModel model)
        {
            if (ModelState.IsValid)
            {
                BusinessLayerResult<EvernoteUser> res = evernoteUserManager.ForgotPassword(model);

                if (res.Errors.Count > 0)
                {
                    res.Errors.ForEach(x => ModelState.AddModelError("", x.Message));
                    return View(model);
                }
                OkViewModel notifyObj = new OkViewModel()
                {
                    Title = "Şifre Sıfırlama Talebi Oluşturuldu",
                    RedirectingUrl = "/Home/Login",
                };

                notifyObj.Items.Add("Lütfen e-posta adresinize gönderdiğimiz şifre sıfırmala linkine tıklayarak yeni şifre belirleyiniz.");

                return View("Ok", notifyObj);
            }

            return View(model);
        }
        public ActionResult ResetPassword(Guid id)
        {
            BusinessLayerResult<EvernoteUser> res = evernoteUserManager.DeleteResetPassGuid(id);
            if (res.Errors.Count > 0)
            {
                ErrorViewModel errorNotifyObj = new ErrorViewModel()
                {
                    Title = "Geçersiz İşlem",
                    Items = res.Errors
                };

                return View("Error", errorNotifyObj);
            }
            else
            {
            return View();
            }
            
        }
        [HttpPost]
        public ActionResult ResetPassword(ResetPassViewModel model)
        {
            BusinessLayerResult<EvernoteUser> res = evernoteUserManager.ResetPassword(model);

            if (res.Errors.Count > 0)
            {
                ErrorViewModel errorNotifyObj = new ErrorViewModel()
                {
                    Title = "Geçersiz İşlem",
                    Items = res.Errors
                };

                return View("Error", errorNotifyObj);
            }

            OkViewModel okNotifyObj = new OkViewModel()
            {
                Title = "Şifre Sıfırlandı",
                RedirectingUrl = "/Home/Login"
            };

            okNotifyObj.Items.Add("Şifre değişiliğiniz başarılı bir şekilde gerçekleştirildi.");

            return View("Ok", okNotifyObj);
        }
        

        public ActionResult UserActivate(Guid id)
        {
            BusinessLayerResult<EvernoteUser> res = evernoteUserManager.ActivateUser(id);

            if (res.Errors.Count > 0)
            {
                ErrorViewModel errorNotifyObj = new ErrorViewModel()
                {
                    Title = "Geçersiz İşlem",
                    Items = res.Errors
                };

                return View("Error", errorNotifyObj);
            }

            OkViewModel okNotifyObj = new OkViewModel()
            {
                Title = "Hesap Aktifleştirildi",
                RedirectingUrl = "/Home/Login"
            };

            okNotifyObj.Items.Add("Hesabınız aktifleştirildi. Artık not paylaşabilir ve beğenme yapabilirsiniz.");

            return View("Ok", okNotifyObj);
        }



        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Index");
        }


        public ActionResult AccessDenied()
        {
            return View();
        }


        public ActionResult HasError()
        {
            return View();
        }
    }
}