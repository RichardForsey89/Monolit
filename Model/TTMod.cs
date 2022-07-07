using RatStash;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TarkovToy.Model
{
    [Serializable]
    internal class TTMod
    {
        public TTBundle ParentBundle { get; set; }
        public string ID { get; set; } = "default";
        public string ShortName { get; set; } = "default";
        public int Price { get; set; } = 0;
        public double RecoilModifier { get; set; }
        public int Ergonomics { get; set; }
        public List <string> ConflictingItems { get; set; } = new List<string>();
        public List<TT_M_Slot> Slots { get; set; } = new List<TT_M_Slot>();

        public static List<TTMod> BuildModules(IEnumerable<WeaponMod> RatMods)
        {
            List<TTMod> modules = new List<TTMod>();

            foreach (var ratMod in RatMods)
            {
                // Initialize TTMod with attributes from RatMod
                TTMod tTMod = new TTMod
                {
                    ID = ratMod.Id,
                    ShortName = ratMod.Name,
                    Price = ratMod.CreditsPrice,
                    RecoilModifier = ratMod.Recoil,
                    Ergonomics = (int)ratMod.Ergonomics,
                    ConflictingItems = ratMod.ConflictingItems.ToList()
                };

                var tempslots = ratMod.Slots.ToList();
                foreach (var slot in tempslots)
                {
                    // Create a TTslot object
                    TT_M_Slot ttSlot = new TT_M_Slot
                    {
                        ID = slot.Id,
                        Name = slot.Name,
                        Required = slot.Required,
                    };

                    // Populate the whitelist for the slot
                    var whitelist = slot.Filters.First().Whitelist;
                    foreach (var filter in whitelist)
                    {
                        ttSlot.ModsWhitelist.Add(filter);
                    }

                    if (ttSlot.Required == false && ttSlot.Name != "mod_magazine")
                    {
                        // Add 'nothing' as acceptable
                        ttSlot.ModsWhitelist.Add("000");
                    }

                    // Add the slot to the TTMod

                    tTMod.Slots.Add(ttSlot);
                }
                modules.Add(tTMod);
            }
            return modules;
        }

        public static void MyMethod(TTMod input, IEnumerable<TTMod> ModMasterList)
        {

        }
    }
}
