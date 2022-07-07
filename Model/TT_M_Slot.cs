using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TarkovToy.Model
{
    [Serializable]
    internal class TT_M_Slot
    {
        public TTBundle ParentBundle { get; set; }
        public string ID { get; set; } = "default";
        public string Name { get; set; } = "default";
        public bool Required { get; set; } = false;
        public HashSet<string> ModsWhitelist { get; set; } = new HashSet<string>();
        public TTMod? InstalledMod { get; set; }

        public void RemoveMod()
        {
            if (InstalledMod != null)
            {
                if (ParentBundle != null)
                {
                    ParentBundle.CompoundName = ParentBundle.CompoundName.Remove(ParentBundle.CompoundName.Length - (1 + InstalledMod.ShortName.Length));
                    ParentBundle.TotalPrice -= InstalledMod.Price;
                    ParentBundle.TotalErgo -= InstalledMod.Ergonomics;
                    ParentBundle.TotalRecoil -= InstalledMod.RecoilModifier;
                }
                InstalledMod = null;
                //Console.WriteLine("A Mod was removed");
            }
        }

        //Contract: a mod that is installed with this has already been whitelsited
        public void InstallMod(TTMod mod, TTBundle parent)
        {
            if(InstalledMod != null)
            {
                RemoveMod();
            }
            InstalledMod = mod;

            if (ParentBundle == null)
                ParentBundle = parent;

            InstalledMod.ParentBundle = ParentBundle;
            if (ParentBundle != null)
            {
                ParentBundle.updateDeets(InstalledMod);
            }
            else
            {
                Console.WriteLine("There's an orphan!");
            }
            
            //Console.WriteLine("A Mod was added");
        }
    }
}
