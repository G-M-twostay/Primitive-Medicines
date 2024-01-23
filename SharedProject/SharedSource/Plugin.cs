using Barotrauma;
using HarmonyLib;
using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
[assembly: IgnoresAccessChecksTo("Barotrauma")]
[assembly: IgnoresAccessChecksTo("DedicatedServer")]
namespace PrimMed
{
    public partial class PMMod : IAssemblyPlugin
    {
        private static readonly Harmony harmony = new Harmony("PMMod.patches");
        private static void regAffs(string[] bloodTypes)
        {
            //typeof(AfflictionsFile).TypeInitializer.Invoke(null,null);//this doesn't seem to work.
            var reg = new AffRegister();
            reg.register(typeof(Affs.Bandaged), "bandaged");
            reg.register(typeof(Affs.Bacterial), "bacterial0");
            reg.register(typeof(Affs.Bacterial), "bacterial1");
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
                    Dictionary<Identifier, float> suites = new Dictionary<Identifier, float>(8);
                    float a = raw ? 96f : 80f;
                    suites.Add(Utils.BLOODLOSS_PFB.Identifier, a);
                    foreach (string recipType in bloodTypes)
                        if (!Utils.bloodTypeCompat(donorType, recipType))
                            suites.Add("blood" + recipType, -a);

                    ItemPrefab_treatmentSuitability_(pfb) = suites.ToImmutableDictionary();
                }
            }
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
