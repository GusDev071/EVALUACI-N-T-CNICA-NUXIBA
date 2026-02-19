using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APITest.Models;

[Table("ccUsers")]
public class CcUser
{
    [Key]
    [Column("User_id")]
    public int UserId { get; set; }

    [Column("Login")]
    public string Login { get; set; } = string.Empty;

    [Column("Nombres")]
    public string Nombres { get; set; } = string.Empty;

    [Column("ApellidoPaterno")]
    public string ApellidoPaterno { get; set; } = string.Empty;

    [Column("ApellidoMaterno")]
    public string ApellidoMaterno { get; set; } = string.Empty;

    [Column("IDArea")]
    public int? IDArea { get; set; }
}
