using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TarkovToy.Model
{
    internal class TTWeapon
    {
        public string ID { get; set; }
        public string? ShortName { get; set; }
        public int Price { get; set; }
        public int RecoilForceUp { get; set; }
        public int RecoilForceBack { get; set; }
        public int Ergonomics { get; set; }
        public int BFireRate { get; set; }
        public List<TT_WSlot> Slots { get; set; } = new List<TT_WSlot>();



//        public string GetStats()
//        {
//            string stats = string.Empty;

//            int _calcErgo = Ergonomics;
//            int _calcRecoilVert = RecoilForceUp;

//            foreach (var slot in Slots)
//            {
//                //foreach (var w in slot.ModsWhitelist)
//                //{
//                //    Console.WriteLine(w);
//                //}
//                Console.WriteLine(slot.Name);
//                if (slot.InstalledBundle != null)
//                {
//                    Console.WriteLine("SlotMod: " + slot.InstalledBundle.ShortName);
//                    Console.WriteLine("SlotErgo: " + slot.InstalledBundle.Ergonomics);
//                    Console.WriteLine("SlotRecoil: " + slot.InstalledBundle.RecoilModifier);

//                    _calcErgo += slot.InstalledMod.Ergonomics;
//                    _calcRecoilVert -= (RecoilForceUp - (int) (RecoilForceUp*(100+slot.InstalledMod.RecoilModifier)/100));

//                    if (slot.InstalledMod.Slots.Count()>0)
//                    {
//                        foreach (TTSlot subSlot in slot.InstalledMod.Slots)
//                        {
//                            Console.WriteLine(subSlot.Name);
//                            Console.WriteLine(subSlot.InstalledMod.ShortName);
//                            if (subSlot.InstalledMod != null)
//                            {
//                                Console.WriteLine("SlotMod: " + subSlot.InstalledMod.ShortName);
//                                Console.WriteLine("SlotErgo: " + subSlot.InstalledMod.Ergonomics);
//                                Console.WriteLine("SlotRecoil: " + subSlot.InstalledMod.RecoilModifier);
//                                _calcErgo += subSlot.InstalledMod.Ergonomics;
//                                _calcRecoilVert -= (RecoilForceUp - (int)(RecoilForceUp * (100 + subSlot.InstalledMod.RecoilModifier) / 100));
//                            }
//                        }
//                    }
//                }
//                Console.WriteLine();
//            }
//            stats = $"This {ShortName} has an Ergo of {_calcErgo} and a recoil of {_calcRecoilVert}";

//            return stats;
//        }

//        public List<string> GetAllCurrentModsByID()
//        {
//            List<string> _currentModsIDs = new List<string>();

//            foreach (TTSlot slot in Slots)
//            {
//                if (slot.InstalledMod != null)
//                {
//                    _currentModsIDs.Add(slot.InstalledMod.ID);
//                }
//            }
//            return _currentModsIDs;
//        }

//        public List<string> GetAllExclusionsByID()
//        {
//            List<string> _exclusions = new List<string>();

//            foreach (TTSlot slot in Slots)
//            {
//                if (slot.InstalledMod != null)
//                {
//                    _exclusions.AddRange(slot.InstalledMod.ConflictingItems);
//                }
//            }
//            return _exclusions;
//        }

//        public void AddBestErgoMods(List<TTMod> TTMods)
//        {
//            foreach (TTSlot slot in Slots)
//            {
//                var _compatibleMods = TTMods.Where(mod => slot.ModsWhitelist.Contains(mod.ID));
//;               _compatibleMods = _compatibleMods.OrderByDescending(mod => mod.Ergonomics).ToList();
//                slot.InstallMod(_compatibleMods.FirstOrDefault());

//                foreach (TTSlot s in slot.InstalledMod.Slots)
//                {
//                    var cmpMods = TTMods.Where(m => s.ModsWhitelist.Contains(m.ID));
//                    cmpMods = cmpMods.OrderByDescending(m => m.Ergonomics).ToList();
//                    s.InstallMod(cmpMods.FirstOrDefault());
//                }
//            };
//        }

//        public void AddBestRecoilMods(List<TTMod> TTMods)
//        {
//            foreach (TTSlot slot in Slots)
//            {
//                var _compatibleMods = TTMods.Where(mod => slot.ModsWhitelist.Contains(mod.ID));
//                _compatibleMods = _compatibleMods.OrderBy(mod => mod.RecoilModifier).ToList();
//                slot.InstallMod(_compatibleMods.FirstOrDefault());

//                foreach (TTSlot s in slot.InstalledMod.Slots)
//                {
//                    var cmpMods = TTMods.Where(m => s.ModsWhitelist.Contains(m.ID));
//                    cmpMods = cmpMods.OrderByDescending(m => m.RecoilModifier).ToList();
//                    s.InstallMod(cmpMods.FirstOrDefault());
//                }
//            };
//        }

//        public void AddBestRecoilModsExclusions(List<TTMod> TTMods)
//        {
//            foreach (TTSlot slot in Slots)
//            {
//                var _compatibleMods = TTMods.Where(mod => slot.ModsWhitelist.Contains(mod.ID));
//                _compatibleMods = _compatibleMods.OrderBy(mod => mod.RecoilModifier).ToList();
                

//                var step2a = _compatibleMods.ToList();
//                step2a.RemoveAll(x => GetAllExclusionsByID().Contains(x.ID));

//                slot.InstallMod(step2a.FirstOrDefault());

//                foreach (TTSlot s in slot.InstalledMod.Slots)
//                {
//                    var cmpMods = TTMods.Where(m => s.ModsWhitelist.Contains(m.ID));
//                    cmpMods = cmpMods.OrderByDescending(m => m.RecoilModifier).ToList();

//                    var step2 = cmpMods.ToList();
//                    step2.RemoveAll(x => GetAllExclusionsByID().Contains(x.ID));

//                    foreach(var s2 in step2)
//                        Console.WriteLine(s2.ShortName);


//                    s.InstallMod(step2.FirstOrDefault());
//                }
//            };
//        }

//        public void AddBestOverallMods(List<TTMod> TTMods)
//        {
//            foreach (TTSlot slot in Slots)
//            {
//                var _compatibleMods = TTMods.Where(mod => slot.ModsWhitelist.Contains(mod.ID));
//                _compatibleMods = _compatibleMods.OrderBy(mod => mod.RecoilModifier).ToList();
//                slot.InstallMod(_compatibleMods.FirstOrDefault());

//                foreach (TTSlot s in slot.InstalledMod.Slots)
//                {
//                    var cmpMods = TTMods.Where(m => s.ModsWhitelist.Contains(m.ID));
//                    cmpMods = cmpMods.OrderByDescending(m => 0 - (int)(RecoilForceUp * (100 + m.RecoilModifier) / 100) - m.Ergonomics).ToList();
//                    s.InstallMod(cmpMods.FirstOrDefault());
//                }
//            };
//        }

//        public void AddMostEfficientRecoilMods(List<TTMod> TTMods)
//        {
//            foreach (TTSlot slot in Slots)
//            {
//                var _compatibleMods = TTMods.Where(mod => slot.ModsWhitelist.Contains(mod.ID));
//                _compatibleMods = _compatibleMods.OrderBy(mod => mod.RecoilModifier).ToList();
//                slot.InstallMod(_compatibleMods.FirstOrDefault());

//                foreach (TTSlot s in slot.InstalledMod.Slots)
//                {
//                    var cmpMods = TTMods.Where(m => s.ModsWhitelist.Contains(m.ID));
//                    cmpMods = cmpMods.OrderBy(m => m.Price/ (int)(RecoilForceUp * (100 + m.RecoilModifier) / 100)).ToList();

//                    foreach (TTMod c in cmpMods)
//                    {
//                        Console.WriteLine(c.ShortName);
//                        Console.WriteLine("effi: " + (c.Price/ (int)(RecoilForceUp * (100 + c.RecoilModifier) / 100)));
//                    }

//                    s.InstallMod(cmpMods.FirstOrDefault());
//                }
//            };
//        }

//        public void RemoveAllMods()
//        {
//            foreach(TTSlot slot in Slots)
//            {
//                slot.RemoveMod();
//            }
//        }
    }
}
