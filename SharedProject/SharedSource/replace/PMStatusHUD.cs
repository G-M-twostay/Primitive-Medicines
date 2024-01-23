using Barotrauma;
using Barotrauma.Items.Components;
namespace PrimMed.Replace
{
    partial class PMStatusHUD : StatusHUD
    {
        public PMStatusHUD(Item item, ContentXElement element)
            : base(item, element)
        {
            var origIc = item.components.Find(static (ic) => ic.GetType() == typeof(StatusHUD));
            item.components.Remove(origIc);
            item.updateableComponents.Remove(origIc);
            item.componentsByType[typeof(StatusHUD)] = this;
        }
    }
}