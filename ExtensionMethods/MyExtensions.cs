using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using RatStash;
using System.Collections;
using System.Globalization;
using Newtonsoft.Json.Converters;
using System.Reflection;

namespace RatStash
{
    public class Ext_Ammo : Ammo
    {
        [Newtonsoft.Json.JsonProperty("PenetrationPower")]
        public int PenetrationPower { get; set; }

        public Ext_Ammo(int pen, Ammo ammo)
        {
            PropertyInfo[] properties = typeof(Ammo).GetProperties();
            foreach (var p in properties.Where(prop => prop.CanRead && prop.CanWrite))
                p.SetMethod.Invoke(this, new object[] { p.GetMethod.Invoke(ammo, null) });

            PenetrationPower = pen;
        }
    }
}

namespace Information
{
   
}

namespace TarkovToy.ExtensionMethods
{
    // Make this be a flywheel
    // Need to look at how the trader offers are made, and then consider how info is paired
    // It may not be possible to have flyweights due to trader price fuckery with weapoon deals
    public class Trader
    {
        public string? Id { get; set; }
        public string? Name { get; set; }

        // Trader Info
        public int TraderLLRequiredToBuy { get; set; }
        public int PlayerLevelRequiredToBuy { get; set; }
        public int BasePrice { get; set; }
        public MarketTrader? BuyForFrom { get; set; } // Who sells the thing cheapest???
        public MarketTrader? SellForTo { get; set; } // Who will give the best price when sold to???

        // Fleamarket Info
        public int low24hPrice { get; set; }
        public int exp24hPrice { get; set; } // Expected price is simply the midpoint between low and avg
        public int avg24hPrice { get; set; }
        public int high24hPrice { get; set; }
    }

    public class MarketTrader
    {
        public string? TraderName { get; set; }
        public int PriceRUB { get; set; } // Use this for comparisions
        public int Price { get; set; }
        public string? Currency { get; set; } // Make this an enum later
    }

    public static class API_ExtensionMethods
    {
        //TODO: setup some of these
    }

    public static class Simple
    {
        public static (Weapon, Ext_Ammo) selectAmmo(Weapon weapon, List<Ext_Ammo> ammo, string mode)
        {
            Ext_Ammo bullet = null;
            ammo = ammo.FindAll(ammo => ammo.Caliber.Equals(weapon.AmmoCaliber));
            if (ammo.Count > 0)
            {
                if (mode.Equals("damage"))
                    ammo.OrderByDescending(ammo => ammo.Damage);
                else if (mode.Equals("penetration"))
                    ammo.OrderByDescending(ammo => ammo.PenetrationPower);
                bullet = ammo.First();
            }
            return (weapon, bullet);
        }

        public static void inspectList<T>(List<WeaponMod> aList)
        {
            var result = aList.Where(x => x.GetType() == typeof(T)).ToList();
            result.ForEach((x) =>
            {
                Console.WriteLine(x.Name);
                Console.WriteLine(x.Id);
                //Console.WriteLine("e: " + x.Ergonomics);
                //Console.WriteLine("r: " + x.Recoil);
                //Console.WriteLine("c: " + x.CreditsPrice);
                //Console.WriteLine("");
                x.ConflictingItems.ForEach(y => Console.WriteLine(y));
            });
        }
        public static void inspectList(List<WeaponMod> aList, List<WeaponMod> allmods)
        {
            aList.ForEach((x) =>
            {
                Console.WriteLine(x.Name);
                Console.WriteLine(x.Id);
                //Console.WriteLine("e: " + x.Ergonomics);
                //Console.WriteLine("r: " + x.Recoil);
                //Console.WriteLine("c: " + x.CreditsPrice);
                //Console.WriteLine("");
                x.ConflictingItems = x.ConflictingItems.Where(x => allmods.Any(y => y.Id==x)).ToList();
                x.ConflictingItems.ForEach(y => {
                    Console.WriteLine(y);
                    var match = allmods.FirstOrDefault(z => z.Id == y);
                    if (match != null)
                        Console.WriteLine("  " + match.Name + " type: " + match.GetType().Name);
                });
                Console.WriteLine("");
            });
        }
    }


    public static class Recursion
    {
        public static bool sameOrWorseStats(WeaponMod original , WeaponMod candidate)
        {
            bool result = false;

            if (original.Ergonomics >= candidate.Ergonomics && original.Recoil <= candidate.Recoil)
                result = true;

            return result;
        }
        public static bool dasBOOT((Trader md, WeaponMod wm) original, (Trader md, WeaponMod wm) candidate)
        {
            bool result = true;

            int original_PurchaseValue = original.md.BuyForFrom.PriceRUB;
            int original_SaleValue = original.md.SellForTo.PriceRUB;

            int candidate_PurchaseValue = candidate.md.BuyForFrom.PriceRUB;
            int candidate_SaleValue = candidate.md.SellForTo.PriceRUB;

            if (original_SaleValue < candidate_PurchaseValue)
                result = false;

            return result;
        }

        public static Weapon JankyCloner (Weapon original)
        {
            Weapon clone = new();

            PropertyInfo[] properties = typeof(Weapon).GetProperties();
            foreach (var p in properties.Where(prop => prop.CanRead && prop.CanWrite))
                p.SetMethod.Invoke(clone, new object[] { p.GetMethod.Invoke(original, null) });

            return clone;
        }

        public static bool HeadToHead(WeaponMod excluder, IEnumerable<WeaponMod> mods, Weapon CI)
        {
            Console.WriteLine(CI.Name);
            Console.WriteLine(excluder.Name);
            List<WeaponMod> allmods = mods.ToList();

            // Clone the CI into two dummies <-Actually just need one.
            Weapon excluder_dummy = new(); excluder_dummy = JankyCloner(CI);
            Weapon candidate_dummy = new(); candidate_dummy = JankyCloner(CI);

            // Gonna make sure there are no confounding items
            excluder_dummy.Slots.ForEach(x=>x.ContainedItem=null); 
            candidate_dummy.Slots.ForEach(x => x.ContainedItem = null);

            // Need to get the oppurtunity slot (heh) options and remove the excluder from it
            // Possibly need to remove other excluders <- YES, fucking ADAR 2-15 stock.....
            var target = excluder_dummy.Slots.FirstOrDefault(x => x.Filters[0].Whitelist.Contains(excluder.Id));
            List<string> otherOptions = new();
            if (target != null) { }
                otherOptions = target.Filters[0].Whitelist.Where(x => !x.Equals(excluder.Id)).ToList();
            
            excluder.ConflictingItems.AddRange(otherOptions); // Actually, I should be putting this into a seperate lsit rather than modifying the list

            excluder.ConflictingItems.RemoveAll(x => allmods.Any(y => y.Id.Equals(x) && y.ConflictingItems.Count > 0)); // Get rid of anything that is also a conflicter
            
            // Filter out any conflicting item IDs which aren't in the master list
            excluder.ConflictingItems = excluder.ConflictingItems.Where(x => allmods.Any(y => y.Id == x)).ToList(); //wtf is this line

            // Check that the list is what you expect
            excluder.ConflictingItems = excluder.ConflictingItems.Where(x => allmods.Any(y => y.Id == x)).ToList(); // why does it repeat??
            excluder.ConflictingItems.ForEach(y => {
                Console.WriteLine(y);
                var match = allmods.FirstOrDefault(z => z.Id == y);
                if (match != null)
                    Console.WriteLine("  " + match.Name + " type: " + match.GetType().Name);
                else
                    Console.WriteLine("ERROR: " + y + "NOT FOUND");
            });

            int best_ergo = 0;
            float best_recoil = 0;

            // Don't need to interate through all items, as the add base attachments method will build a version of that weapon with the best combo
            candidate_dummy = hacky_addBaseAttachments(candidate_dummy, excluder.ConflictingItems, mods, "recoil");

            var c_d_res = accumulateTotals(candidate_dummy.Slots);
            var e_d_res = getModTotals(excluder);

            if (c_d_res.t_ergo > e_d_res.t_ergo || c_d_res.t_recoil < e_d_res.t_recoil)
                Console.WriteLine($"Excluder {excluder.Name} lost vs recoil combo e{e_d_res} vs c{c_d_res}");
            else
                Console.WriteLine($"Excluder {excluder.Name} WINS! vs recoil combo e{e_d_res} vs c{c_d_res}");

            candidate_dummy = hacky_addBaseAttachments(candidate_dummy, excluder.ConflictingItems, mods, "ergo");

            c_d_res = accumulateTotals(candidate_dummy.Slots);
            e_d_res = getModTotals(excluder);

            if (c_d_res.t_ergo > e_d_res.t_ergo || c_d_res.t_recoil < e_d_res.t_recoil)
                Console.WriteLine($"Excluder {excluder.Name} lost vs ergo combo e{e_d_res} vs c{c_d_res}");
            else
                Console.WriteLine($"Excluder {excluder.Name} WINS! vs ergo combo e{e_d_res} vs c{c_d_res}");

            // Just need to set this up so that it correctly has a process flow control bertween recoil and ergo.
            // Need to check that the limited list is accurate and that an open list wouldn't be better. <-Limited is fine once you account for the selfslot items

            // For the main loop need to consider how it will interact
            // Perhaps the way to go would be to call a method which removes any offending item that has already been placed, and re-runs the fitting with the
            // exclusion list applied. The other half would of course be to make further fitting choices consider the exclusion list. Perhaps the way to do this
            // would be to just simply remove the offending items from the master mod list? wouldn't need to update the main method that way and
            // accomplishes the goal.
            return false;
        }
        public static Weapon hacky_addBaseAttachments(this Weapon weapon, List<string> baseAttachments, IEnumerable<WeaponMod> mods, string mode)
        {
            List<WeaponMod> attachmentsList = mods.Where(x => baseAttachments.Contains(x.Id)).ToList();
            weapon = (Weapon) recursiveFit(weapon, attachmentsList, mode);

            //weapon.Slots = addBaseAttachmentsFlatLoop(weapon.Slots, attachmentsList, weapon);
            return weapon;
        }

        public static Weapon addBaseAttachments(this Weapon weapon, List<string> baseAttachments, IEnumerable<WeaponMod> mods)
        {
            List<WeaponMod> attachmentsList = mods.Where(x => baseAttachments.Contains(x.Id)).ToList();
            weapon = (Weapon) recursiveFit(weapon, attachmentsList, "base");

            //weapon.Slots = addBaseAttachmentsFlatLoop(weapon.Slots, attachmentsList, weapon);
            return weapon;
        }

        // TODO:  add combinational sorting, add checks for incompatible items.
        //  Implement an efficency grading comparision.
        public static CompoundItem recursiveFit(CompoundItem CI, IEnumerable<WeaponMod> mods, string mode)
        {
            // Need to account for the possibility of base mods
            // Need to make the efficency ranking as a helper methods used in OrderBy()
            // Need to add guard condition for if the stat is 0, then it sorts by the other stat
            // Need to add guard that if the module in the slot is a default && the new mod is equal or worse in that stat, skip.

            // Idea: "Marginal benefit" check that sees if the value of sold default - cost of bought replacement is positive
            // Idea: Use a simple cull method where items which match or are less than the default mod are removed from shortlist

            // TODO: Implement a vulgar efficency comparision of just stat/credit value
            // TODO: Implement a way of comparing vs blocking items and selecting for the better option.


            // NOTE: One edge case is muzzle devices; it isn't a choice between ergo and recoil, but loud and supression and for both wanting lowest recoil.
            // This edge case has been somewhat accoutned for, but needs a review

            foreach (Slot slot in CI.Slots)
            {
                List<WeaponMod> shortList = mods.Where(item => slot.Filters[0].Whitelist.Contains(item.Id)).ToList();
                List<WeaponMod>? candidatesList = new();

                candidatesList.AddRange(shortList.Select(item => (WeaponMod)recursiveFit(item, mods, mode)));

                if (shortList.Count > 0)
                {
                    // repalce this with a switch when you add more options.
                    if (mode.Equals("ergo") && !slot.Name.Contains("mod_muzzle"))
                    {
                        // Simple cull step if the slot currently has a default option
                        if (slot.ContainedItem != null)
                        {
                            var original = (WeaponMod)slot.ContainedItem;
                            candidatesList = candidatesList.Where(item => getModTotals(item).t_ergo > getModTotals(original).t_ergo).ToList();
                        }
                        if (candidatesList.Count > 0)
                        {
                            // Sort options by trait
                            candidatesList = candidatesList.OrderByDescending(x => getModTotals(x).t_ergo).ToList();

                            // Check if there are multiple best options and then sort by lowest price to best
                            candidatesList = candidatesList.Where(x => getModTotals(x).t_ergo == getModTotals(candidatesList.First()).t_ergo).ToList();
                            candidatesList = candidatesList.OrderBy(x => getModTotals(x).t_price).ToList();
                        }
                        
                    }
                    else if (mode.Equals("recoil") || slot.Name.Contains("mod_muzzle") == true)
                    {
                        // Simple cull step if the slot currently has a default option
                        if (slot.ContainedItem != null)
                        {
                            var original = (WeaponMod)slot.ContainedItem;
                            candidatesList = candidatesList.Where(item => getModTotals(item).t_recoil < getModTotals(original).t_recoil).ToList();
                        }

                        if (candidatesList.Count > 0)
                        {
                            // Sort options by trait
                            candidatesList = candidatesList.OrderBy(x => getModTotals(x).t_recoil).ToList();

                            // Check if there are multiple best options and then sort by lowest price to best
                            candidatesList = candidatesList.Where(x => getModTotals(x).t_recoil == getModTotals(candidatesList.First()).t_recoil).ToList();
                            candidatesList = candidatesList.OrderBy(x => getModTotals(x).t_price).ToList();
                        }
                    }
                    if(candidatesList.Count > 0)
                        slot.ContainedItem = candidatesList.First();
                }

                slot.ParentItem = CI;
            }

            return CI;
        }

        public static List<WeaponMod> accumulateMods(this List<Slot> slots)
        {
            List<WeaponMod> attachedMods = new List<WeaponMod>();
            IEnumerable<Slot> notNulls = slots.Where(x => x.ContainedItem != null);

            foreach (Slot slot in notNulls)
            {
                WeaponMod wm = (WeaponMod)slot.ContainedItem;
                attachedMods.Add(wm);
                attachedMods.AddRange(accumulateMods(wm.Slots));
            }

            return attachedMods;
        }

        public static (int t_price, int t_ergo, float t_recoil) getWeaponTotals(this Weapon w)
        {
            var ts = accumulateTotals(w.Slots);

            int finalPrice = w.CreditsPrice + ts.t_price;
            int finalErgo = w.Ergonomics + ts.t_ergo;
            float finalRecoil = w.RecoilForceUp + (w.RecoilForceUp * (ts.t_recoil / 100));

            return (finalPrice, finalErgo, finalRecoil) ;
        }
        public static (int t_price, int t_ergo, float t_recoil) getModTotals(this WeaponMod wm)
        {
            var ts = accumulateTotals(wm.Slots);

            int finalPrice = wm.CreditsPrice + ts.t_price;
            int finalErgo = (int) wm.Ergonomics + ts.t_ergo;
            float finalRecoil = wm.Recoil + ts.t_recoil; //remember, when we call this for a mod, we don't know the weapon!

            return (finalPrice, finalErgo, finalRecoil);
        }

        public static (int t_price, int t_ergo, float t_recoil) accumulateTotals(this List<Slot> slots)
        {
            List<WeaponMod> weaponMods = accumulateMods(slots);
            return getTotals(weaponMods);
        }

        public static (int t_price, int t_ergo, float t_recoil) getTotals(List<WeaponMod> weaponMods)
        {
            int t_price = 0;
            int t_ergo = 0;
            float t_recoil = 0;

            foreach (WeaponMod wm in weaponMods)
            {
                t_price += wm.CreditsPrice;
                t_ergo += (int) wm.Ergonomics; // In a Weapon Ergo is int, so... ¯\_(ツ)_/¯
                t_recoil += wm.Recoil;
            }

            return (t_price, t_ergo, t_recoil);
        }

    }

    public static class MyExtensions
    {
        public static T DeepClone<T>(this T obj)
        {
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, obj);
                ms.Position = 0;

                return (T)formatter.Deserialize(ms);
            }
        }

        // Need to look over these and remove the ones that are outdated now.

        public static float calcErgo(this Weapon obj)
        {
            float total = 0;
            total += obj.Ergonomics;

            foreach (Slot slot in obj.Slots)
            {
                WeaponMod mod = (WeaponMod) slot.ContainedItem;
                total += mod.Ergonomics;
            }

            return total;
        }
        
        public static int recursivePriceWeapon(this Weapon obj)
        {
            int total = 0;
            total += obj.CreditsPrice;

            IEnumerable<Slot> notNulls = obj.Slots.Where(x => x.ContainedItem != null);

            foreach (Slot slot in notNulls)
            {
                total += recursivePriceBySlots(slot);
            }

            return total;
        }

        public static float recursivePriceByMod(this WeaponMod obj)
        {
            int total = 0;
            total += obj.CreditsPrice;

            if (obj.Slots.Count > 0)
            {
                IEnumerable<Slot> notNulls = obj.Slots.Where(x => x.ContainedItem != null);

                if (notNulls.Count() > 0)
                {
                    foreach (Slot slot in notNulls)
                    {
                        total += recursivePriceBySlots(slot);
                    }
                }
            }
            return total;
        }

        private static int recursivePriceBySlots(this Slot obj)
        {
            int total = 0;
            WeaponMod mod = (WeaponMod)obj.ContainedItem;

            total += mod.CreditsPrice;

            if (mod.Slots.Count > 0)
            {
                IEnumerable<Slot> notNulls = mod.Slots.Where(x => x.ContainedItem != null);

                if (notNulls.Count() > 0)
                {
                    foreach (var slot in notNulls)
                    {
                        total += recursivePriceBySlots(slot);
                    }
                }
            }

            return total;
        }

        public static float recursiveErgoWeapon(this Weapon obj)
        {
            float total = 0;
            total += obj.Ergonomics;

            IEnumerable<Slot> notNulls = obj.Slots.Where(x => x.ContainedItem != null);

            foreach (Slot slot in notNulls)
            {
                total += recursiveErgoBySlots (slot);
            }

            return total;
        }

        public static float recursiveErgoByMod(this WeaponMod obj)
        {
            float total = 0;
            total += obj.Ergonomics;

            if (obj.Slots.Count > 0)
            {
                IEnumerable<Slot> notNulls = obj.Slots.Where(x => x.ContainedItem != null);

                if (notNulls.Count() > 0)
                {
                    foreach (Slot slot in notNulls)
                    {
                        total += recursiveErgoBySlots(slot);
                    }
                }
            }
            return total;
        }

        private static float recursiveErgoBySlots(this Slot obj)
        {
            float total = 0;
            WeaponMod mod = (WeaponMod) obj.ContainedItem;
            total += mod.Ergonomics;

            if (mod.Slots.Count > 0)
            {
                IEnumerable<Slot> notNulls = mod.Slots.Where(x => x.ContainedItem != null);

                if (notNulls.Count() > 0)
                {
                    foreach (var slot in notNulls)
                    {
                        total += recursiveErgoBySlots(slot);
                    }
                }
            }
            return total;
        }

        public static float recursiveRecoilWeapon(this Weapon obj)
        {
            float total = 0;
            float totalModifier = 0;
            total += obj.RecoilForceUp;

            IEnumerable<Slot> notNulls = obj.Slots.Where(x => x.ContainedItem != null);

            foreach (Slot slot in notNulls)
            {
                totalModifier += recursiveRecoilBySlots(slot);
            }
            total = total + (total*(totalModifier/100));

            return total;
        }

        public static float recursiveRecoilByMod(this WeaponMod obj)
        {
            float total = 0;
            total += obj.Recoil;

            if (obj.Slots.Count > 0)
            {
                IEnumerable<Slot> notNulls = obj.Slots.Where(x => x.ContainedItem != null);

                if (notNulls.Count() > 0)
                {
                    foreach (Slot slot in notNulls)
                    {
                        total += recursiveRecoilBySlots(slot);
                    }
                }
            }
            return total;
        }

        private static float recursiveRecoilBySlots(this Slot obj)
        {
            float total = 0;
            WeaponMod mod = (WeaponMod)obj.ContainedItem;
            total += mod.Recoil;

            if (mod.Slots.Count > 0)
            {
                IEnumerable<Slot> notNulls = mod.Slots.Where(x => x.ContainedItem != null);

                if (notNulls.Count() > 0)
                {
                    foreach (var slot in notNulls)
                    {
                        total += recursiveRecoilBySlots(slot);
                    }
                }
            }

            return total;
        }

        public static void recursivePrint((Weapon, Ext_Ammo) obj)
        {
            Console.WriteLine(obj.Item1.Name);
            Console.WriteLine("Ergo: "+obj.Item1.Ergonomics);
            Console.WriteLine("Recoil: "+ obj.Item1.RecoilForceUp);
            Console.WriteLine("Price: "+ obj.Item1.CreditsPrice);

            IEnumerable<Slot> notNulls = obj.Item1.Slots.Where(x => x.ContainedItem != null);

            foreach (Slot slot in notNulls)
            {
                recursivePrintBySlots(slot);
            }

            var cartridge = obj.Item2;

            Console.Write("Bullet: " + cartridge.Name);
            Console.WriteLine(" Pen: " + cartridge.PenetrationPower + " Damage: " + cartridge.Damage);

            Console.WriteLine("New Ergo: " + MyExtensions.recursiveErgoWeapon(obj.Item1));
            Console.WriteLine("New Recoil: " + MyExtensions.recursiveRecoilWeapon(obj.Item1));
            Console.WriteLine("Total Cost: " + MyExtensions.recursivePriceWeapon(obj.Item1).ToString("C", new CultureInfo("ru-RU")));
            Console.WriteLine("");

        }

        private static void recursivePrintBySlots(this Slot obj)
        {
            Console.WriteLine("  " + obj.ContainedItem.Name);
            WeaponMod mod = (WeaponMod)obj.ContainedItem;
            Console.Write("    Ergo: " + mod.Ergonomics);
            Console.Write("  Recoil: " + mod.Recoil);
            Console.WriteLine("  Price: " + mod.CreditsPrice);

            IEnumerable<Slot> notNulls = mod.Slots.Where(x => x.ContainedItem != null);

            foreach (var slot in notNulls)
            {
                recursivePrintBySlots(slot);
            }

            
        }
    }
}
