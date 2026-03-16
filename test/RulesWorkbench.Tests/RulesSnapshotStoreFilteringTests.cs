using System.Text;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;

using RulesWorkbench.Services;
using Shouldly;
using Xunit;

namespace RulesWorkbench.Tests
{
	public class RulesSnapshotStoreFilteringTests
	{
      [Fact]
		public void GetFileShareRuleSummaries_WhenQueryMatchesId_ReturnsSingleMatch()
		{
			var root = CreateTempDir();
			WriteRule(root, "a", "first");
			WriteRule(root, "b", "second");

			var env = new TestWebHostEnvironment
			{
               ContentRootPath = root,
				ContentRootFileProvider = new PhysicalFileProvider(root),
			};

			var store = new RulesSnapshotStore(new NullLogger<RulesSnapshotStore>(), env, new SystemTextJsonRuleJsonValidator());
			store.LoadFileShareRules();

			var result = store.GetFileShareRuleSummaries("b");

			result.Count.ShouldBe(1);
			result[0].Id.ShouldBe("b");
			result[0].Index.ShouldBe(1);
		}

		[Fact]
		public void UpdateFileShareRuleJson_WhenJsonValid_UpdatesInMemoryRule()
		{
            var root = CreateTempDir();
			WriteRule(root, "a", "first");
			var env = new TestWebHostEnvironment
			{
               ContentRootPath = root,
				ContentRootFileProvider = new PhysicalFileProvider(root),
			};

			var store = new RulesSnapshotStore(new NullLogger<RulesSnapshotStore>(), env, new SystemTextJsonRuleJsonValidator());
			store.LoadFileShareRules();

			var updateResult = store.UpdateFileShareRuleJson(0, "{\"id\":\"a\",\"description\":\"updated\"}");

			updateResult.IsValid.ShouldBeTrue();
			var rules = store.GetFileShareRuleSummaries(null);
			rules.Count.ShouldBe(1);
			rules[0].Description.ShouldBe("updated");
		}

		[Fact]
		public void UpdateFileShareRuleJson_WhenJsonInvalid_ReturnsInvalid()
		{
            var root = CreateTempDir();
			WriteRule(root, "a", null);
			var env = new TestWebHostEnvironment
			{
               ContentRootPath = root,
				ContentRootFileProvider = new PhysicalFileProvider(root),
			};

			var store = new RulesSnapshotStore(new NullLogger<RulesSnapshotStore>(), env, new SystemTextJsonRuleJsonValidator());
			store.LoadFileShareRules();

			var updateResult = store.UpdateFileShareRuleJson(0, "{");

			updateResult.IsValid.ShouldBeFalse();
			updateResult.ErrorMessage.ShouldNotBeNullOrWhiteSpace();
		}

		[Fact]
		public void GetFileShareRuleSummaries_WhenQueryMatchesDescription_PreservesOrder()
		{
            var root = CreateTempDir();
			WriteRule(root, "a", "match");
			WriteRule(root, "b", "match");
			var env = new TestWebHostEnvironment
			{
               ContentRootPath = root,
				ContentRootFileProvider = new PhysicalFileProvider(root),
			};

			var store = new RulesSnapshotStore(new NullLogger<RulesSnapshotStore>(), env, new SystemTextJsonRuleJsonValidator());
			store.LoadFileShareRules();

			var result = store.GetFileShareRuleSummaries("match");

			result.Count.ShouldBe(2);
			result[0].Id.ShouldBe("a");
			result[1].Id.ShouldBe("b");
			result[0].Index.ShouldBe(0);
			result[1].Index.ShouldBe(1);
		}

		private sealed class TestWebHostEnvironment : IWebHostEnvironment
		{
			public string ApplicationName { get; set; } = "RulesWorkbench.Tests";

			public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();

			public string WebRootPath { get; set; } = string.Empty;

			public string EnvironmentName { get; set; } = "Development";

			public string ContentRootPath { get; set; } = string.Empty;

			public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
		}

       private static string CreateTempDir()
		{
			var path = Path.Combine(Path.GetTempPath(), "ukho-search-tests", Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(path);
			return path;
		}

		private static void WriteRule(string contentRoot, string id, string? description)
		{
			var providerRoot = Path.Combine(contentRoot, "Rules", "file-share");
			Directory.CreateDirectory(providerRoot);
			var filePath = Path.Combine(providerRoot, $"{id}.json");

			var rule = description is null
				? $"{{\"id\":\"{id}\"}}"
				: $"{{\"id\":\"{id}\",\"description\":\"{description}\"}}";

         var doc = $"{{\"schemaVersion\":\"1.0\",\"rule\":{rule}}}";
			File.WriteAllText(filePath, doc);
		}
	}
}
