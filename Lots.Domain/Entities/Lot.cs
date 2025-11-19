using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lots.Domain.Entities;

public class Lot
{
    public int id { get; set; }
    public int? medicine_id { get; set; }   
    public string batch_number { get; set; } = string.Empty;
    public DateTime expiration_date { get; set; }
    public int quantity { get; set; }
    public decimal unit_cost { get; set; }
    public bool is_deleted { get; set; }
    public DateTime created_at { get; set; }
    public DateTime? updated_at { get; set; }
    public int? created_by { get; set; }
    public int? updated_by { get; set; }
}

