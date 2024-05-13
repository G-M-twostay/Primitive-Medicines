using Barotrauma;
using HarmonyLib;
using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
[assembly: IgnoresAccessChecksTo("BarotraumaCore")]
[assembly: IgnoresAccessChecksTo("Barotrauma")]
[assembly: IgnoresAccessChecksTo("DedicatedServer")]
namespace PrimMed
{
    public partial class PMMod : IAssemblyPlugin
    {
        internal static readonly ContentPackage CntPkg = ContentPackageManager.EnabledPackages.All.FirstOrDefault(static p => p.Name == "Primitive Medicines");

        private static readonly Harmony harmony = new Harmony("PMMod.patches");
        private static void regAffs(string[] bloodTypes)
        {
            //typeof(AfflictionsFile).TypeInitializer.Invoke(null,null);//this doesn't seem to work.
            var reg = new AffRegister();
            reg.register(typeof(Affs.Bandaged), "bandaged");
            reg.register(typeof(Affs.Infection), "infection");
            reg.register(typeof(Affs.Bacterial1), "bacterial1");
            reg.register(typeof(Affs.Pain), "SurfacePain");
            reg.register(typeof(Affs.Mortal), "mortal");//register it in case someone wants to add it using command.
            reg.register(typeof(Affs.Hemolysis), "hemolysis");
            reg.register(typeof(Affs.Sealed), "vsealed");
            reg.register(typeof(Affs.LiverDmg), "liverdmg");
            reg.register(typeof(Affs.HeartDmg), "heartdmg");
            reg.register(typeof(Affs.LungDmg), "lungdmg");
            reg.register(typeof(Affs.LiverDmg), "livermissing");
            reg.register(typeof(Affs.HeartDmg), "heartmissing");
            reg.register(typeof(Affs.LungDmg), "lungmissing");
            reg.register(typeof(Affs.Rejection), "immunereject");
            reg.register(typeof(Affs.Suppressed), "immunesuppresed");
            reg.register(typeof(Affs.Pack), "heated");
            reg.register(typeof(Affs.Pack), "iced");

            foreach (string bloodType in bloodTypes)
                reg.register(typeof(Affs.BloodType), "blood" + bloodType);
        }
        private static void calcSuitableTreatment(string[] bloodTypes)
        {
            AccessTools.FieldRef<ItemPrefab, ImmutableDictionary<Identifier, float>> ItemPrefab_treatmentSuitability_ = AccessTools.FieldRefAccess<ItemPrefab, ImmutableDictionary<Identifier, float>>(typeof(ItemPrefab).GetField("treatmentSuitability", BindingFlags.NonPublic | BindingFlags.SetField | BindingFlags.Instance | BindingFlags.ExactBinding));

            foreach (ItemPrefab pfb in ItemPrefab.Prefabs)
            {
                string donorType = null;
                bool raw = false;
                if (pfb.Identifier.StartsWith("raw_"))
                {
                    donorType = pfb.Identifier.Value.Remove(0, 9);
                    raw = true;
                }
                else if (pfb.Identifier.StartsWith("proc_"))
                    donorType = pfb.Identifier.Value.Remove(0, 10);


                if (donorType is not null)
                {
                    Dictionary<Identifier, float> suites = new(8);
                    float a = raw ? 96f : 80f;
                    suites.Add(Utils.BLOODLOSS_PFB.Identifier, a);
                    foreach (string recipType in bloodTypes)
                        if (!Utils.bloodTypeCompat(donorType, recipType))
                            suites.Add("blood" + recipType, -1000);

                    ItemPrefab_treatmentSuitability_(pfb) = suites.ToImmutableDictionary();
                }
            }
        }
        private static void addNPCItem()
        {
            (ContentXElement, float)[] t0Med = { (new ContentXElement(CntPkg, XElement.Parse("<Item identifier=\"alpha_stim\"/>")), 0.05f), (new ContentXElement(CntPkg, XElement.Parse("<Item identifier=\"traumafix\"/>")), 0.1f), (new ContentXElement(CntPkg, XElement.Parse("<Item identifier=\"gamma_stim\"/>")), 0.075f), (new ContentXElement(CntPkg, XElement.Parse("<Item identifier=\"pacifiedhusk\"/>")), 0.05f) };
            (ContentXElement, float)[] t1Med = { (new ContentXElement(CntPkg, XElement.Parse("<Item identifier=\"prednisone\"/>")), 0.1f), (new ContentXElement(CntPkg, XElement.Parse("<Item identifier=\"Ciclosporin\"/>")), 0.2f) };
            (ContentXElement, float)[] t2Med = { (new ContentXElement(CntPkg, XElement.Parse("<Item identifier=\"aspirin\"/>")), 0.2f), (new ContentXElement(CntPkg, XElement.Parse("<Item identifier=\"antibiotics\"/>")), 0.4f), (new ContentXElement(CntPkg, XElement.Parse("<Item identifier=\"adrenaline\"/>")), 0.3f) };
            (ContentXElement, float)[] t3Med = { (new ContentXElement(CntPkg, XElement.Parse("<Item identifier=\"disinfectant\"/>")), 0.5f), (new ContentXElement(CntPkg, XElement.Parse("<Item identifier=\"antibloodloss2\"/>")), 0.6f), (new ContentXElement(CntPkg, XElement.Parse("<Item identifier=\"heatpack\"/>")), 0.35f), (new ContentXElement(CntPkg, XElement.Parse("<Item identifier=\"antibleeding1\"/>")), 0.75f) };

            void copySet(HumanPrefab p, IEnumerable<(ContentXElement, float)> toAdd)
            {
                if (p is not null)
                {
                    List<(ContentXElement, float)> newSets = new(p.ItemSets.Count);
                    foreach ((ContentXElement origSet, float origCom) in p.ItemSets)
                    {
                        foreach ((ContentXElement it, float com) in toAdd)
                        {
                            var newEle = new ContentXElement(CntPkg, new XElement(origSet.Element));
                            newEle.Add(it);
                            newSets.Add((newEle, origCom * com));
                        }
                    }
                    p.ItemSets.AddRange(newSets);
                }
            }
            var t = NPCSet.Get("outpostnpcs1", "merchantresearch");
            copySet(t, t0Med);
            copySet(t, t1Med);
            copySet(NPCSet.Get("outpostnpcs1", "researcher"), t0Med);
            t = NPCSet.Get("outpostnpcs1", "outpostdoctor");
            copySet(t, t3Med);
            copySet(t, t2Med);
            copySet(t, t1Med);
            copySet(NPCSet.Get("outpostnpcs1", "merchantmedical"), t2Med.Concat(t1Med));
            copySet(NPCSet.Get("outpostnpcs1", "securitynpccoalition"), t3Med);
            copySet(NPCSet.Get("outpostnpcs1", "securitynpcseparatists"), t3Med);

            copySet(NPCSet.Get("abandonedoutpostnpcs", "bandit"), t3Med);
            t = NPCSet.Get("abandonedoutpostnpcs", "bandit_heavy");
            copySet(t, t3Med);
            copySet(t, t3Med);
            t = NPCSet.Get("abandonedoutpostnpcs", "bandit_elite");
            copySet(t, t3Med);
            copySet(t, t2Med);
            copySet(NPCSet.Get("abandonedoutpostnpcs", "banditleader"), t3Med.Concat(t2Med));
            t = NPCSet.Get("abandonedoutpostnpcs", "banditleader_heavy");
            copySet(t, t3Med.Concat(t2Med));
            copySet(t, t3Med);
            copySet(NPCSet.Get("abandonedoutpostnpcs", "psychoclown"), t3Med);
        }
        public void Initialize()
        {
            // When your plugin is loading, use this instead of the constructor
            // Put any code here that does not rely on other plugins.
            harmony.PatchAll();
            string[] bloodTypes = new string[] { "a0", "a1", "b0", "b1", "o0", "o1", "ab0", "ab1" };

            regAffs(bloodTypes);
            calcSuitableTreatment(bloodTypes);
        }

        public void OnLoadCompleted()
        {
            // After all plugins have loaded
            // Put code that interacts with other plugins here.
            addNPCItem();
        }

        public void PreInitPatching()
        {
            // Not yet supported: Called during the Barotrauma startup phase before vanilla content is loaded.
        }

        public void Dispose()
        {
            // Cleanup your plugin!
            //throw new NotImplementedException();
        }
    }
}
