﻿using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DragaliaAPI.Database.Entities;

[PrimaryKey(nameof(GameId), nameof(ViewerId))]
public class DbTimeAttackPlayer
{
    [ForeignKey(nameof(Clear))]
    public required string GameId { get; set; }

    [ForeignKey(nameof(Player))]
    public required long ViewerId { get; set; }

    /// <summary>
    /// Party info blob.
    /// </summary>
    /// <remarks>
    /// For manual inspection after contest ends.
    /// </remarks>
    [Column(TypeName = "jsonb")]
    public required string PartyInfo { get; set; }

    public List<DbTimeAttackClearUnit> Units { get; set; } = new();

    public DbTimeAttackClear Clear { get; set; } = null!;

    public DbPlayer Player { get; set; } = null!;
}
