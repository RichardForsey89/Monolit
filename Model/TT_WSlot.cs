using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TarkovToy.Model
{
    internal class TT_WSlot
    {
        public string ID { get; set; } = "default";
        public string Name { get; set; } = "default";
        public bool Required { get; set; } = false;
        public HashSet<string> IDWhitelist { get; set; } = new HashSet<string>();
        public TTBundle? InstalledBundle { get; set; }

        public void RemoveBundle()
        {
            if (InstalledBundle != null)
            {
                InstalledBundle = null;
            }
        }

        public bool ValidBundle(string ID)
        {
            return IDWhitelist.Contains(ID);
        }

        // Do I even need this method?
        public void InstallBundle(TTBundle bundle)
        {
            // Do I reall need this check?
            if (InstalledBundle == null)
            {
                if (IDWhitelist.Contains(bundle.ID) == true)
                {
                    InstalledBundle = bundle;
                }
            }
        }
    }
}
