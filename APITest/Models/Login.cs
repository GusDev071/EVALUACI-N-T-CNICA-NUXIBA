using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APITest.Models;

[Table("ccloglogin")]
public class Login
{
    [Key]
    [Column("Log_id")]
    public int LogId { get; set; }

    [Column("User_id")]
    public int UserId { get; set; }

    [Column("Extension")]
    public string? Extension { get; set; }

    public int TipoMov { get; set; }

    [Column("fecha")]
    public DateTime Fecha { get; set; }
}
