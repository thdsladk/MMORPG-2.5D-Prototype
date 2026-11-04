// 역할 : Unity(MemoryPack netstandard2.1 + Unity 컴파일러) 직렬화 결과가 서버와 같은 바이트인지 확인한다.
//        서버 쪽 같은 테스트: Server/Tests/Shared.Tests/SerializationProbeTests.cs
using System.Text;
using Game.Shared.Diagnostics;
using MemoryPack;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public class SerializationProbeEditModeTests
    {
        [Test]
        public void Serialize_MatchesServerBytes()
        {
            byte[] bytes = MemoryPackSerializer.Serialize(SerializationProbe.Create());

            Assert.AreEqual(SerializationProbe.ExpectedHex, ToHex(bytes));
        }

        [Test]
        public void Deserialize_ServerBytes_KeepsValues()
        {
            byte[] serverBytes = FromHex(SerializationProbe.ExpectedHex);

            SerializationProbe probe = MemoryPackSerializer.Deserialize<SerializationProbe>(serverBytes);
            SerializationProbe expected = SerializationProbe.Create();

            Assert.AreEqual(expected.Id, probe.Id);
            Assert.AreEqual(expected.Name, probe.Name);
            Assert.AreEqual(expected.SkillIds, probe.SkillIds);
            Assert.AreEqual(expected.Position.Z, probe.Position.Z);
        }

        private static string ToHex(byte[] bytes)
        {
            var builder = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
            {
                builder.Append(b.ToString("X2"));
            }

            return builder.ToString();
        }

        private static byte[] FromHex(string hex)
        {
            var bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = System.Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }

            return bytes;
        }
    }
}
