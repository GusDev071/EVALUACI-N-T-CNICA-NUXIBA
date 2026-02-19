using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APITest.Models;

[Table("ccRIACat_Areas")]
public class Area
{
    [Key]
    [Column("IDArea")]
    public int IDArea { get; set; }

    [Column("AreaName")]
    public string AreaName { get; set; } = string.Empty;

    [Column("StatusArea")]
    public int StatusArea { get; set; }

    [Column("CreateDate")]
    public DateTime CreateDate { get; set; }
}
