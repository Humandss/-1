using Unity.Collections;

namespace LockDown.Ballistic.Job
{
    /// <summary>
    /// NativeArray&lt;BulletState&gt;의 슬롯을 free-list로 관리한다.
    /// O(1) 할당/반환. capacity 고정.
    /// </summary>
    public class BulletSlotAllocator : System.IDisposable
    {
        public int Capacity { get; private set; }
        public int Active { get; private set; }

        private NativeArray<BulletState> states;
        private NativeList<int> freeIndices;

        public NativeArray<BulletState> States => states;

        public void Initialize(int capacity)
        {
            Capacity = capacity;
            Active = 0;
            states = new NativeArray<BulletState>(capacity, Allocator.Persistent);
            freeIndices = new NativeList<int>(capacity, Allocator.Persistent);
            for (int i = capacity - 1; i >= 0; i--) freeIndices.Add(i);
        }

        public bool TryAllocate(out int slotIndex)
        {
            if (freeIndices.Length == 0)
            {
                slotIndex = -1;
                return false;
            }

            int last = freeIndices.Length - 1;
            slotIndex = freeIndices[last];
            freeIndices.RemoveAt(last);
            Active++;

            // isAlive 즉시 마킹. 나머지 필드는 호출자가 Spawn에서 채움.
            var s = states[slotIndex];
            s.isAlive = 1;
            states[slotIndex] = s;
            return true;
        }


        public void Release(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= Capacity) return;

            var s = states[slotIndex];
            s.isAlive = 0;
            states[slotIndex] = s;
            freeIndices.Add(slotIndex);
            Active--;
        }

        public void Dispose()
        {
            if (states.IsCreated) states.Dispose();
            if (freeIndices.IsCreated) freeIndices.Dispose();
        }
    }
}
