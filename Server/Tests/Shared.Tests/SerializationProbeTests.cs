// 역할 : MemoryPack 직렬화가 Unity 쪽과 같은 바이트를 내는지 고정값(ExpectedHex)으로 확인한다.
//        Unity EditMode 테스트(SerializationProbeEditModeTests)도 같은 ExpectedHex 와 비교한다.
using Game.Shared.Diagnostics;
using MemoryPack;

namespace Game.Shared.Tests;

public class SerializationProbeTests
{
    [Fact]
    public void Serialize_MatchesExpectedBytes()
    {
        byte[] bytes = MemoryPackSerializer.Serialize(SerializationProbe.Create());

        Assert.Equal(SerializationProbe.ExpectedHex, Convert.ToHexString(bytes));
    }

    [Fact]
    public void RoundTrip_KeepsAllValues()
    {
        SerializationProbe original = SerializationProbe.Create();

        SerializationProbe? copy = MemoryPackSerializer.Deserialize<SerializationProbe>(MemoryPackSerializer.Serialize(original));

        Assert.NotNull(copy);
        Assert.Equal(original.Id, copy!.Id);
        Assert.Equal(original.Timestamp, copy.Timestamp);
        Assert.Equal(original.Speed, copy.Speed);
        Assert.Equal(original.IsAlive, copy.IsAlive);
        Assert.Equal(original.Name, copy.Name);
        Assert.Equal(original.SkillIds, copy.SkillIds);
        Assert.Equal(original.Position, copy.Position);
    }
}
