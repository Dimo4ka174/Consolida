using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Text;
using System;

namespace DB.Entity.Enum
{
    public enum Country
    {
        [Display(Name = "Россия")]
        Russia = 1,
        [Display(Name = "Китай")]
        China = 2
    }
}
