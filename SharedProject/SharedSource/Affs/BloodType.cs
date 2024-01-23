using Barotrauma;
namespace PrimMed.Affs
{

    class BloodType : Affliction
    {
        internal static readonly (string, ushort)[] RankedTypes;
        static BloodType()
        {
            (string, byte)[] config = new (string, byte)[] { ("blooda0", 62), ("blooda1", 5), ("bloodb0", 30), ("bloodb1", 2), ("bloodab0", 10), ("bloodab1", 1), ("bloodo0", 84), ("bloodo1", 6) };
            RankedTypes = new (string, ushort)[config.Length];
            ushort sum = 0;
            for (byte i = 0; i < config.Length; ++i)
            {
                (string name, byte wei) = config[i];
                sum += wei;
                RankedTypes[i] = (name, sum);
            }
        }
        internal static BloodType chooseType()
        {
            int r = Rand.Int(RankedTypes[^1].Item2, Rand.RandSync.ServerAndClient);
            int index = Array.BinarySearch(RankedTypes, (null, (ushort)r), Comparer<(string, ushort)>.Create(static (a, b) => a.Item2.CompareTo(b.Item2)));
            if (index < 0)
                index = ~index;
            (string name, _) = RankedTypes[index];
            return new BloodType(AfflictionPrefab.Prefabs[name], 1f);
        }
        public BloodType(AfflictionPrefab prefab, float strength) : base(prefab, strength) { }
        public override void Update(CharacterHealth ch, Limb targetLimb, float deltaTime)
        {
        }
    }
}