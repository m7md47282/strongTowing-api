using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DALfirst
{
    class ColServices:DbContext
    {
        public DbSet<services_req> request_table { get; set; }

        public TowingCollection(DbContextOptions<TowingCollection> options) : base(options)
        {

        }
    }
}
