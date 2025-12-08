namespace Advent2025.Day08;

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using Vec = Vector3D<int>;

internal unsafe sealed class Day08 : DayBase {
    private const int SIZE = 1_000;
    private const int PAIR_COUNT = SIZE * SIZE - SIZE >>> 1;
    private const int INITIAL_UNION_COUNT = 1_000;
    private const int TOTAL_UNION_ESTIMATE = 4_200;

    private static readonly long* dists = (long*)NativeMemory.Alloc(PAIR_COUNT, sizeof(long));
    private static readonly long* dists2 = (long*)NativeMemory.Alloc(TOTAL_UNION_ESTIMATE, sizeof(long));
    
    private readonly int prodOfThreeLargest;
    private readonly int xProduct;

    [SkipLocalsInit]
    public Day08() {
        static ushort* getNextAligned(byte* ptr) {
            var delta = 32 - ((nint)ptr & 31) & 31;
            return (ushort*)(ptr + delta);
        }

        using var reader = GetDataReader();

        Span<Vec> points = stackalloc Vec[SIZE];
        var dsuBytes = stackalloc byte[(SIZE << 2) + (sizeof(Vector256<ushort>) << 1)];
        var dsuBuffer = getNextAligned(dsuBytes);
        var countsBuffer = getNextAligned((byte*)(dsuBuffer + SIZE));

        for (var write = 0; !reader.EndOfStream; ++write)
            points[write] = new(reader.ReadNextInt(), reader.ReadNextInt(), reader.ReadNextInt());

        for (var (i, pairIdx) = (0L, 0); i < SIZE; ++i) {
            ref var a = ref points[(int)i];
            for (var j = i + 1; j < SIZE; ++j, ++pairIdx)
                dists[pairIdx] = a.SquaredDistanceTo<long>(points[(int)j]) << 20 | i << 10 | j;
        }

        var sorted = SortSmallest(dists, dists2, PAIR_COUNT, TOTAL_UNION_ESTIMATE);

        DSU dsu = new(dsuBuffer, countsBuffer, SIZE);
        const long IDX_MASK = 0x3FF;
        for (var i = 0; i < INITIAL_UNION_COUNT; ++i) {
            var packed = sorted[i];
            var (a, b) = ((ushort)(packed >>> 10 & IDX_MASK), (ushort)(packed & IDX_MASK));
            dsu.Union(a, b);
        }

        var sizes = stackalloc ushort[SIZE];
        var sizesEnd = sizes - 1;
        for (ushort i = 0; i < SIZE; ++i) {
            var id = dsu.Find(i);
            if (id != i)
                continue;
            var c = countsBuffer[id];
            if (c > 1)
                *++sizesEnd = c;
        }

        Debug.Assert(sizesEnd - sizes >= 2);
        QuickSelectLargest(sizes, sizes, sizesEnd, 3);
        prodOfThreeLargest = *sizes * sizes[1] * sizes[2];

        for (var i = INITIAL_UNION_COUNT; true; ++i) {
            Debug.Assert(i < TOTAL_UNION_ESTIMATE);
            var packed = sorted[i];
            var (a, b) = ((ushort)(packed >>> 10 & IDX_MASK), (ushort)(packed & IDX_MASK));
            dsu.Union(a, b);
            if (countsBuffer[a] == SIZE) {
                xProduct = points[a].x * points[b].x;
                break;
            }
        }
    }

    private static T QuickSelectLargest<T>(T* data, T* left, T* right, int k) where T : unmanaged, IComparisonOperators<T, T, bool> {
        while (true) {
            var offset = Random.Shared.Next((int)(right - left) + 1);
            var pivot = left + offset;
            var pivotVal = *pivot;
            (*pivot, *right) = (*right, *pivot);

            var lt = left;
            var gt = right;
            for (var curr = left; curr <= gt;) {
                if (*curr > pivotVal) {
                    (*curr, *lt) = (*lt, *curr);
                    ++curr;
                    ++lt;
                } else if (*curr < pivotVal) {
                    (*curr, *gt) = (*gt, *curr);
                    --gt;
                } else
                    ++curr;
            }

            if (k < lt - data)
                right = lt - 1;
            else if (k > gt - data)
                left = gt + 1;
            else
                return pivotVal;
        }
    }

    private static T* SortSmallest<T>(T* nums, T* buffer, int len, int k) where T : unmanaged, IBinaryInteger<T> {
        var kth = QuickSelectLargest(nums, nums, nums + (len - 1), len - k);

        var write = nums - 1;
        var writeEnd = nums + k;
        for (var curr = nums; write < writeEnd; ++curr) {
            if (*curr <= kth)
                *++write = *curr;
        }
        
        if ( k < 5) {
            new Span<T>(nums, k).Sort();
            return nums;
        }

        return Sort(nums, buffer, k);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static T* Sort<T>(T* nums, T* buffer, int len) where T : unmanaged, IBinaryInteger<T> {
        const int EXP = 8;
        const int COUNTS_LEN = 1 << EXP;
        const int SET_LEN = 1 << (EXP - 6);

        var digitMask_MASK = (T.One << EXP) - T.One;

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        static void reorder(T* nums, T* buffer, int len, int* counts, ulong* set, int offset) {
            var digitMask_MASK = (T.One << EXP) - T.One;

            var sum = 0;
            for (var i = 0; i < SET_LEN; ++i) {
                var countsBase = counts + (i << 6);
                for (var bits = set[i]; bits != 0; bits = Bmi1.X64.ResetLowestSetBit(bits)) {
                    var c = countsBase + Bmi1.X64.TrailingZeroCount(bits);
                    (*c, sum) = (sum, sum + *c);
                }
            }

            for (var i = 0; i < len; ++i) {
                var num = nums[i];
                var digit = int.CreateTruncating(num >>> offset & digitMask_MASK);
                buffer[counts[digit]++] = num;
            }
        }

        var counts = stackalloc int[COUNTS_LEN];
        var set = stackalloc ulong[SET_LEN];

        const int MIN_OFFSET = 20;
        const int MAX_OFFSET = MIN_OFFSET + 34;

        for (var i = 0; i < len; ++i) {
            var num = nums[i] >>> MIN_OFFSET;
            var digit = int.CreateTruncating(num & digitMask_MASK);
            Debug.Assert(digit < COUNTS_LEN);
            ++counts[digit];
            set[digit >>> 6] |= 1UL << (int)digit;
        }

        reorder(nums, buffer, len, counts, set, MIN_OFFSET);

        var offsetEnd = MAX_OFFSET;
        for (var offset = MIN_OFFSET + EXP; offset < offsetEnd; offset += EXP) {
            var tmp = buffer;
            buffer = nums;
            nums = tmp;

            NativeMemory.Clear(counts, COUNTS_LEN << 2);
            NativeMemory.Clear(set, SET_LEN << 3);
            for (var i = 0; i < len; ++i) {
                var shifted = nums[i] >>> offset;
                var digit = int.CreateTruncating(shifted & digitMask_MASK);
                Debug.Assert(digit < COUNTS_LEN);
                ++counts[digit];
                set[digit >>> 6] |= 1UL << (int)digit;
            }

            reorder(nums, buffer, len, counts, set, offset);
        }

        return buffer;
    }

    public override object? Part1() {
        Print("The product of the three largest group sizes after 1,000 connections is: {0}", prodOfThreeLargest);
        return Box<int>.Instance(prodOfThreeLargest);
    }

    public override object? Part2() {
        Print("The product of the final two X coordinates is: {0}", xProduct);
        return Box<int>.Instance(xProduct);
    }
}
