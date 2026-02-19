using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using APITest.Data;

#nullable disable

namespace APITest.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260219_CreateLogins")]
    public partial class CreateLogins : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'ccloglogin', N'U') IS NULL
BEGIN
    CREATE TABLE [ccloglogin] (
        [Log_id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [User_id] INT NOT NULL,
        [Extension] INT NOT NULL,
        [TipoMov] INT NOT NULL,
        [fecha] DATETIME2 NOT NULL
    );
END
ELSE
BEGIN
    IF COL_LENGTH('ccloglogin', 'Log_id') IS NULL AND COL_LENGTH('ccloglogin', 'Id') IS NOT NULL
    BEGIN
        EXEC sp_rename 'ccloglogin.Id', 'Log_id', 'COLUMN';
    END;
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ccloglogin");
        }
    }
}
