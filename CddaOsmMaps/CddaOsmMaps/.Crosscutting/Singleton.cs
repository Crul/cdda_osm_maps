using System;

namespace CddaOsmMaps.Crosscutting
{
    // TODO add random seed and set via command line arg
    public sealed class RandomSingleton
    {
        public Random Rnd { get; private set; }

        private RandomSingleton()
            => Rnd = new Random();

        public static RandomSingleton Instance { get { return Nested.instance; } }

        private class Nested
        {
            // Explicit static constructor to tell C# compiler
            // not to mark type as beforefieldinit
            static Nested() { }

            internal static readonly RandomSingleton instance = new RandomSingleton();
        }
    }
}
