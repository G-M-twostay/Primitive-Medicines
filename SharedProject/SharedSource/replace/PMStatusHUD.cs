using Barotrauma;
using Barotrauma.Items.Components;
using HarmonyLib;
using System.Reflection;
using System.Xml.Linq;
namespace PrimMed.Replace
{
    partial class PMStatusHUD : StatusHUD
    {
        private static readonly AccessTools.FieldRef<ItemComponent, Dictionary<ActionType, List<StatusEffect>>> ItemComponent_statusEffectLists_ = AccessTools.FieldRefAccess<ItemComponent, Dictionary<ActionType, List<StatusEffect>>>(typeof(ItemComponent).GetField("statusEffectLists", BindingFlags.Public | BindingFlags.SetField | BindingFlags.Instance | BindingFlags.ExactBinding));
        private static readonly ContentXElement DrainBattery = new(PMMod.CntPkg, XElement.Parse("<StatusEffect type=\"OnWearing\" targettype=\"Contained\" TargetSlot=\"0\" Condition=\"-0.15\">\r\n            <RequiredItem items=\"mobilebattery\" type=\"Contained\" />          </StatusEffect>"));
        public PMStatusHUD(Item item, ContentXElement element)
            : base(item, element)
        {
            var origIc = item.components.Find(static (ic) => ic.GetType() == typeof(StatusHUD));
            item.components.Remove(origIc);
            item.updateableComponents.Remove(origIc);
            item.componentsByType[typeof(StatusHUD)] = this;
            if (!element.GetAttributeBool("thermalgoggles", false))
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
                    default:
                        DrainBattery.SetAttributeValue("Condition", "-0.1");
                        break;
                }
                ItemComponent_statusEffectLists_(this) = new(1) { { ActionType.OnWearing, new List<StatusEffect>(1) { new(DrainBattery, item.Name) } } };//apparently if there is no SE defined in xml then this variable will be set to null readonly.
            }
        }
    }
}