using System.Net.Http.Json;
using System.Runtime.Serialization.Json;
using System.Xml.Linq;
using System.Xml.XPath;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TarkovToy.Model;

using System.Text.Json;
using System.Text.Json.Serialization;
using RatStash;
using System.Collections.Generic;
using System.Collections;
using TarkovToy.ExtensionMethods;
using System.Globalization;
using System.Diagnostics;

var data = new Dictionary<string, string>()
{
    {"query", "{traders(lang:en){ id name levels{ id level requiredReputation requiredPlayerLevel cashOffers{ item{ id name } priceRUB currency price }}}}" }
};

string traders;

using (var httpClient = new HttpClient())
{

    //Http response message
    var httpResponse = await httpClient.PostAsJsonAsync("https://api.tarkov.dev/graphql", data);

    //Response content
    var responseContent = await httpResponse.Content.ReadAsStringAsync();

    //Write response
    traders = JToken.Parse(responseContent).ToString();

    using (StreamWriter writetext = new StreamWriter("C:\\Users\\richa\\source\\repos\\TarkovToy\\Data\\traders.json"))
    {
        writetext.Write(traders);
    }
}

JObject o = JObject.Parse(traders);

//var filtering = o.SelectTokens("$['data']['traders'][0]['levels'][1]['cashOffers'][*]['item']['id']").ToList();

/* I decided to use JSONpaths fed into the SelectTokens() method as it is reasonably readable, and string interpolation will allow for flexibility. Useful links:
 * https://www.newtonsoft.com/json/help/html/QueryJsonSelectTokenJsonPath.htm
 * https://goessner.net/articles/JsonPath/index.html#e2
 * https://stackoverflow.com/questions/38021032/multiple-filters-in-jsonpath
 * https://jsonpath.com/ <-This one is extremely helpful if you need to experiment
 */

string[] traderNames =
    {
      "Prapor", "Therapist", "Fence", "Skier",
      "Peacekeeper","Mechanic", "Ragman", "Jaeger"
    };
int[] traderLevels = { 1 };

List<string> traderMask = new List<string>();

foreach (string traderName in traderNames)
{
    foreach (int traderLevel in traderLevels)
    {
        string searchJSONpath = $"$.data.traders.[?(@.name=='{traderName}')].levels.[?(@.level=={traderLevel})].cashOffers.[*].item.id";
        var filtering = o.SelectTokens(searchJSONpath).ToList();
        filtering.ForEach(x => traderMask.Add(x.ToString()));
    }
}

CultureInfo ci = new CultureInfo("ru-RU");
Console.OutputEncoding = System.Text.Encoding.Unicode;
Console.WriteLine(ci.DisplayName + " - currency symbol: " + ci.NumberFormat.CurrencySymbol);

// Setup of the input from the JSONs
Database database = Database.FromFile("bsg-data.json", false);

// Split the DB between mods and weapons
IEnumerable<Item> AllMods = database.GetItems(m => m is WeaponMod);
IEnumerable<Item> AllWeapons = database.GetItems(m => m is Weapon);


// Setup the filters for things that I don't think are relevant, but we also remove the Mounts so they can be added in clean later
Type[] ModsFilter = 
    { typeof(IronSight), typeof(CompactCollimator), typeof(Collimator),
      typeof(OpticScope), typeof(NightVision), typeof(ThermalVision),
      typeof(AssaultScope), typeof(SpecialScope),
      typeof(CombTactDevice), typeof(Flashlight), typeof(LaserDesignator),
      typeof(Mount)};

// Apply that filter
var FilteredMods = AllMods.Where(mod => !ModsFilter.Contains(mod.GetType())).ToList();

// Get the mounts from AllMods into a list, filter it to be only the mounts we want (for foregrips) and add them back to FilteredMods
IEnumerable<Mount> Mounts = AllMods.OfType<Mount>();
var MountsFiltered = Mounts.Where(mod => mod.Slots.Any(slot=> slot.Name == "mod_foregrip")).Cast<Item>().ToArray();
FilteredMods.AddRange(MountsFiltered);

// Apply the traders mask
FilteredMods = FilteredMods.FindAll(x => traderMask.Contains(x.Id));

Console.WriteLine(FilteredMods.Count);

Environment.Exit(0);


Database GunsDB = Database.FromFile("bsg-data.json", false);

database = database.Filter(x => x is WeaponMod);
IEnumerable<WeaponMod> items = database.GetItems(m => m is WeaponMod).Cast<WeaponMod>();

string[] shitlist = { "RatStash.Collimator", "RatStash.CompactCollimator", "RatStash.CombTactDevice", "RatStash.OpticScope", "RatStash.NightVision", "RatStash.Flashlight", "RatStash.SpecialScope", "RatStash.ThermalVision", "RatStash.IronSight" };
string[] shitlist2 = { "Geissele Super Precision 30mm ring scope mount", "TROY QARS 4.2 inch rail", "Vltor CASV KeyMod 2 inch rail", "Vltor CASV KeyMod 4 inch rail", "Geissele Super Precision 30mm ring scope mount (DDC)",
    "Axion Kobra dovetail mount", "KMZ 1P59 dovetail mount", "NPZ 1P78-1 dovetail mount", "VOMZ Pilad 043-02 dovetail mount", "Zenit B-13 \"Klassika\" dovetail rail platform", "AR-15 ADAR 2-15 wooden stock", "Alexander Arms 3 inch rail"};

IEnumerable<WeaponMod> names = items.DistinctBy(i => i.GetType());

var res = names.Where(x => !shitlist.Contains(x.GetType().ToString()));

foreach (var item in names)
{
    Console.WriteLine(item.GetType().ToString());
}

Console.WriteLine("");

foreach (var item in res)
{
    Console.WriteLine(item.GetType().ToString());
}


var assaultRifles = GunsDB.GetItems(item => item is AssaultRifle).Cast<AssaultRifle>();
var mods = database.GetItems(m => m is WeaponMod).Cast<WeaponMod>();

mods = mods.Where(x => !shitlist.Contains(x.GetType().ToString())).Cast<WeaponMod>();
mods = mods.Where(x => !shitlist2.Contains(x.Name)).Cast<WeaponMod>();
//var mods = res;


String[] skippedMods = { "mod_scope", "mod_sight_rear"  };

List<TTMod> TTmods = TTMod.BuildModules(mods);

Console.WriteLine("Num of guns: " + assaultRifles.Count());
Console.WriteLine("Num of mods: " + mods.Count());


IEnumerable<Item> wMods = items.Where(item => item is WeaponMod);
IEnumerable<Item> foregrips = items.Where(item => item is Foregrip);
IEnumerable<Item> muzzleDevices = items.Where(item => item is MuzzleDevice);
IEnumerable<Item> gasBlocks = items.Where(item => item is GasBlock);
IEnumerable<Item> essentials = items.Where(item => item is EssentialMod);
IEnumerable<Item> stocks = items.Where(item => item is Stock);

List<AssaultRifle> ergos = new List<AssaultRifle>();
List<AssaultRifle> recoils = new List<AssaultRifle>();

foreach (AssaultRifle rifle in assaultRifles)
{
    AssaultRifle assaultRifle_e = (AssaultRifle)MyExtensions.recursiveFitErgoWeapon2(rifle, mods).recursiveRemoveEmptyMount();
    AssaultRifle assaultRifle_r = (AssaultRifle)MyExtensions.recursiveFitRecoilWeapon2(rifle, mods).recursiveRemoveEmptyMount();

    ergos.Add(assaultRifle_e);
    recoils.Add(assaultRifle_r);
}

ergos = ergos.OrderByDescending(x => MyExtensions.recursiveErgoWeapon(x)).ToList();
recoils = recoils.OrderBy(x => MyExtensions.recursiveRecoilWeapon(x)).ToList();

Console.WriteLine("");
Console.WriteLine("");

for (int i = 0; i < 5; i++)
{
    MyExtensions.recursivePrint(ergos[i]);
    Console.WriteLine("New Ergo: " + MyExtensions.recursiveErgoWeapon(ergos[i]));
    Console.WriteLine("New Recoil: " + MyExtensions.recursiveRecoilWeapon(ergos[i]));
    Console.WriteLine("Total Cost: " + MyExtensions.recursivePriceWeapon(ergos[i]).ToString("C", ci));
    Console.WriteLine("");
}

Console.WriteLine("");
Console.WriteLine("");

for (int i = 0; i < 5; i++)
{
    MyExtensions.recursivePrint(recoils[i]);
    Console.WriteLine("New Ergo: " + MyExtensions.recursiveErgoWeapon(recoils[i]));
    Console.WriteLine("New Recoil: " + MyExtensions.recursiveRecoilWeapon(recoils[i]));
    Console.WriteLine("Total Cost: " + MyExtensions.recursivePriceWeapon(recoils[i]).ToString("C", ci));
    Console.WriteLine("");
}


//AssaultRifle AKM_e = (AssaultRifle)GunsDB.GetItem("59d6088586f774275f37482f");  // AKM
//AssaultRifle AKM_r = (AssaultRifle)GunsDB.GetItem("59d6088586f774275f37482f");

//AssaultRifle AK74N_e = (AssaultRifle)GunsDB.GetItem("5644bd2b4bdc2d3b4c8b4572");  // AK-74N
//AssaultRifle AK74N_r = (AssaultRifle)GunsDB.GetItem("5644bd2b4bdc2d3b4c8b4572");

//AssaultRifle M4A1_e = (AssaultRifle)GunsDB.GetItem("5447a9cd4bdc2dbd208b4567");  // M4A1
//AssaultRifle M4A1_r = (AssaultRifle)GunsDB.GetItem("5447a9cd4bdc2dbd208b4567");

//Console.WriteLine("M4A1_e");
//Console.WriteLine("Base Ergo: " + M4A1_e.Ergonomics);
//M4A1_e = (AssaultRifle) MyExtensions.recursiveFitErgoWeapon2(M4A1_e, mods);
//M4A1_e = (AssaultRifle) MyExtensions.recursiveRemoveEmptyMount(M4A1_e);
//MyExtensions.recursivePrint(M4A1_e);
//Console.WriteLine("New Ergo: " + MyExtensions.recursiveErgoWeapon(M4A1_e));
//Console.WriteLine("New Recoil: " + MyExtensions.recursiveRecoilWeapon(M4A1_e));
//Console.WriteLine("Total Cost: " + MyExtensions.recursivePriceWeapon(M4A1_e).ToString("C", ci));

//Console.WriteLine("");

//Console.WriteLine("M4A1_r");
//Console.WriteLine("Base Ergo: " + M4A1_r.Ergonomics);
//M4A1_r = (AssaultRifle)MyExtensions.recursiveFitRecoilWeapon2(M4A1_r, mods);
//M4A1_r = (AssaultRifle)MyExtensions.recursiveRemoveEmptyMount(M4A1_r);
//MyExtensions.recursivePrint(M4A1_r);
//Console.WriteLine("New Ergo: " + MyExtensions.recursiveErgoWeapon(M4A1_r));
//Console.WriteLine("New Recoil: " + MyExtensions.recursiveRecoilWeapon(M4A1_r));
//Console.WriteLine("Total Cost: " + MyExtensions.recursivePriceWeapon(M4A1_r).ToString("C", ci));

//Console.WriteLine("");
//Console.WriteLine("");

//Console.WriteLine("AK74N_e");
//Console.WriteLine("Base Ergo: " + AK74N_e.Ergonomics);
//AK74N_e = (AssaultRifle)MyExtensions.recursiveFitErgoWeapon2(AK74N_e, mods);
//AK74N_e = (AssaultRifle)MyExtensions.recursiveRemoveEmptyMount(AK74N_e);
//MyExtensions.recursivePrint(AK74N_e);
//Console.WriteLine("New Ergo: " + MyExtensions.recursiveErgoWeapon(AK74N_e));
//Console.WriteLine("New Recoil: " + MyExtensions.recursiveRecoilWeapon(AK74N_e));
//Console.WriteLine("Total Cost: " + MyExtensions.recursivePriceWeapon(AK74N_e).ToString("C", ci));

//Console.WriteLine("");

//Console.WriteLine("AK74N_r");
//Console.WriteLine("Base Ergo: " + AK74N_r.Ergonomics);
//AK74N_r = (AssaultRifle)MyExtensions.recursiveFitRecoilWeapon2(AK74N_r, mods);
//AK74N_r = (AssaultRifle)MyExtensions.recursiveRemoveEmptyMount(AK74N_r);
//MyExtensions.recursivePrint(AK74N_r);
//Console.WriteLine("New Ergo: " + MyExtensions.recursiveErgoWeapon(AK74N_r));
//Console.WriteLine("New Recoil: " + MyExtensions.recursiveRecoilWeapon(AK74N_r));
//Console.WriteLine("Total Cost: " + MyExtensions.recursivePriceWeapon(AK74N_r).ToString("C", ci));

//Console.WriteLine("");

//Console.WriteLine("AKM_e");
//Console.WriteLine("Base Ergo: " + AKM_e.Ergonomics);
//AKM_e = (AssaultRifle)MyExtensions.recursiveFitErgoWeapon2(AKM_e, mods);
//AKM_e = (AssaultRifle)MyExtensions.recursiveRemoveEmptyMount(AKM_e);
//MyExtensions.recursivePrint(AKM_e);
//Console.WriteLine("New Ergo: " + MyExtensions.recursiveErgoWeapon(AKM_e));
//Console.WriteLine("New Recoil: " + MyExtensions.recursiveRecoilWeapon(AKM_e));
//Console.WriteLine("Total Cost: " + MyExtensions.recursivePriceWeapon(AKM_e).ToString("C", ci));

//Console.WriteLine("");

//Console.WriteLine("AKM_r");
//Console.WriteLine("Base Ergo: " + AKM_r.Ergonomics);
//AKM_r = (AssaultRifle)MyExtensions.recursiveFitRecoilWeapon2(AKM_r, mods);
//AKM_r = (AssaultRifle)MyExtensions.recursiveRemoveEmptyMount(AKM_r);
//MyExtensions.recursivePrint(AKM_r);
//Console.WriteLine("New Ergo: " + MyExtensions.recursiveErgoWeapon(AKM_r));
//Console.WriteLine("New Recoil: " + MyExtensions.recursiveRecoilWeapon(AKM_r));
//Console.WriteLine("Total Cost: " + MyExtensions.recursivePriceWeapon(AKM_r).ToString("C", ci));

//foreach (var slot in M4A1.Slots)
//{
//    IEnumerable<WeaponMod> whiteList = mods.Where(item => slot.Filters[0].Whitelist.Contains(item.Id));
//    slot.ContainedItem = whiteList.First();

//    Console.WriteLine(slot.ContainedItem.ShortName);
//}
//Console.WriteLine(MyExtensions.calcErgo(M4A1));



//IEnumerable<float> ErgoQuerey =
//    (from weaponmod in foregrips
//     orderby weaponmod.Ergonomics
//     select weaponmod.Ergonomics).Distinct();

//IEnumerable<float> RecoilQuerey =
//    (from weaponmod in foregrips
//     orderby weaponmod.Recoil
//     select weaponmod.Recoil).Distinct();

//Dictionary< float, List<WeaponMod>> keyValuePairs = new Dictionary< float, List<WeaponMod>>();

//foreach (var weaponmod in foregrips)
//{
//    bool attempt = keyValuePairs.TryAdd(weaponmod.Recoil, new List<WeaponMod> { weaponmod });
//    if (!attempt)
//    {
//        List<WeaponMod> retArrayList;
//        keyValuePairs.TryGetValue(weaponmod.Recoil, out retArrayList);
//        if (retArrayList != null)
//        {
//            retArrayList.Add(weaponmod);
//        }
//    }
//}

//foreach( var key in keyValuePairs.Keys)
//{
//    Console.WriteLine("Recoil: "+key);
//    Console.WriteLine(keyValuePairs[key].Count());
//    foreach (var value in keyValuePairs[key])
//    {
//        Console.WriteLine(value.ShortName);
//        Console.WriteLine(value.CreditsPrice);
//    }
//}

//foreach (var item in ErgoQuerey)
//{ 
//    Console.WriteLine(item);
//}
//foreach (var item in RecoilQuerey)
//{
//    Console.WriteLine(item);
//}

//foreach (var foregrip in foregrips)
//{
//    Console.WriteLine(foregrip.Name);
//    Console.WriteLine(foregrip.Ergonomics);
//    Console.WriteLine(foregrip.Recoil);
//    Console.WriteLine(foregrip.CreditsPrice);
//}



//// Create the seed list so that our bundles only start from relevant parts
//HashSet<string> seeds = new HashSet<string>();
//foreach(var weapon in assaultRifles)
//{
//    weapon.Slots.ForEach(slot =>
//    {
//        slot.Filters.FirstOrDefault().Whitelist.ForEach(filter => seeds.Add(filter));
//    });
//}
//Console.WriteLine("Num of Seeds: " + seeds.Count());

////Create our StartBundles from the HashSet of seeds
//List<TTBundle> StartBundles = TTBundle.BuildStartBundles(seeds, mods);
//Console.WriteLine("Num of StartBundles: " + StartBundles.Count());

////List<TTMod> test = StartBundles.First().TTMod.TestMethod(mods);
////Console.WriteLine("Num of test: "+test.Count);
////test.ForEach(x => Console.WriteLine(x.ShortName));


//foreach (var bundle in StartBundles)
//{
//    Console.WriteLine("\t" + bundle.CompoundName);
//}

//TTBundle.Permutator3(StartBundles, mods);

//List<TTBundle> Permutations = TTBundle.Permutator2(StartBundles, mods);
//Console.WriteLine("Num of Permutations: " + Permutations.Count());
//foreach (var bundle in Permutations)
//{
//    Console.WriteLine("\t" + bundle.CompoundName);
//}



//List<TTWeapon> TTAssaultRifles = new List<TTWeapon>();
//List<TTMod> TTMods = new List<TTMod>();

//TTMod nothing = new TTMod()
//{
//    ID = "000",
//    ShortName = "nothing",
//    Price = 0,
//    RecoilModifier = 0,
//    Ergonomics = 0,
//};

//TTMods.Add(nothing);

//List <TTBundle> TTBundles = TTBundle.BuildBasicBundles(mods);
//Console.WriteLine(TTBundles.Count());

//foreach (TTBundle bundle in TTBundles)
//{
//    Console.WriteLine(bundle.CompoundName);
//}


//for (int i = 1413; i < 1713+300; i++)
//{
//    Console.WriteLine(TTBundles.ElementAt(i).CompoundName);
//}
//TTBundles.ForEach(a =>
//{
//    if (a.CompoundName.Contains("AK CAA RS47"))
//        Console.WriteLine(a.CompoundName);
//});

//for (int i = 0; i < 120; i++)
//{
//    Console.WriteLine(TTBundles.ElementAt(i).CompoundName + " " + TTBundles.ElementAt(i).TotalPrice);
//}

//foreach (TTBundle bundle in TTBundles)
//{
//    Console.WriteLine(bundle.CompoundName);
//}

//foreach (var assaultRifle in assaultRifles)
//{
//    // Initialize TTWeapon with attributes from RatWeapon
//    TTWeapon weapon = new TTWeapon
//    {
//        ID = assaultRifle.Id,
//        ShortName = assaultRifle.ShortName,
//        Price = assaultRifle.CreditsPrice,
//        RecoilForceUp = assaultRifle.RecoilForceUp,
//        RecoilForceBack = assaultRifle.RecoilForceBack,
//        Ergonomics = assaultRifle.Ergonomics,
//        BFireRate = assaultRifle.BFirerate,
//    };

//    // Put all the RatSlots into a list
//    var tempslots = assaultRifle.Slots.ToList();
//    foreach (var slot in tempslots)
//    {
//        // Create a TTslot object
//        TT_M_Slot ttSlot = new TT_M_Slot
//        {
//            ID = slot.Id,
//            Name = slot.Name,
//            Required = slot.Required,
//        };

//        // Populate the whitelist for the slot
//        var whitelist = slot.Filters.First().Whitelist;
//        foreach (var filter in whitelist) 
//        {
//            ttSlot.ModsWhitelist.Add(filter);
//        }

//        if (ttSlot.Required == false && ttSlot.Name != "mod_magazine")
//        {
//            // Add 'nothing' as acceptable
//            ttSlot.ModsWhitelist.Add("000");
//        }

//        // Add the slot to the TTWeapon

//        weapon.Slots.Add(ttSlot);
//    }

//    // Add the finished TT weapon to the list
//    TTAssaultRifles.Add(weapon);
//};

//foreach (var ratMod in mods)
//{
//    // Initialize TTMod with attributes from RatMod
//    TTMod tTMod = new TTMod
//    {
//        ID = ratMod.Id,
//        ShortName = ratMod.ShortName,
//        Price = ratMod.CreditsPrice,
//        RecoilModifier = ratMod.Recoil,
//        Ergonomics = (int)ratMod.Ergonomics,
//        ConflictingItems = ratMod.ConflictingItems.ToList()
//    };

//    var tempslots = ratMod.Slots.ToList();
//    foreach (var slot in tempslots)
//    {
//        // Create a TTslot object
//        TT_M_Slot ttSlot = new TT_M_Slot
//        {
//            ID = slot.Id,
//            Name = slot.Name,
//            Required = slot.Required,
//        };

//        // Populate the whitelist for the slot
//        var whitelist = slot.Filters.First().Whitelist;
//        foreach (var filter in whitelist)
//        {
//            ttSlot.ModsWhitelist.Add(filter);
//        }

//        if (ttSlot.Required == false && ttSlot.Name != "mod_magazine")
//        {
//            // Add 'nothing' as acceptable
//            ttSlot.ModsWhitelist.Add("000");
//        }

//        // Add the slot to the TTMod

//        tTMod.Slots.Add(ttSlot);
//    }


//    TTMods.Add(tTMod);
//}

//Console.WriteLine(TTAssaultRifles.Count());
//Console.WriteLine(TTMods.Count());
//var AKSU = TTAssaultRifles.Where(x => x.ShortName == "AKS-74").First();
//AKSU.RemoveAllMods();
//Console.WriteLine(AKSU.GetStats());
//AKSU.AddBestRecoilModsExclusions(TTMods);
//Console.WriteLine(AKSU.GetStats());

//foreach (var assaultRifle in assaultRifles) 
//{
//    Console.WriteLine(assaultRifle.ShortName); //Name of weapon
//    Console.WriteLine(assaultRifle.Ergonomics); 
//    Console.WriteLine(assaultRifle.RecoilForceUp); // Vert Recoil
//    Console.WriteLine(assaultRifle.RecoilForceBack); // Horiz Recoil
//    Console.WriteLine(assaultRifle.CreditsPrice); //Price
//    Console.WriteLine(assaultRifle.BFirerate); //ROF

//    foreach (var slot in assaultRifle.Slots)
//    {
//        Console.WriteLine(slot.Name);
//        foreach(var filter in slot.Filters)
//        {
//            foreach(var item in filter.Whitelist)
//            {
//                Console.WriteLine(item); //An allowed Item in that list
//                IEnumerable<Item> mods = database.GetItems(mod => mod.Id == item);
//                WeaponMod firstFoundItem = (WeaponMod)mods.FirstOrDefault();
//                Console.WriteLine(firstFoundItem.Name);
//                Console.WriteLine(firstFoundItem.CreditsPrice);
//                Console.WriteLine(firstFoundItem.Ergonomics);
//                Console.WriteLine(firstFoundItem.Recoil);
//            }
//        }
//    }
//    Console.WriteLine("");
//}


//JObject test = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(@"C:\Users\richa\source\repos\TarkovToy\Data\bsg-data.json"));

////Mod mod = JsonConvert.DeserializeObject<Mod>(File.ReadAllText(@"C:\Users\richa\source\repos\TarkovToy\Data\AKSU.json"));

////Console.WriteLine(mod.ToString());

//Console.WriteLine(test.Count);

//JEnumerable<JToken> testChildren = test.Children();


//foreach (JToken testChild in testChildren)
//{
//    if (testChild.SelectToken(".._id").ToString() == "5447b5f14bdc2d61278b4567")
//    {

//    }
//}

//Console.WriteLine(testChildren.First().ToString());

//Console.WriteLine(testChildren.Count());

//IList<string> names = test.SelectToken("_name").Select(s => (string)s).ToList();

//JToken test2 = test.First;
//Console.WriteLine(test2);

//List<Mod> mods = JsonConvert.DeserializeObject<List<Mod>>(File.ReadAllText(@"C:\Users\richa\source\repos\TarkovToy\Data\AKSU.json"));

//foreach (var m in mods)
//{
//    Console.WriteLine(m.ToString());
//}


//List<Handguard> handguards = new List<Handguard>();
//handguards.Add(new Handguard
//{
//    Name = "B-11",
//    Price = 5974,
//    Ergonomics = 3,
//    RecoilModifier = 0.01
//});
//handguards.Add(new Handguard
//{
//    Name = "6P26 Sb.6",
//    Price = 814,
//    Ergonomics = 4
//});
//handguards.Add(new Handguard
//{
//    Name = "Goliaf",
//    Price = 5818,
//    Ergonomics = 3,
//    RecoilModifier = 0.01
//});

//foreach(Handguard hg in handguards)
//{
//    Console.WriteLine(hg.ToString());

//}

//handguards.Sort();

//foreach (Handguard hg in handguards)
//{
//    Console.WriteLine(hg.ToString());

//}

//Weapon AKS_74U = new Weapon
//{
//    Core = new Core
//    {
//        Name = "AKS-74U",
//        Price = 15281,
//        RateOfFire = 650,
//        Ergonomics = 44,
//        Accuracy = 3.44,
//        RecoilVertical = 141,
//        RecoilHorizontal = 445,
//        MuzzleVelocity = 731,
//        PistolGrip = new PistolGrip
//        {
//            Name = "6P4 Sb.9",
//            Price = 806,
//            Ergonomics = 6
//        },
//        Stock = new Stock
//        {
//            Name = "6P26 Sb.5",
//            Price = 1683,
//            Ergonomics = 10,
//            RecoilModifier = .30,
//            ButtPad = new ButtPad
//            {
//                Name = "6G15U",
//                Price = 4051,
//                Ergonomics = 2,
//                RecoilModifier = .05
//            }
//        },
//        Magazine = new Magazine
//        {
//            Name = "6L20",
//            Price = 2214,
//            Ergonomics = -3,
//            Capacity = 30
//        },
//        MuzzleDevice = new MuzzleDevice
//        {
//            Name = "6P26 0-20",
//            Price = 784,
//            Ergonomics = -2,
//            RecoilModifier = .08
//        },
//        DustCover = new DustCover
//        {
//            Name = "6P26 Sb.7",
//            Price = 1306,
//            Ergonomics = 5,
//        },
//        GasTube = new GasTube
//        {
//            Name = "6P26 Sb.1-2",
//            Price = 2242,
//            Handguard = new Handguard
//            {
//                Name = "6P26 Sb.6",
//                Price = 814,
//                Ergonomics = 4
//            }
//        }
//    }
//};
//Console.WriteLine(AKS_74U.Core.PartsList());
//Console.WriteLine(AKS_74U.Core.CalcStats());