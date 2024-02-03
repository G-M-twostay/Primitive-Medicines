using Barotrauma;

namespace PrimMed
{
    partial class Utils
    {
        internal static readonly string[] SCALPEL_ACTIONS = new string[] { "scalpel.incise", "scalpel.lung", "scalpel.liver", "scalpel.heart" };
        internal static readonly AfflictionPrefab
            PAIN_PFB = AfflictionPrefab.Prefabs["SurfacePain"],
            BACTERIAL0_PFB = AfflictionPrefab.Prefabs["bacterial0"],
            BACTERIAL1_PFB = AfflictionPrefab.Prefabs["bacterial1"],
            HEMOLYSIS_PFB = AfflictionPrefab.Prefabs["hemolysis"],
            BLOODLOSS_PFB = AfflictionPrefab.Prefabs["bloodloss"],
            PIERCE_PFB = AfflictionPrefab.Prefabs["pierce"],
            SEALED_PFB = AfflictionPrefab.Prefabs["vsealed"],
            INCISION_PFB = AfflictionPrefab.Prefabs["incision"],
            SULTURAL_PFB = AfflictionPrefab.Prefabs["sutural"],
            SCAR_PFB = AfflictionPrefab.Prefabs["scar"],
            CNCT_PFB = AfflictionPrefab.Prefabs["vconnected"],
            LUNG_DMG_PFB = AfflictionPrefab.Prefabs["lungdmg"],
            LIVER_DMG_PFB = AfflictionPrefab.Prefabs["liverdmg"],
            HEART_DMG_PFB = AfflictionPrefab.Prefabs["heartdmg"],
            IN_DMG_PFB = AfflictionPrefab.Prefabs["internaldamage"],
            HEATED_PFB = AfflictionPrefab.Prefabs["heated"],
            ICED_PFB = AfflictionPrefab.Prefabs["iced"],
            INCENDIUM_PFB = AfflictionPrefab.Prefabs["heated1"];
        internal static readonly ItemPrefab LIQUIDBAG_PFB = ItemPrefab.Prefabs["liquidbag"];
    }
}