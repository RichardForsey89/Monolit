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



namespace TarkovToy.ExtensionMethods
{
    public static class API_ExtensionMethods
    {
        //TODO: setup some of these
    }

    public static class Recursion
    {
        public static Weapon addBaseAttachments(this Weapon weapon, List<string> baseAttachments, IEnumerable<WeaponMod> mods)
        {
            List<WeaponMod> attachmentsList = mods.Where(x => baseAttachments.Contains(x.Id)).ToList();

            foreach (var slot in weapon.Slots)
            {
                var loopSlot = slot;
                loopSlot.ParentItem = weapon;
                loopSlot = addBaseModRecursive(loopSlot, ref attachmentsList);
            }

            return weapon;
        }

        private static Slot addBaseModRecursive(this Slot slot, ref List<WeaponMod> attachmentsList)
        {
            var filter = slot.Filters[0].Whitelist;
            var candidate = attachmentsList.Find(x => filter.Contains(x.Id));
            if(candidate != null)
            {
                //attachmentsList.Remove(candidate); // Possibly don't need this, but nyeeh
                if(candidate.Slots.Count > 0)
                    foreach(Slot slot2 in candidate.Slots)
                    {
                        var loop2slot = slot2;
                        loop2slot.ParentItem = candidate;
                        loop2slot = addBaseModRecursive(slot2, ref attachmentsList);
                    }
                slot.ContainedItem = candidate;
                
            }
            
            return slot;
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


        public static void recursivePrint(this Weapon obj)
        {
            Console.WriteLine(obj.Name);
            Console.WriteLine("Ergo: "+obj.Ergonomics);
            Console.WriteLine("Recoil: "+ obj.RecoilForceUp);
            Console.WriteLine("Price: "+ obj.CreditsPrice);

            IEnumerable<Slot> notNulls = obj.Slots.Where(x => x.ContainedItem != null);

            foreach (Slot slot in notNulls)
            {
                recursivePrintBySlots(slot);
            }

            Console.WriteLine("New Ergo: " + MyExtensions.recursiveErgoWeapon(obj));
            Console.WriteLine("New Recoil: " + MyExtensions.recursiveRecoilWeapon(obj));
            Console.WriteLine("Total Cost: " + MyExtensions.recursivePriceWeapon(obj).ToString("C", new CultureInfo("ru-RU")));
            Console.WriteLine("");

        }

        private static void recursivePrintBySlots(this Slot obj)
        {
            Console.WriteLine("  " + obj.ContainedItem.Name);
            WeaponMod mod = (WeaponMod)obj.ContainedItem;
            Console.Write("  Ergo: " + mod.Ergonomics);
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
