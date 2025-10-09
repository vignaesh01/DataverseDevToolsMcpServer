using Microsoft.Crm.Sdk.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataverseDevToolsMcpServer.Helpers
{
    public class SecurityManagementHelper
    {
        public static string PrivilegeDepthToString(int depthMask)
        {
            return depthMask switch
            {
                1 => "Basic (User)",
                2 => "Local (Business Unit)",
                4 => "Deep (BU + Child BUs)",
                8 => "Global (Organization)",
                _ => "None"
            };
        }

        public static string AccessRightToString(int accessRight)
        {
            return accessRight switch
            {
                1 => "Read",
                2 => "Write",
                4 => "Append",
                16 => "AppendTo",
                32 => "Create",
                65536 => "Delete",
                262144 => "Share",
                524288 => "Assign",
                _ => $"Unknown({accessRight})"
            };
        }


    }
}
