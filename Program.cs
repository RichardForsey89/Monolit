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
using System.Threading.Tasks;

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

var data_AmmoPenetration = new Dictionary<string, string>()
{
    {"query", "{ items(categoryNames: Ammo) { id name properties { ... on ItemPropertiesAmmo { penetrationPower } } } }" }
};


JObject TradersJSON;
JObject BaseAttachmentsJSON;
JObject AmmoPenetrationJSON;

using (var httpClient = new HttpClient())
{
    //Http response message
    var httpResponse = await httpClient.PostAsJsonAsync("https://api.tarkov.dev/graphql", data_traders);
    var httpResponse2 = await httpClient.PostAsJsonAsync("https://api.tarkov.dev/graphql", data_baseAttachments);
    var httpResponse3 = await httpClient.PostAsJsonAsync("https://api.tarkov.dev/graphql", data_AmmoPenetration);

    //Response content
    var responseContent = await httpResponse.Content.ReadAsStringAsync();
    var responseContent2 = await httpResponse2.Content.ReadAsStringAsync();
    var responseContent3 = await httpResponse3.Content.ReadAsStringAsync();

    //Parse response
    TradersJSON = JObject.Parse(responseContent);
    BaseAttachmentsJSON = JObject.Parse(responseContent2);
    AmmoPenetrationJSON = JObject.Parse(responseContent3);

    using (StreamWriter writetext = new StreamWriter("C:\\Users\\richa\\source\\repos\\TarkovToy\\Data\\BaseAttachments.json"))
    {
        writetext.Write(BaseAttachmentsJSON);
    }

    using (StreamWriter writetext = new StreamWriter("C:\\Users\\richa\\source\\repos\\TarkovToy\\Data\\AmmoPenetration.json"))
    {
        writetext.Write(AmmoPenetrationJSON);
    }
}

//JObject bsgdata;
//bsgdata = JObject.Parse("bsg-data.json");

//var split = bsgdata.SelectTokens("$.[*]").ToList();
//List<JToken> filtered = new List<JToken>();
//foreach (var token in split)
//{
//    token.SelectToken("$['5a33e75ac4a2826c6e06d759']['_props']['ConflictingItems']");
//}

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
int[] traderLevels = { 1, 2 };

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

// Split the DB between mods, weapons and ammo
IEnumerable<Item> AllMods = database.GetItems(m => m is WeaponMod);
IEnumerable<Item> AllWeapons = database.GetItems(m => m is Weapon);

IEnumerable<Item> AllAmmo = database.GetItems(m => m is Ammo);




// A janky workaround maybe
List<string> ammo_IDs = new();
List<int> ammo_Pens = new();
Dictionary<string, int> ammo_dict = new();

string searchAmooID = "$.data.items.[*].id";
string searchAmooPen = "$.data.items.[*]..penetrationPower";

//var ammoIDs =
//    from c in AmmoPenetrationJSON["data"]["items"].Distinct()
//    select (string)c["id"];

//var ammoPens =
//    from c in AmmoPenetrationJSON["data"]["items"].Distinct()
//    select (string)c["penetrationPower"];

var ammoIDs = AmmoPenetrationJSON.SelectTokens(searchAmooID).ToList();
var ammoPens = AmmoPenetrationJSON.SelectTokens(searchAmooPen).ToList();

ammoIDs.ToList().ForEach(x => ammo_IDs.Add(x.ToString()));
ammoPens.ToList().ForEach(x => ammo_Pens.Add(int.Parse(x.ToString())));

for (int i = 0; i < ammo_IDs.Count; i++)
{
    ammo_dict.Add(ammo_IDs[i], ammo_Pens[i]);
}

List<Ext_Ammo> processed_ammo = new();

foreach (var key in ammo_dict.Keys)
{
    var patron = AllAmmo.OfType<Ammo>().FirstOrDefault(a => a.Id == key);
    if (patron != null)
    {
        Ext_Ammo boolit = new Ext_Ammo(ammo_dict.GetValueOrDefault(key), patron);
        processed_ammo.Add(boolit);
    }
}



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
bool silencers = false;
List<Type> ModsFilter = new List<Type>() {
    typeof(IronSight), typeof(CompactCollimator), typeof(Collimator),
    typeof(OpticScope), typeof(NightVision), typeof(ThermalVision),
    typeof(AssaultScope), typeof(SpecialScope), typeof(Magazine),
    typeof(CombTactDevice), typeof(Flashlight), typeof(LaserDesignator),
    typeof(Mount)};

if (!silencers)
    ModsFilter.Add(typeof(Silencer));

// Apply that filter
var FilteredMods = AllMods.Where(mod => !ModsFilter.Contains(mod.GetType())).ToList();

// Get the mounts from AllMods into a list, filter it to be only the mounts we want (for foregrips) and add them back to FilteredMods
IEnumerable<Mount> Mounts = AllMods.OfType<Mount>();
var MountsFiltered = Mounts.Where(mod => mod.Slots.Any(slot=> slot.Name == "mod_foregrip")).Cast<Item>().ToArray();
FilteredMods.AddRange(MountsFiltered);


IEnumerable<Item> bastards = FilteredMods.Where(m => m.ConflictingItems.Count > 0);
//foreach(var bastard in bastards)
//{

//}

//var hera = bastards.FirstOrDefault(x => x.Id == "5a33e75ac4a2826c6e06d759");//ar15
//var tester = processedWeapons.FirstOrDefault(x => x.Name.Contains("M4A1"));
var hera = bastards.FirstOrDefault(x => x.Id == "619b69037b9de8162902673e");//ak74
var tester = processedWeapons.FirstOrDefault(x => x.Name.Contains("AK-74N"));  // In case of need to debug, break glass

var testResult = Recursion.HeadToHead((WeaponMod)hera, FilteredMods.OfType<WeaponMod>(), (Weapon) tester);
//Simple.inspectList(bastards.OfType<WeaponMod>().ToList(), FilteredMods.OfType<WeaponMod>().ToList());
Environment.Exit(0);

Console.WriteLine("Num of mods: " + FilteredMods.Count());
Console.WriteLine("Num of guns: " + AllWeapons.Count());
Console.WriteLine("Num of ammo: " + AllAmmo.Count());

// Apply the traders' mask
FilteredMods = FilteredMods.FindAll(x => traderMask.Contains(x.Id));
var FilteredGuns = processedWeapons.ToList().FindAll(x => traderMask.Contains(x.Id));
var FilteredAmmo = (List<Ext_Ammo>)processed_ammo.Where(x => traderMask.Contains(x?.Id)).ToList();

Console.WriteLine("Filt. Mods: " + FilteredMods.Count);
Console.WriteLine("Filt. Guns: " + FilteredGuns.Count);
Console.WriteLine("Filt. Ammo: " + FilteredAmmo.Count);

// These are just here really to plug in to existing code
var assaultRifles = FilteredGuns.Where(c => c is AssaultRifle).ToList();

//assaultRifles = assaultRifles.Where(x => x.Name.Contains("Kalashnikov AK-74N")).ToList();  // In case of need to debug, break glass

var mods = FilteredMods.OfType<WeaponMod>();

List<(Weapon, Ext_Ammo)> ergos = new ();
List<(Weapon, Ext_Ammo)> recoils = new ();


// Try making this into parallel.foreach, or other things like List<T> to ConcurrentBag<T> 
foreach (Weapon rifle in assaultRifles)
{
    Weapon assaultRifle_e = (Weapon)Recursion.recursiveFit((CompoundItem) rifle, mods, "ergo");
    var ergo_weapon = Simple.selectAmmo(assaultRifle_e, FilteredAmmo, "penetration");

    Weapon assaultRifle_r = (Weapon)Recursion.recursiveFit((CompoundItem) rifle, mods, "recoil");
    var recoil_weapon = Simple.selectAmmo(assaultRifle_r, FilteredAmmo, "penetration");

    // Stanky hack if there isn't a bullet availible within params
    if(ergo_weapon.Item2 != null)
        ergos.Add(ergo_weapon);
    if(recoil_weapon.Item2 != null)
        recoils.Add(recoil_weapon);
}

int MinimumPenetration = 20;
ergos = ergos.FindAll(x => x.Item2.PenetrationPower > MinimumPenetration);
recoils = recoils.FindAll(x => x.Item2.PenetrationPower > MinimumPenetration);

ergos = ergos.OrderByDescending(x => MyExtensions.recursiveErgoWeapon(x.Item1)).ToList();
recoils = recoils.OrderBy(x => MyExtensions.recursiveRecoilWeapon(x.Item1)).ToList();

//Console.WriteLine("");
//Console.WriteLine("");
//Console.WriteLine("==== ERGO ====");
//Console.WriteLine("");

//var printListErgos = ergos.Take(7).ToList();
//foreach (var rifle in printListErgos)
//{
//    MyExtensions.recursivePrint(rifle);
//}

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