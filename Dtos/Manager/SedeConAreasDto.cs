using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dtos.Manager
{
    public class SedeConAreasDto
    {
        public string SiteId { get; set; }
        public string SiteName { get; set; }
        public List<AreaDto> Areas { get; set; } = new();

        public List<CostCenterDto> CostCenters { get; set; } = new();
    }
}
