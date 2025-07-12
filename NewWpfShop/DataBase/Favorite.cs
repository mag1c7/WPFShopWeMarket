using System;
using System.Collections.Generic;

namespace NewWpfShop.DataBase;

public partial class Favorite
{
    public int FavoriteId { get; set; }

    public int UserId { get; set; }

    public int ProductId { get; set; }

    public DateTime AddedDate { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
