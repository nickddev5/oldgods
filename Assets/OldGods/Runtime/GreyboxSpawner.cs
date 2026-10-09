using UnityEngine;

namespace OldGods.Runtime
{
    /// <summary>Test spawner for the greybox map: keeps a fixed number of enemies alive.</summary>
    public sealed class GreyboxSpawner : MonoBehaviour
    {
        public HordeManager Horde;
        [Tooltip("Enemies kept alive at the start; grows by RampPerSecond up to Target.")]
        public int StartCount = 25;
        public float RampPerSecond = 1.2f;
        public int Target = 350;
        public float SpawnPerSecond = 40f;
        [Tooltip("Enemy health grows by this fraction per minute.")]
        public float HealthPerMinute = 0.5f;
        float bank, elapsed;

        void Update()
        {
            if (Horde == null || Horde.Types.Count == 0) return;
            elapsed += Time.deltaTime;
            int want = Mathf.Min(Target, StartCount + Mathf.FloorToInt(elapsed * RampPerSecond));
            if (Horde.AliveCount >= want) return;
            bank += SpawnPerSecond * Time.deltaTime;
            float health = 1f + HealthPerMinute * elapsed / 60f;
            while (bank >= 1f && Horde.AliveCount < want)
            {
                bank -= 1f;
                Horde.SpawnOnRing(Horde.SpawnRng.Range(0, Horde.Types.Count), health);
            }
        }
    }
}
