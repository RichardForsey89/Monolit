using RatStash;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using TarkovToy.ExtensionMethods;

namespace TarkovToy.Model
{
    [Serializable]
    internal class TTBundle
    {
        // Will take the ID of the first module
        public string ID { get; set; } = "default";
        public string CompoundName { get; set; } = "";
        public int TotalPrice { get; set; } = 0;
        public int TotalErgo { get; set; } = 0;
        public double TotalRecoil { get; set; } = 0;
        public TTMod TTMod { get; set; } = new TTMod();

        public void updateDeets(TTMod addition)
        {
            CompoundName = CompoundName + "+"+addition.ShortName;
            TotalPrice += addition.Price;
            TotalErgo += addition.Ergonomics;
            TotalRecoil += addition.RecoilModifier;
        }
        public void AddModule(TTMod module)
        {
            module.ParentBundle = this;
            ID = module.ID;
            CompoundName = module.ShortName;
            TotalPrice = module.Price;
            TotalErgo = module.Ergonomics;
            TotalRecoil = module.RecoilModifier;
            TTMod = module;
            foreach (var slot in TTMod.Slots)
            {
                slot.ParentBundle = this;
            }
        }

        // This method will create all of the start bundles without any duplicates
        public static List<TTBundle> BuildStartBundles(HashSet<string> seeds, IEnumerable<WeaponMod> RatMods)
        {
            List<TTBundle> StartBundles = new List<TTBundle>();

            // First we will convert RatMods to TTMods
            List<TTMod> mods = TTMod.BuildModules(RatMods);

            foreach (var seed in seeds)
            {
                // Find the mod that matches the seed
                var seedMod = mods.Find(mod => mod.ID == seed);

                if (seedMod != null)
                {
                    // Create a bundle
                    TTBundle _StartBundle = new TTBundle();

                    //Add the SeedMod to the bundle
                    _StartBundle.AddModule(seedMod);

                    //Add the new bundle to StartBundles
                    StartBundles.Add(_StartBundle);
                }
            }

            return StartBundles;
        }

        public static List<TTBundle> AndoMethod(List<TTBundle> StartBundles, IEnumerable<WeaponMod> RatMods)
        {
            List<TTBundle> output = new List<TTBundle>();
            // First we will convert RatMods to TTMods
            List<TTMod> mods = TTMod.BuildModules(RatMods);

            foreach (TTBundle bundle in StartBundles)
            {
                output.AddRange(AndoHelper(bundle, mods));
            }
            return output;
        }

        public static List<TTBundle> AndoHelper(TTBundle StartBundle, List<TTMod> mods)
        {
            List<TTBundle> output = new List<TTBundle>();

            foreach(var _slot in StartBundle.TTMod.Slots)
            {
                var whitelist = _slot.ModsWhitelist;
                var shortlist = mods.FindAll(a => whitelist.Contains(a.ID));
                // A recursive method will go here!
            }


            return output;
        }

        public static void Permutator3(List<TTBundle> StartBundles, IEnumerable<WeaponMod> RatMods)
        {

            List<TTBundle> output = new List<TTBundle>();
            // First we will convert RatMods to TTMods
            List<TTMod> mods = TTMod.BuildModules(RatMods);

            

            // For every starter bundle, we need to exhaust the possibilities
            foreach (var bundle in StartBundles)
            {
                List<List<TTMod>> shortlists = new List<List<TTMod>>();

                foreach (var _slot in bundle.TTMod.Slots)
                {
                    var whitelist = _slot.ModsWhitelist;
                    var shortlist = mods.FindAll(a => whitelist.Contains(a.ID));
                    shortlists.Add(shortlist);
                }

                for (int i = 0; i < bundle.TTMod.Slots.Count; i++)
                {
                    bundle.TTMod.Slots.ElementAt(i);
                }
            }

            //return output;
        }

        public static List<TTBundle> Permutator2(List<TTBundle> StartBundles, IEnumerable<WeaponMod> RatMods)
        {
            List<TTBundle> Permutations = new List<TTBundle>();
            // First we will convert RatMods to TTMods
            List<TTMod> mods = TTMod.BuildModules(RatMods);

            List<List<TTMod>> manyLists = new List<List<TTMod>>();
            // Get a bundle from the StartBundles
            foreach (TTBundle bundle in StartBundles)
            {
                foreach (var slot in bundle.TTMod.Slots)
                {
                    var whitelist = slot.ModsWhitelist;
                    var shortlist = mods.FindAll(a => whitelist.Contains(a.ID));
                    if (shortlist != null)
                    {
                        manyLists.Add(shortlist);
                    }
                }
            }
            Console.WriteLine();
                

            return Permutations;
        }

        public static List<TTBundle> BundleMixer(List<TTBundle> input, List<TTMod> mods)
        {
            var output = new List<TTBundle>();

            // For each bundle
            foreach (var bundle in input)
            {

                // For each slot of the basemod
                //foreach(var mod in bundle.TTMod.Slots)
                for (var SlotNum = 0; SlotNum < bundle.TTMod.Slots.Count; SlotNum++)
                {
                    var modSlot = bundle.TTMod.Slots.ElementAt(SlotNum);
                    output.AddRange(RecursiveAdd(bundle, modSlot, SlotNum, mods));
                }
            }

            return output;
        }
        public static List<TTBundle> RecursiveAdd(TTBundle bundle, TT_M_Slot modSlot, int SlotNum, List<TTMod> mods)
        {
            var output = new List<TTBundle>();
            // local function captures bundle and deduplicates logic for each branch
            void AddNewBundle(TTMod p)
            {
                var newTTBundle = new TTBundle
                {
                    ID = bundle.ID,
                    CompoundName = bundle.CompoundName,
                    TotalPrice = bundle.TotalPrice,
                    TotalErgo = bundle.TotalErgo,
                    TotalRecoil = bundle.TotalRecoil,
                    TTMod = MyExtensions.DeepClone(p)
                };
                newTTBundle.TTMod.Slots.ElementAt(SlotNum).InstallMod(p,newTTBundle);
                output.Add(newTTBundle);
            }

            if (modSlot.InstalledMod == null)
            {
                // Get a whitelist of possibilities for the slot
                var whitelist = modSlot.ModsWhitelist;
                var shortlist = mods.FindAll(a => whitelist.Contains(a.ID));
                // For each possibility
                shortlist.ForEach(AddNewBundle);
            }
            // If the slot is not empty, check that it has slots and if so, fill them
            else
            {
                for (var subSlotNum = 0; subSlotNum < modSlot.InstalledMod.Slots.Count; subSlotNum++)
                {
                    var subModSlot = modSlot.InstalledMod.Slots.ElementAt(subSlotNum);
                    output.AddRange(RecursiveAdd(bundle, subModSlot, subSlotNum, mods));
                }
            }
            return output;
        }

        //        public static List<TTBundle> BuildBasicBundles(IEnumerable<WeaponMod> RatMods)
        //        {
        //            List<TTBundle> bundles = new List<TTBundle>();

        //            // First we will need to create all of the independent modules
        //            List<TTMod> mods = TTMod.BuildModules(RatMods);

        //            // Now let's create the bundles
        //            foreach (WeaponMod _ratMod in RatMods)
        //            {
        //                // Create a bundle
        //                TTBundle _NewTTBundle = new TTBundle()
        //                {
        //                    // Give the bundle the ID of the base module and other details
        //                    ID = _ratMod.Id,
        //                    CompoundName = _ratMod.Name,
        //                    TotalPrice = _ratMod.CreditsPrice,
        //                    TotalErgo = (int)_ratMod.Ergonomics,
        //                    TotalRecoil = _ratMod.Recoil,

        //                    // Attach the base module to the bundle.
        //                    TTMod = new TTMod()
        //                    {
        //                        ID = _ratMod.Id,
        //                        ShortName = _ratMod.ShortName,
        //                        Price = _ratMod.CreditsPrice,
        //                        RecoilModifier = _ratMod.Recoil,
        //                        Ergonomics = (int)_ratMod.Ergonomics,
        //                        ConflictingItems = _ratMod.ConflictingItems
        //                    }
        //                };
        //                // If the _ratMod has slots, add them to the base module
        //                if (_ratMod.Slots.Count > 0)
        //                {
        //                    foreach (var _RatSlot in _ratMod.Slots)
        //                    {
        //                        // Take slot details from the RatSlot
        //                        TT_M_Slot _newModuleSlot = new TT_M_Slot()
        //                        {
        //                            ParentBundle = _NewTTBundle,
        //                            ID = _RatSlot.Id,
        //                            Name = _RatSlot.Name,
        //                            Required = _RatSlot.Required,
        //                        };

        //                        // Populate the whitelist for the slot
        //                        var whitelist = _RatSlot.Filters.First().Whitelist;
        //                        foreach (var filter in whitelist)
        //                        {
        //                            _newModuleSlot.ModsWhitelist.Add(filter);
        //                        }

        //                        // Add the "nothing" possibility unless it's the mag slot
        //                        if (_newModuleSlot.Required == false && _newModuleSlot.Name != "mod_magazine")
        //                        {
        //                            _newModuleSlot.ModsWhitelist.Add("000");
        //                        }
        //                        // Add the slot to the module
        //                        _NewTTBundle.TTMod.Slots.Add(_newModuleSlot);
        //                    }
        //                }

        //                // After all that, add the basic bundle to the bundles
        //                bundles.Add(_NewTTBundle);

        //            };
        //            foreach (TTBundle bundle in bundles)
        //            {
        //                Console.WriteLine(bundle.CompoundName);
        //            }

        //            List<TTBundle> bundles2 = Turksarama(bundles, mods);
        //            Console.WriteLine("Turks num: " + bundles2.Count);

        //            bundles.AddRange(bundles2);

        //            return bundles;
        //        }

        //        public static List<TTBundle> Turksarama(List<TTBundle> input, List<TTMod> mods)
        //        {
        //            var output = new List<TTBundle>();

        //            // For each bundle
        //            foreach (var bundle in input)
        //            {
        //                // For each slot of the basemod
        //                //foreach(var mod in bundle.TTMod.Slots)
        //                for (var SlotNum = 0; SlotNum < bundle.TTMod.Slots.Count; SlotNum++)
        //                {
        //                    var modSlot = bundle.TTMod.Slots.ElementAt(SlotNum);
        //                    output.AddRange(RecursiveAdd(bundle, modSlot, SlotNum, mods));
        //                }
        //            }
        //            return output;
        //        }

        //        public static List<TTBundle> RecursiveAdd(TTBundle bundle, TT_M_Slot modSlot, int SlotNum, List<TTMod> mods)
        //        {
        //            var output = new List<TTBundle>();
        //            // local function captures bundle and deduplicates logic for each branch
        //            void AddNewBundle(TTMod p)
        //            {
        //                var newTTBundle = new TTBundle
        //                {
        //                    ID = bundle.ID,
        //                    CompoundName = bundle.CompoundName,
        //                    TotalPrice = bundle.TotalPrice,
        //                    TotalErgo = bundle.TotalErgo,
        //                    TotalRecoil = bundle.TotalRecoil,
        //                    TTMod = new TTMod()
        //                    {
        //                        ID = bundle.TTMod.ID,
        //                        ShortName = bundle.TTMod.ShortName,
        //                        Price = bundle.TTMod.Price,
        //                        RecoilModifier = bundle.TTMod.RecoilModifier,
        //                        Ergonomics = bundle.TTMod.Ergonomics,
        //                        ConflictingItems = bundle.TTMod.ConflictingItems,
        //                        Slots = bundle.TTMod.Slots
        //                    }
        //                };
        //                newTTBundle.TTMod.Slots.ElementAt(SlotNum).InstallMod(p);
        //                output.Add(newTTBundle);
        //            }

        //            if (modSlot.InstalledMod == null)
        //            {
        //                // Get a whitelist of possibilities for the slot
        //                var whitelist = modSlot.ModsWhitelist;
        //                var shortlist = mods.FindAll(a => whitelist.Contains(a.ID));
        //                // For each possibility
        //                shortlist.ForEach(AddNewBundle);
        //            }
        //            // If the slot is not empty, check that it has slots and if so, fill them
        //            else
        //            {
        //                for (var subSlotNum = 0; subSlotNum < modSlot.InstalledMod.Slots.Count; subSlotNum++)
        //                {
        //                    var subModSlot = modSlot.InstalledMod.Slots.ElementAt(subSlotNum);
        //                    output.AddRange(RecursiveAdd(bundle, subModSlot, subSlotNum, mods));
        //                }
        //            }
        //            return output;
        //        }


        //        public static List<TTBundle> BundleMixer(List<TTBundle> input, List<TTMod> mods)
        //        {
        //            var output = new List<TTBundle>();
        //            // For each bundle
        //            input.ForEach(x =>
        //            {
        //                // For each slot of the basemod
        //                x.TTMod.Slots.ForEach(y =>
        //                {
        //                    // Get the current slot number, need it later.
        //                    int SlotNum = x.TTMod.Slots.IndexOf(y);
        //                    // If the slot is empty
        //                    if(y.InstalledMod == null)
        //                    {
        //                        // Get a whitelist of possibilities for the slot
        //                        var whitelist = y.ModsWhitelist;
        //                        var shortlist = mods.FindAll(a => whitelist.Contains(a.ID));

        //                        // For each possibility
        //                        shortlist.ForEach(p =>
        //                        {
        //                            TTBundle newTTBundle = new TTBundle
        //                            {
        //                                ID = x.ID,
        //                                CompoundName = x.CompoundName,
        //                                TotalPrice = x.TotalPrice,
        //                                TotalErgo = x.TotalErgo,
        //                                TotalRecoil = x.TotalRecoil,
        //                                TTMod = x.TTMod
        //                            };
        //                            newTTBundle.TTMod.Slots.ElementAt(SlotNum).InstallMod(p);
        //                            output.Add(newTTBundle);

        //                            foreach (var sub_slot in p.Slots)
        //                            {

        //                            }
        //                        });
        //                    }
        //                });
        //            });

        //            return output;
        //        }

        //        private static List<TTBundle> bundleHelper(List<TTBundle> input, List<TTMod> mods)
        //        {
        //            List<TTBundle> output = new List<TTBundle>();

        //            foreach (TTBundle bundle in input)
        //            {
        //                if (bundle.TTMod.Slots.Count() > 0)
        //                {
        //                    for (int i = 0; i < bundle.TTMod.Slots.Count(); i++)
        //                    {
        //                        if (bundle.TTMod.Slots.ElementAt(i).InstalledMod == null)
        //                        {
        //                            var whiteList = bundle.TTMod.Slots.ElementAt(i).ModsWhitelist;
        //                            var shortlist = mods.FindAll(x => whiteList.Contains(x.ID));
        //                            foreach (var item in shortlist)
        //                            {
        //                                bundle.TTMod.Slots.ElementAt(i).InstallMod(item);
        //                                if(i > bundle.TTMod.Slots.Count() - 1)
        //                                {
        //                                    var whiteList2 = bundle.TTMod.Slots.ElementAt(i+1).ModsWhitelist;
        //                                    var shortlist2 = mods.FindAll(x => whiteList2.Contains(x.ID));
        //                                    foreach(var item2 in shortlist2)
        //                                    {
        //                                        bundle.TTMod.Slots.ElementAt(i+1).InstallMod(item2);
        //                                    }
        //                                }
        //                                output.Add(bundle);
        //                            }
        //                        }
        //                    }

        //                    //for (int i = 0; i < bundle.TTMod.Slots.Count(); i++)
        //                    //{
        //                    //    if (bundle.TTMod.Slots.ElementAt(i).InstalledMod == null)
        //                    //    {
        //                    //        var whiteList = bundle.TTMod.Slots.ElementAt(i).ModsWhitelist;
        //                    //        var shortlist = mods.FindAll(x => whiteList.Contains(x.ID));
        //                    //        foreach (var item in shortlist)
        //                    //        {
        //                    //            bundle.TTMod.Slots.ElementAt(i).InstallMod(item);
        //                    //            output.Add(bundle);
        //                    //        }
        //                    //    }
        //                    //}
        //                }
        //            }

        //            return output;
        //        }

        //        //public static List<TTBundle> BuildAdvancedBundles(List<TTBundle> bundles)
        //        //{
        //        //    List<TTBundle> _bundles = bundles;

        //        //    foreach (TTBundle bundle in _bundles)
        //        //    {
        //        //        if (bundle.TTMod.Slots.Count() > 0)
        //        //        {
        //        //            for (int i = 0; i < bundle.TTMod.Slots.Count(); i++)
        //        //            {
        //        //                if (bundle.TTMod.Slots.ElementAt(i) != null)
        //        //                {

        //        //                }
        //        //            }
        //        //        }
        //        //    }
        //        //}
    }
}
