using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace MyEvernote.Entities.ValueObjects
{
    public class ForgotPassViewModel
    {
    [DisplayName("E-posta"),
    Required(ErrorMessage = "{0} alanı boş geçilemez."),
    StringLength(70, ErrorMessage = "{0} max. {1} karakter olmalı."),
    EmailAddress(ErrorMessage = "{0} alanı için geçerli bir e-posta adresi giriniz.")]
    public string EMail { get; set; }
    }
}
