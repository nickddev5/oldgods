using UnityEngine;

namespace OldGods.Runtime
{
    /// <summary>Test spawner for the greybox map: keeps a fixed number of enemies alive.</summary>
    public sealed class GreyboxSpawner : MonoBehaviour
    {
        public HordeManager Horde;
        public int Target = 120;
        public float SpawnPerSecond = 40f;
        float bank;

        void Update()
        {
            if (Horde == null || Horde.Types.Count == 0) return;
            if (Horde.AliveCount >= Target) return;
            bank += SpawnPerSecond * Time.deltaTime;
            while (bank >= 1f && Horde.AliveCount < Target)
            {
                bank -= 1f;
                Horde.SpawnOnRing(Horde.SpawnRng.Range(0, Horde.Types.Count));
            }
        }
    }
}
