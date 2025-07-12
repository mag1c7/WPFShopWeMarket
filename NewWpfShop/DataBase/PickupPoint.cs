using System;
using System.Collections.Generic;

namespace NewWpfShop.DataBase;

public partial class PickupPoint
{
    public int PickupPointId { get; set; }

    public string Address { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
