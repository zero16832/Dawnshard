﻿// <auto-generated />
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DragaliaAPI.Database.Migrations
{
    /// <inheritdoc />
    public partial class questclearparties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QuestClearPartyUnits",
                columns: table => new
                {
                    DeviceAccountId = table.Column<string>(type: "text", nullable: false),
                    QuestId = table.Column<int>(type: "integer", nullable: false),
                    IsMulti = table.Column<bool>(type: "boolean", nullable: false),
                    UnitNo = table.Column<int>(type: "integer", nullable: false),
                    CharaId = table.Column<int>(type: "integer", nullable: false),
                    EquipDragonKeyId = table.Column<long>(type: "bigint", nullable: false),
                    EquipWeaponBodyId = table.Column<int>(type: "integer", nullable: false),
                    EquipCrestSlotType1CrestId1 = table.Column<int>(type: "integer", nullable: false),
                    EquipCrestSlotType1CrestId2 = table.Column<int>(type: "integer", nullable: false),
                    EquipCrestSlotType1CrestId3 = table.Column<int>(type: "integer", nullable: false),
                    EquipCrestSlotType2CrestId1 = table.Column<int>(type: "integer", nullable: false),
                    EquipCrestSlotType2CrestId2 = table.Column<int>(type: "integer", nullable: false),
                    EquipCrestSlotType3CrestId1 = table.Column<int>(type: "integer", nullable: false),
                    EquipCrestSlotType3CrestId2 = table.Column<int>(type: "integer", nullable: false),
                    EquipTalismanKeyId = table.Column<long>(type: "bigint", nullable: false),
                    EquipWeaponSkinId = table.Column<int>(type: "integer", nullable: false),
                    EditSkill1CharaId = table.Column<int>(type: "integer", nullable: false),
                    EditSkill2CharaId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestClearPartyUnits", x => new { x.DeviceAccountId, x.QuestId, x.IsMulti, x.UnitNo });
                    table.ForeignKey(
                        name: "FK_QuestClearPartyUnits_Players_DeviceAccountId",
                        column: x => x.DeviceAccountId,
                        principalTable: "Players",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuestClearPartyUnits");
        }
    }
}