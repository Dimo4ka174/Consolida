using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

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
