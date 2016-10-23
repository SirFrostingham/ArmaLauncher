using System.Collections.Generic;

namespace ArmaLauncher.Models
{
    public class ServerRulesModel
    {
        public string RuleName { get; set; }
        public List<string> ServerContainsList { get; set; }
        public string ClientMod { get; set; }
    }
}