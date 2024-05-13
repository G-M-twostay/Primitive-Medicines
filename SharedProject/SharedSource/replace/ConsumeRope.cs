using Barotrauma;
using Barotrauma.Items.Components;
namespace PrimMed.Replace
{
    partial class ConsumeRope : Rope
    {
        public ConsumeRope(Item item, ContentXElement element) : base(item, element)
        {
        }
        public override void Update(float deltaTime, Camera cam)
        {
            base.Update(deltaTime, cam);
            if (!IsActive)
                Entity.Spawner.AddItemToRemoveQueue(target);
        }
    }
}