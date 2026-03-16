using System.Text.Json;
using System.Text.Json.Nodes;

using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace RulesWorkbench.Services
{
	public sealed class RulesSnapshotStore
	{
       private const string RulesRootDirectoryName = "Rules";
		private const string FileShareProviderDirectoryName = "file-share";
		private const string SupportedSchemaVersion = "1.0";

		private readonly ILogger<RulesSnapshotStore> _logger;
       private readonly IWebHostEnvironment _environment;
		private readonly IRuleJsonValidator _ruleJsonValidator;

		private readonly object _gate = new();
		private FileShareRulesSnapshot? _snapshot;
        private string? _fileShareRulesRootAbsolutePath;
		private bool _isUsingRuleFilesDirectory;

		public RulesSnapshotStore(
            ILogger<RulesSnapshotStore> logger,
			IWebHostEnvironment environment,
			IRuleJsonValidator ruleJsonValidator)
		{
       _logger = logger;
        _environment = environment;
		_ruleJsonValidator = ruleJsonValidator;
		}

		public IReadOnlyList<RuleSummary> GetFileShareRuleSummaries(string? query)
		{
			var snapshot = LoadFileShareRules();
			if (!snapshot.IsLoaded || snapshot.FileShareRules is null)
			{
				return Array.Empty<RuleSummary>();
			}

			var result = new List<RuleSummary>(snapshot.FileShareRules.Count);
			var trimmedQuery = string.IsNullOrWhiteSpace(query) ? null : query.Trim();
			var queryLower = trimmedQuery?.ToLowerInvariant();

         for (var i = 0; i < snapshot.FileShareRules.Count; i++)
			{
				var ruleNode = snapshot.FileShareRules[i];
				if (ruleNode is null)
				{
					continue;
				}

               var filePath = ruleNode["$filePath"]?.GetValue<string?>();
				var id = ruleNode["id"]?.GetValue<string?>();
				var description = ruleNode["description"]?.GetValue<string?>();

				if (queryLower is not null)
				{
					var matches = false;
					if (!string.IsNullOrWhiteSpace(id) && id!.ToLowerInvariant().Contains(queryLower, StringComparison.Ordinal))
					{
						matches = true;
					}
					else if (!string.IsNullOrWhiteSpace(description) && description!.ToLowerInvariant().Contains(queryLower, StringComparison.Ordinal))
					{
						matches = true;
					}

					if (!matches)
					{
						continue;
					}
				}

              result.Add(new RuleSummary(i, filePath, id, description, ruleNode));
			}

			return result;
		}

		public RuleJsonValidationResult UpdateFileShareRuleJson(int ruleIndex, string json)
		{
         _ = LoadFileShareRules();

			var validation = _ruleJsonValidator.Validate(json);
			if (!validation.IsValid)
			{
				return validation;
			}

			JsonNode? node;
			try
			{
				node = JsonNode.Parse(json);
			}
			catch (JsonException ex)
			{
				return RuleJsonValidationResult.Invalid(ex.Message);
			}

			if (node is null)
			{
				return RuleJsonValidationResult.Invalid("JSON is empty or invalid.");
			}

			var snapshot = LoadFileShareRules();
			if (!snapshot.IsLoaded || snapshot.FileShareRules is null)
			{
				return RuleJsonValidationResult.Invalid("Rules snapshot is not loaded.");
			}

			lock (_gate)
			{
				if (_snapshot?.FileShareRules is null)
				{
					return RuleJsonValidationResult.Invalid("Rules snapshot is not loaded.");
				}

				if (ruleIndex < 0 || ruleIndex >= _snapshot.FileShareRules.Count)
				{
					return RuleJsonValidationResult.Invalid("Rule index out of range.");
				}

             var existing = _snapshot.FileShareRules[ruleIndex] as JsonObject;
				var existingFilePath = existing?["$filePath"]?.GetValue<string?>();

				if (node is JsonObject obj && !string.IsNullOrWhiteSpace(existingFilePath))
				{
					obj["$filePath"] = existingFilePath;
				}

				_snapshot.FileShareRules[ruleIndex] = node;
				_logger.LogInformation("Updated in-memory file-share rule at index {RuleIndex}", ruleIndex);

             if (_isUsingRuleFilesDirectory)
				{
					try
					{
						PersistFileShareRule(node);
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Failed to persist file-share rule change at index {RuleIndex}", ruleIndex);
						return RuleJsonValidationResult.Invalid("Rule JSON updated in memory but could not be saved to disk.");
					}
				}

				return RuleJsonValidationResult.Valid();
			}
		}

		public FileShareRulesSnapshot LoadFileShareRules()
		{
			lock (_gate)
			{
				if (_snapshot is not null)
				{
					return _snapshot;
				}

				_snapshot = LoadFileShareRulesInternal();
				return _snapshot;
			}
		}

		private FileShareRulesSnapshot LoadFileShareRulesInternal()
		{
         _isUsingRuleFilesDirectory = false;
			_fileShareRulesRootAbsolutePath = null;

			if (TryGetContentRootAbsolutePath(out var contentRoot))
			{
				var rulesRoot = Path.Combine(contentRoot, RulesRootDirectoryName);
				var providerRoot = Path.Combine(rulesRoot, FileShareProviderDirectoryName);

				if (Directory.Exists(providerRoot))
				{
					_isUsingRuleFilesDirectory = true;
					_fileShareRulesRootAbsolutePath = providerRoot;
					return LoadFileShareRulesFromDirectory(providerRoot);
				}

				_logger.LogError("Rules directory not found: {ProviderRoot}", providerRoot);
				return FileShareRulesSnapshot.Failed(new RulesSnapshotError(
					providerRoot,
                 "Rules directory not found.",
					$"Expected directory '{providerRoot}'."));
			}

			_logger.LogError("RulesWorkbench requires a physical content root to load rules from the '{RulesRoot}' directory.", RulesRootDirectoryName);
			return FileShareRulesSnapshot.Failed(new RulesSnapshotError(
				RulesRootDirectoryName,
				"RulesWorkbench requires a physical content root to load rules."));
		}

		private FileShareRulesSnapshot LoadFileShareRulesFromDirectory(string providerRoot)
		{
			try
			{
				var allJsonFiles = Directory.EnumerateFiles(providerRoot, "*.json", SearchOption.AllDirectories)
					.OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
					.ToArray();

				var fileShareRules = new JsonArray();
				foreach (var filePath in allJsonFiles)
				{
					var json = File.ReadAllText(filePath);
					if (string.IsNullOrWhiteSpace(json))
					{
						return FileShareRulesSnapshot.Failed(new RulesSnapshotError(filePath, "Rule file is empty."));
					}

					JsonNode? node;
					try
					{
						node = JsonNode.Parse(json);
					}
					catch (JsonException ex)
					{
						return FileShareRulesSnapshot.Failed(new RulesSnapshotError(filePath, "Invalid JSON in rule file.", ex.Message));
					}

					var doc = node as JsonObject;
                 var schemaVersion = doc?["schemaVersion"]?.GetValue<string?>() ?? doc?["SchemaVersion"]?.GetValue<string?>();
					if (!string.Equals(schemaVersion, SupportedSchemaVersion, StringComparison.Ordinal))
					{
						return FileShareRulesSnapshot.Failed(new RulesSnapshotError(filePath, $"Unsupported SchemaVersion '{schemaVersion}'. Expected '{SupportedSchemaVersion}'."));
					}

                  var rule = (doc?["rule"] as JsonObject) ?? (doc?["Rule"] as JsonObject);
					if (rule is null)
					{
						// Allow rule documents to be stored directly at the root (schemaVersion + rule fields).
						rule = doc;
					}

					NormalizeRuleKeys(rule);

					// Track origin so edits can be persisted back to the correct file.
                   rule["$filePath"] = filePath;
					fileShareRules.Add(rule.DeepClone());
				}

				_logger.LogInformation("Loaded per-rule files from {ProviderRoot}; rules: {RulesCount}", providerRoot, fileShareRules.Count);
				return FileShareRulesSnapshot.Loaded(new JsonObject(), fileShareRules);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to load rules from directory: {ProviderRoot}", providerRoot);
				return FileShareRulesSnapshot.Failed(new RulesSnapshotError(providerRoot, "Failed to load rules from directory.", ex.Message));
			}
		}

		private void PersistFileShareRule(JsonNode node)
		{
			if (node is not JsonObject rule)
			{
				throw new InvalidOperationException("Rule must be a JSON object.");
			}

			NormalizeRuleKeys(rule);

			var id = rule["id"]?.GetValue<string?>();
			if (string.IsNullOrWhiteSpace(id))
			{
				throw new InvalidOperationException("Rule is missing required 'id'.");
			}

         if (!TryGetContentRootAbsolutePath(out var contentRoot))
			{
				throw new InvalidOperationException("Cannot persist rule files without a physical content root.");
			}
			var providerRoot = _fileShareRulesRootAbsolutePath
				?? Path.Combine(contentRoot, RulesRootDirectoryName, FileShareProviderDirectoryName);

			Directory.CreateDirectory(providerRoot);

			var existingFilePath = rule["$filePath"]?.GetValue<string?>();
			var targetPath = !string.IsNullOrWhiteSpace(existingFilePath)
				? existingFilePath!
				: Path.Combine(providerRoot, $"{id}.json");

			var ruleCopy = new JsonObject();
			foreach (var kvp in rule)
			{
              if (string.Equals(kvp.Key, "$filePath", StringComparison.Ordinal)
					|| string.Equals(kvp.Key, "schemaVersion", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				ruleCopy[kvp.Key] = kvp.Value?.DeepClone();
			}

            var doc = new JsonObject
			{
				["schemaVersion"] = SupportedSchemaVersion,
				["rule"] = ruleCopy
			};

			var json = doc.ToJsonString(new JsonSerializerOptions
			{
				WriteIndented = true
			});

			File.WriteAllText(targetPath, json);
			rule["$filePath"] = targetPath;
			_logger.LogInformation("Saved rule '{RuleId}' to {RuleFilePath}", id, targetPath);
		}

		private static void NormalizeRuleKeys(JsonObject rule)
		{
			// Allow both current rule schema casing variants (e.g. 'Id' and 'id') but normalize to lower-case
			// because the Workbench UI and filtering expect 'id'/'description'.
			if (rule.ContainsKey("id") == false && rule.TryGetPropertyValue("Id", out var idValue))
			{
               rule["id"] = idValue?.DeepClone();
				rule.Remove("Id");
			}

			if (rule.ContainsKey("description") == false && rule.TryGetPropertyValue("Description", out var descValue))
			{
                rule["description"] = descValue?.DeepClone();
				rule.Remove("Description");
			}
		}

     private bool TryGetContentRootAbsolutePath(out string contentRoot)
		{
         contentRoot = _environment.ContentRootPath;
			if (!string.IsNullOrWhiteSpace(contentRoot) && Directory.Exists(contentRoot))
			{
				return true;
			}

			if (_environment.ContentRootFileProvider is PhysicalFileProvider physical && Directory.Exists(physical.Root))
			{
				contentRoot = physical.Root;
				return true;
			}

			contentRoot = string.Empty;
			return false;
		}
	}
}
