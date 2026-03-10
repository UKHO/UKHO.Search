using System.Reflection;
using System.Text;
using Shouldly;
using UKHO.Search.Pipelines.Nodes;
using Xunit;

namespace UKHO.Search.Tests.Pipelines
{
    public sealed class KeyPartitionHashVectorsTests
    {
        [Theory]
        [InlineData("abc", 97)]
        [InlineData("café", 97)]
        [InlineData("💩", 97)]
        public void Partition_hash_matches_utf8_bytes_fnv1a(string key, int partitions)
        {
            var actual = GetPartitionViaReflection(key, partitions);
            var expected = GetPartitionUtf8BytesReference(key, partitions);
            actual.ShouldBe(expected);
        }

        [Fact]
        public void Non_ascii_keys_do_not_match_char_enumeration_hashing()
        {
            var key = "café";
            const int partitions = 97;

            var actual = GetPartitionViaReflection(key, partitions);
            var utf8 = GetPartitionUtf8BytesReference(key, partitions);
            var chars = GetPartitionCharEnumerationReference(key, partitions);

            actual.ShouldBe(utf8);
            utf8.ShouldNotBe(chars);
        }

        private static int GetPartitionViaReflection(string key, int partitions)
        {
            var method = typeof(KeyPartitionNode<int>).GetMethod("GetPartition", BindingFlags.Static | BindingFlags.NonPublic);

            method.ShouldNotBeNull();

            return (int)method!.Invoke(null, new object[] { key, partitions })!;
        }

        private static int GetPartitionUtf8BytesReference(string key, int partitions)
        {
            unchecked
            {
                var hash = 2166136261;
                var bytes = Encoding.UTF8.GetBytes(key);
                for (var i = 0; i < bytes.Length; i++)
                {
                    hash ^= bytes[i];
                    hash *= 16777619;
                }

                return (int)(hash % (uint)partitions);
            }
        }

        private static int GetPartitionCharEnumerationReference(string key, int partitions)
        {
            unchecked
            {
                var hash = 2166136261;
                for (var i = 0; i < key.Length; i++)
                {
                    hash ^= key[i];
                    hash *= 16777619;
                }

                return (int)(hash % (uint)partitions);
            }
        }
    }
}