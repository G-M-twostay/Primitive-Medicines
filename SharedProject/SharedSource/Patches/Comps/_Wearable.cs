/*using Barotrauma;
using Barotrauma.Items.Components;
using HarmonyLib;
using System.Xml.Linq;
namespace PrimMed.Patches
{
    [HarmonyPatch(typeof(Wearable))]
    static class _Wearable
    {
        private static readonly ContentXElement DrainBattery = new(PMMod.CntPkg, XElement.Parse("<StatusEffect type=\"OnWearing\" targettype=\"Contained\" TargetSlot=\"0\" Condition=\"-0.15\">\r\n            <RequiredItem items=\"mobilebattery\" type=\"Contained\" />          </StatusEffect>"));

        [HarmonyPrefix]
        [HarmonyPatch(MethodType.Constructor, new Type[] { typeof(Item), typeof(ContentXElement) })]
        public static void PreCtor(Wearable __instance, Item item, ContentXElement element)
        {
            if (item.Prefab.Identifier == "healthscanner")
            {
                switch (item.Quality)
                {
                    case 1:
                        DrainBattery.SetAttributeValue("Condition", "-0.2");
                        break;
                    case 2:
                        DrainBattery.SetAttributeValue("Condition", "-0.15");
                        break;
                    case 3:
                        DrainBattery.SetAttributeValue("Condition", "-0.1");
                        break;
                }

            }
        }
    }
}*/