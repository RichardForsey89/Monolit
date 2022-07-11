﻿using System;
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

namespace TarkovToy.ExtensionMethods
{
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
    }


    public static class Recursion
    {
        // Depreciated
        public static Weapon addBaseAttachments(this Weapon weapon, List<string> baseAttachments, IEnumerable<WeaponMod> mods)
        {
            List<WeaponMod> attachmentsList = mods.Where(x => baseAttachments.Contains(x.Id)).ToList();

            foreach (var slot in weapon.Slots)
            {
                var loopSlot = slot;
                loopSlot.ParentItem = weapon;
                loopSlot = addBaseModRecursive(loopSlot, attachmentsList);
            }
            var test = accumulateMods(weapon.Slots);

            return weapon;
        }
        // Depreciated
        private static Slot addBaseModRecursive(this Slot slot, List<WeaponMod> attachmentsList)
        {
            var filter = slot.Filters[0].Whitelist;
            var candidate = attachmentsList.Find(x => filter.Contains(x.Id));
            if(candidate != null)
            {
                if(candidate.Slots.Count > 0)
                    foreach(Slot slot2 in candidate.Slots)
                    {
                        var loop2slot = slot2;
                        loop2slot.ParentItem = candidate;
                        loop2slot = addBaseModRecursive(slot2, attachmentsList);
                    }
                slot.ContainedItem = candidate;
            }
            return slot;
        }

        public static Weapon addBaseAttachmentsFlat(this Weapon weapon, List<string> baseAttachments, IEnumerable<WeaponMod> mods)
        {
            // Update this into a wrapper method, don't need the flat loop method below


            List<WeaponMod> attachmentsList = mods.Where(x => baseAttachments.Contains(x.Id)).ToList();
            weapon = (Weapon) recursiveFit(weapon, attachmentsList, "base");

            var test = accumulateMods(weapon.Slots);


            //weapon.Slots = addBaseAttachmentsFlatLoop(weapon.Slots, attachmentsList, weapon);
            return weapon;
        }

        private static List<Slot> addBaseAttachmentsFlatLoop(this List<Slot> slots, List<WeaponMod> attachmentsList, Item parent)
        {
            // I could cast parent to CompoundItem and make this be Weapon/WeaponMod agnostic and collapse it in to the first method
            // This method is also depreciated
            foreach (var slot in slots)
            {
                var filter = slot.Filters[0].Whitelist;
                var candidate = attachmentsList.Find(x => filter.Contains(x.Id));
                if (candidate != null)
                {
                    if (candidate.Slots.Count > 0)
                        candidate.Slots = addBaseAttachmentsFlatLoop(candidate.Slots, attachmentsList, candidate);

                    slot.ContainedItem = candidate;
                    slot.ParentItem = parent;
                }
            }
            return slots;
        }


        // TODO: Extend this method to take into account the difference between base mods and potential mods, add combinational sorting, add checks for incompatible items.
        // Look at the old method where a "batch" of options are made and then chosen from, eg, stocks with rubber pads vs expensive stocks. Implement an efficency grading comparision.
        public static CompoundItem recursiveFit(CompoundItem CI, IEnumerable<WeaponMod> mods, string mode)
        {
            foreach (Slot slot in CI.Slots)
            {
                List<WeaponMod> shortList = mods.Where(item => slot.Filters[0].Whitelist.Contains(item.Id)).ToList();
                List<WeaponMod> candidatesList = new();

                candidatesList.AddRange(shortList.Select(item => (WeaponMod)recursiveFit(item, mods, mode)));

                if (shortList.Count > 0)
                {
                    // repalce this with a switch when you add more options.
                    if (mode.Equals("ergo"))
                        candidatesList = candidatesList.OrderByDescending(x => getModTotals(x).t_ergo).ToList();
                    else if (mode.Equals("recoil"))
                        candidatesList = candidatesList.OrderBy(x => getModTotals(x).t_recoil).ToList();

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

        public static Weapon recursiveRemoveEmptyMount(this Weapon obj)
        {

            IEnumerable<Slot> targetSlots = obj.Slots.Where(x => x.ContainedItem != null);

            foreach (Slot slot in targetSlots)
            {
                var ContainedItem = (WeaponMod) slot.ContainedItem;
                if (ContainedItem.Slots.Any())
                {
                    IEnumerable<Slot> CI_targetSlots = ContainedItem.Slots.Where(x => x.ContainedItem != null && (x.Name.Contains("mod_mount") || x.Name.Contains("mod_scope")));
                    foreach (Slot CI in CI_targetSlots)
                    {
                        var sub_ContainedItem = (WeaponMod)CI.ContainedItem;
                        IEnumerable<Slot> list2 = sub_ContainedItem.Slots.Where(x => x.ContainedItem == null);
                        if (list2.Any())
                        {
                            CI.ContainedItem = null;
                        }
                        
                    }
                    foreach (Slot CI in CI_targetSlots)
                    {
                        var sub_ContainedItem = (WeaponMod)CI.ContainedItem;
                        IEnumerable<Slot> sub_CI_targetSlots = sub_ContainedItem.Slots.Where(x => x.ContainedItem == null && (x.Name.Contains("mod_mount") || x.Name.Contains("mod_scope")));
                        foreach (Slot blah in sub_CI_targetSlots)
                        {
                            blah.ContainedItem = null;
                        }
                    }

                    CI_targetSlots = ContainedItem.Slots.Where(x => x.ContainedItem == null && (x.Name.Contains("mod_mount") || x.Name.Contains("mod_scope")));
                    foreach (Slot i in CI_targetSlots)
                    {
                        i.ContainedItem = null;
                    }
                }
                if (slot.Id == "57486f552459770b2a5e1c05")
                {
                    slot.ContainedItem = null;
                }
            }

            return obj;
        }

        private static WeaponMod recursiveRemoveEmptyMountMod(this Slot obj)
        {
            WeaponMod mod = (WeaponMod) obj.ContainedItem;
            IEnumerable<Slot> targetSlots = mod.Slots.Where(x => x.ContainedItem != null);

            return mod;
        }

        public static Weapon recursiveFitErgoWeapon2(this Weapon obj, IEnumerable<WeaponMod> mods)
        {

            String[] categories = { "mod_charge", "mod_stock", "mod_pistol_grip",
                                             "mod_barrel", "mod_handguard", "mod_muzzle",
                                             "mod_gas_block", "mod_reciever",
                                             "mod_foregrip"};

            IEnumerable<WeaponMod> filtered_mods = mods.Where(x => x.CreditsPrice < 20000);

            // We will probably set filters to this later
            IEnumerable<Slot> targetSlots = obj.Slots.Where(x => categories.Contains(x.Name));

            List<WeaponMod> modsList = filtered_mods.ToList();

            foreach (Slot tSlot in targetSlots)
            {
                List<WeaponMod> candidateBundles = new List<WeaponMod>();

                List<string> tSlot_whitelist = tSlot.Filters[0].Whitelist;

                foreach (string id in tSlot_whitelist)
                {
                    WeaponMod weaponMod = modsList.Find(x => x.Id.Contains(id));
                    if (weaponMod != null)
                    {
                        foreach (Slot slot_weaponMod in weaponMod.Slots)
                        {
                            slot_weaponMod.ContainedItem = recursiveFitErgoMods2(slot_weaponMod, filtered_mods);
                        }

                        candidateBundles.Add(weaponMod);
                    }
                }

                // Sort and then select from the candidate bundles for the best one to put in the tSlot
                candidateBundles = candidateBundles.OrderByDescending(x => recursiveErgoByMod(x)).ToList();
                if (candidateBundles.Count > 0)
                    tSlot.ContainedItem = candidateBundles.First();
                else
                    tSlot.ContainedItem = null;
            }

            return obj;
        }

        private static WeaponMod recursiveFitErgoMods2(this Slot obj, IEnumerable<WeaponMod> mods)
        {


            String[] categories = { "mod_charge", "mod_stock", "mod_pistol_grip",
                                             "mod_barrel", "mod_handguard", "mod_muzzle",
                                             "mod_gas_block",
                                             "mod_foregrip"};

            WeaponMod candidate = null;

            List<WeaponMod> shortList = mods.Where(item => obj.Filters[0].Whitelist.Contains(item.Id)).ToList();
            shortList.RemoveAll(x=> x.ToString() == "RatStash.Collimator" ||
                                    x.ToString() == "RatStash.CompactCollimator");
            shortList = shortList.OrderByDescending(x => x.Ergonomics).ToList();

            if (shortList.Any())
            {

                candidate = shortList.First();

                IEnumerable<Slot> filtered = candidate.Slots.Where(x => categories.Contains(x.Name));

                foreach (Slot filteredSlot in filtered)
                {
                    filteredSlot.ContainedItem = recursiveFitErgoMods2(filteredSlot, mods);
                }
            }

            return candidate;
        }

        public static Weapon recursiveFitRecoilWeapon2(this Weapon obj, IEnumerable<WeaponMod> mods)
        {

            String[] categories = { "mod_charge", "mod_stock", "mod_pistol_grip",
                                             "mod_barrel", "mod_handguard", "mod_muzzle",
                                             "mod_gas_block", "mod_reciever", "mod_mount_000", "mod_mount_001",
                                             "mod_foregrip"};

            IEnumerable<WeaponMod> filtered_mods = mods.Where(x => x.CreditsPrice < 20000);

            // We will probably set filters to this later
            IEnumerable<Slot> targetSlots = obj.Slots.Where(x => categories.Contains(x.Name));

            List<WeaponMod> modsList = filtered_mods.ToList();

            foreach (Slot tSlot in targetSlots)
            {
                List<WeaponMod> candidateBundles = new List<WeaponMod>();

                List<string> tSlot_whitelist = tSlot.Filters[0].Whitelist;

                foreach (string id in tSlot_whitelist)
                {
                    WeaponMod weaponMod = modsList.Find(x => x.Id.Contains(id));
                    if (weaponMod != null)
                    {
                        foreach (Slot slot_weaponMod in weaponMod.Slots)
                        {
                            slot_weaponMod.ContainedItem = recursiveFitRecoilMods2(slot_weaponMod, filtered_mods);
                        }

                        candidateBundles.Add(weaponMod);
                    }
                }

                // Sort and then select from the candidate bundles for the best one to put in the tSlot
                candidateBundles = candidateBundles.OrderBy(x => recursiveRecoilByMod(x)).ToList();

                if (candidateBundles.Count > 0)
                    tSlot.ContainedItem = candidateBundles.First();
                else
                    tSlot.ContainedItem = null;
            }

            return obj;
        }

        private static WeaponMod recursiveFitRecoilMods2(this Slot obj, IEnumerable<WeaponMod> mods)
        {


            String[] categories = { "mod_charge", "mod_stock", "mod_pistol_grip",
                                             "mod_barrel", "mod_handguard", "mod_muzzle",
                                             "mod_gas_block", "mod_mount_000", "mod_mount_001",
                                             "mod_foregrip"};

            WeaponMod candidate = null;

            List<WeaponMod> shortList = mods.Where(item => obj.Filters[0].Whitelist.Contains(item.Id)).ToList();
            shortList.RemoveAll(x => x.ToString() == "RatStash.Collimator" ||
                                    x.ToString() == "RatStash.CompactCollimator");
            shortList = shortList.OrderBy(x => x.Recoil).ToList();

            if (shortList.Any())
            {

                candidate = shortList.First();

                IEnumerable<Slot> filtered = candidate.Slots.Where(x => categories.Contains(x.Name));

                foreach (Slot filteredSlot in filtered)
                {
                    filteredSlot.ContainedItem = recursiveFitRecoilMods2(filteredSlot, mods);
                }
            }

            return candidate;
        }

        public static Weapon recursiveFitErgoWeapon(this Weapon obj, IEnumerable<WeaponMod> mods)
        {
            
            //mods = mods.Where(mod => mod.CreditsPrice < 10000);

            //IEnumerable<Slot> required = obj.Slots.Where(x => x.Required == true);

            IEnumerable<Slot> required = obj.Slots;

            List<WeaponMod> shortList = mods.ToList();


            List<string> conflictingItems = new List<string>();

            foreach (Slot slot in required)
            {
                IEnumerable<WeaponMod> slotMods = mods;

                List<WeaponMod> candidateBundles = new List<WeaponMod>();
                List<string> whitelist = slot.Filters[0].Whitelist;
                List<WeaponMod> cleanedList = shortList.Where(x => whitelist.Contains(x.Id)).ToList();


                if (cleanedList.Count > 0)
                {
                    foreach (string id_wl in whitelist)
                    {
                        WeaponMod wm = cleanedList.Find(x => x.Id.Contains(id_wl));
                        if (wm != null)
                        {
                            foreach (Slot s_wm in wm.Slots)
                            {
                                s_wm.ContainedItem = recursiveFitErgoMods(slot, mods, ref conflictingItems);
                            }
                            candidateBundles.Add(wm);
                        }
                        
                    }

                    slot.ContainedItem = candidateBundles.First();
                }
                
            }

            return obj;
        }

        private static WeaponMod recursiveFitErgoMods(this Slot obj, IEnumerable<WeaponMod> mods, ref List<string> conflictingItems)
        {

            List<string> localCI = conflictingItems;
            WeaponMod candidate = null;

            // First get a whitelist of mods for the slot and order it by DESC
            IEnumerable<WeaponMod> whiteList = mods.Where(item => obj.Filters[0].Whitelist.Contains(item.Id));

            if (whiteList.Count() > 0)
            {
                whiteList = whiteList.Where(x => !localCI.Contains(x.Id));

                //Pick the best item 
                candidate = whiteList.First();


                if (candidate != null)
                {
                    conflictingItems.AddRange(candidate.ConflictingItems);

                    String[] categories = { "mod_charge", "mod_stock", "mod_pistol_grip",
                                             "mod_barrel", "mod_handguard", "mod_muzzle",
                                             "mod_gas_block", "mod_mount_000", "mod_mount_001",
                                             "mod_foregrip"};

                    // Get the slots of that item which are required
                    //IEnumerable<Slot> required = candidate.Slots.Where(x => x.Required == true);

                    IEnumerable<Slot> required = candidate.Slots.Where(x => categories.Contains(x.Name));

                    //IEnumerable<Slot> required = candidate.Slots;

                    foreach (Slot requiredSlot in required)
                    {
                        requiredSlot.ContainedItem = recursiveFitErgoMods(requiredSlot, mods, ref conflictingItems);
                    }
                }
            }
            

            return candidate;
        }

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
