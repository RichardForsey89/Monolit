using System.Net.Http.Json;
using System.Runtime.Serialization.Json;
using System.Xml.Linq;
using System.Xml.XPath;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Text.Json;
using System.Text.Json.Serialization;
using RatStash;
using System.Collections.Generic;
using System.Collections;
using TarkovToy.ExtensionMethods;
using System.Globalization;
using System.Diagnostics;



CultureInfo ci = new CultureInfo("ru-RU");
Console.OutputEncoding = System.Text.Encoding.Unicode;
Console.WriteLine(ci.DisplayName + " - currency symbol: " + ci.NumberFormat.CurrencySymbol);

var data_traders = new Dictionary<string, string>()
{
    {"query", "{traders(lang:en){ id name levels{ id level requiredReputation requiredPlayerLevel cashOffers{ item{ id name } priceRUB currency price }}}}" }
};

var data_baseAttachments = new Dictionary<string, string>()
{
    {"query", "{ items(categoryNames: Weapon) { id name containsItems { item { id name } } } }" }
};


JObject TradersJSON;
JObject BaseAttachmentsJSON;

using (var httpClient = new HttpClient())
{

    //Http response message
    var httpResponse = await httpClient.PostAsJsonAsync("https://api.tarkov.dev/graphql", data_traders);
    var httpResponse2 = await httpClient.PostAsJsonAsync("https://api.tarkov.dev/graphql", data_baseAttachments);

    //Response content
    var responseContent = await httpResponse.Content.ReadAsStringAsync();
    var responseContent2 = await httpResponse2.Content.ReadAsStringAsync();

    //Parse response
    TradersJSON = JObject.Parse(responseContent);
    BaseAttachmentsJSON = JObject.Parse(responseContent2);

    using (StreamWriter writetext = new StreamWriter("C:\\Users\\richa\\source\\repos\\TarkovToy\\Data\\BaseAttachments.json"))
    {
        writetext.Write(BaseAttachmentsJSON);
    }
}

/* I decided to use JSONpaths fed into the SelectTokens() method as it is reasonably readable, and string interpolation will allow for flexibility. Useful links:
 * https://www.newtonsoft.com/json/help/html/QueryJsonSelectTokenJsonPath.htm
 * https://goessner.net/articles/JsonPath/index.html#e2
 * https://stackoverflow.com/questions/38021032/multiple-filters-in-jsonpath
 * https://docs.hevodata.com/sources/streaming/rest-api/writing-jsonpath-expressions/
 * https://jsonpath.com/ <-This one is extremely helpful if you need to experiment
 */

// TRADER INFO STUFF =========================================
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
        var filtering = TradersJSON.SelectTokens(searchJSONpath).ToList();
        filtering.ForEach(x => traderMask.Add(x.ToString()));
    }
}


// BASE WEAPON BUNDLES  =========================================
Dictionary<string,List<string>> BaseWeaponBundles = new();

var weaponIDs =
    from c in BaseAttachmentsJSON["data"]["items"].Distinct()
    select (string)c["id"];

foreach (var id in weaponIDs)
{
    string searchJSONpath_BWB = $"$.data.items.[?(@.id=='{id}')]..item.id";
    var result = BaseAttachmentsJSON.SelectTokens(searchJSONpath_BWB).ToList();

    List<string> containedIDs = new ();
    foreach (var item in result) { containedIDs.Add(item.ToString()); }
    
    BaseWeaponBundles.Add(id, containedIDs);
}

// Setup of the input from the JSONs
Database database = Database.FromFile("bsg-data.json", false);

// Split the DB between mods and weapons
IEnumerable<Item> AllMods = database.GetItems(m => m is WeaponMod);
IEnumerable<Item> AllWeapons = database.GetItems(m => m is Weapon);

// Add the basic attachments to the weapons
List<Weapon> processedWeapons = AllWeapons.OfType<Weapon>().ToList();
foreach (var pair in BaseWeaponBundles)
{
    Weapon temp = processedWeapons.FirstOrDefault(x => x.Id == pair.Key);
    if (temp != null)
    {
        temp = Recursion.addBaseAttachments(temp, pair.Value, AllMods.OfType<WeaponMod>());
        int index = processedWeapons.FindIndex(x => x.Id == temp.Id);
        processedWeapons.RemoveAt(index);
        processedWeapons.Add(temp);
    }
    
}

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

Console.WriteLine("Num of mods: " + FilteredMods.Count());
Console.WriteLine("Num of guns: " + AllWeapons.Count());

// Apply the traders' mask
FilteredMods = FilteredMods.FindAll(x => traderMask.Contains(x.Id));
var FilteredGuns = processedWeapons.ToList().FindAll(x => traderMask.Contains(x.Id));

Console.WriteLine("Filt. Mods: " + FilteredMods.Count);
Console.WriteLine("Filt. Guns: " + FilteredGuns.Count);

// These are just here really to plug in to existing code
var assaultRifles = FilteredGuns.Where(c => c is AssaultRifle || c is AssaultCarbine || c is Smg).ToList();
var mods = FilteredMods.OfType<WeaponMod>();

List<Weapon> ergos = new List<Weapon>();
List<Weapon> recoils = new List<Weapon>();

foreach (Weapon rifle in assaultRifles)
{
    Weapon assaultRifle_e = (Weapon)MyExtensions.recursiveFitErgoWeapon2(rifle, mods).recursiveRemoveEmptyMount();
    Weapon assaultRifle_r = (Weapon)MyExtensions.recursiveFitRecoilWeapon2(rifle, mods).recursiveRemoveEmptyMount();

    ergos.Add(assaultRifle_e);
    recoils.Add(assaultRifle_r);
}

ergos = ergos.OrderByDescending(x => MyExtensions.recursiveErgoWeapon(x)).ToList();
recoils = recoils.OrderBy(x => MyExtensions.recursiveRecoilWeapon(x)).ToList();

Console.WriteLine("");
Console.WriteLine("");
Console.WriteLine("==== ERGO ====");
Console.WriteLine("");

var printListErgos = ergos.Take(10).ToList();
foreach (var rifle in printListErgos)
{
    MyExtensions.recursivePrint(rifle);
}

Console.WriteLine("");
Console.WriteLine("");
Console.WriteLine("==== RECOIL ====");
Console.WriteLine("");


var printListRecoils = recoils.Take(10).ToList();
foreach (var rifle in printListRecoils)
{
    MyExtensions.recursivePrint(rifle);
    Console.WriteLine("");
}