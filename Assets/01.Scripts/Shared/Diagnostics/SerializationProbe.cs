// 역할 : 클라이언트(Unity)와 서버(.NET)의 MemoryPack 직렬화 결과가 바이트 단위로 같은지 확인하는 표본.
//        양쪽 테스트가 Create() 를 직렬화해 ExpectedHex 와 비교한다. 필드를 바꾸면 ExpectedHex 도 다시 구해야 한다.
using MemoryPack;

namespace Game.Shared.Diagnostics
{
    [MemoryPackable]
    public partial class SerializationProbe
    {
        public const string ExpectedHex =
            "07E9030000A00BD5A4A10100000000704001F4FFFFFF07000000EB8F84EC82AC20446F7361"
            + "080000000B0000000C0000000D0000001500000016000000170000001F00000020000000"
            + "00004841000000000000E8C0";

        public int Id { get; set; }

        public long Timestamp { get; set; }

        public float Speed { get; set; }

        public bool IsAlive { get; set; }

        public string Name { get; set; }

        public int[] SkillIds { get; set; }

        public ProbeVector Position { get; set; }

        public static SerializationProbe Create()
        {
            return new SerializationProbe
            {
                Id = 1001,
                Timestamp = 1_793_766_788_000L,
                Speed = 3.75f,
                IsAlive = true,
                Name = "도사 Dosa",
                SkillIds = new[] { 11, 12, 13, 21, 22, 23, 31, 32 },
                Position = new ProbeVector(12.5f, 0f, -7.25f),
            };
        }
    }

    // 고정 크기 구조체(unmanaged)도 같은 바이트가 나오는지 함께 본다.
    [MemoryPackable]
    public partial struct ProbeVector
    {
        public float X;
        public float Y;
        public float Z;

        public ProbeVector(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }
}
