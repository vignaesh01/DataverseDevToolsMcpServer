using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataverseDevToolsMcpServer.Models
{
    public class Team
    {
        public Guid teamId { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public Guid businessUnitId { get; set; }
        public string businessUnitName { get; set; }
        public int teamType { get; set; }
        public string teamTypeName { get {
                return Enum.IsDefined(typeof(TeamType), teamType)
            ? ((TeamType)teamType).ToString()
            : "Unknown";
            } }

    }

   public enum TeamType
    {
        Owner = 0,
        Access = 1,
        SecurityGroup = 2,
        OfficeGroup = 3
    }
}
