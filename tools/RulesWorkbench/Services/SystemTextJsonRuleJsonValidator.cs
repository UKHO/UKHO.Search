using System.Text.Json;

namespace RulesWorkbench.Services
{
	public sealed class SystemTextJsonRuleJsonValidator : IRuleJsonValidator
	{
		public RuleJsonValidationResult Validate(string json)
		{
			if (string.IsNullOrWhiteSpace(json))
			{
				return RuleJsonValidationResult.Invalid("JSON is empty.");
			}

			try
			{
               using var document = JsonDocument.Parse(json);
				var ruleElement = GetRuleElement(document.RootElement);

				if (ruleElement.ValueKind != JsonValueKind.Object)
				{
					return RuleJsonValidationResult.Invalid("Rule JSON must be an object.");
				}

				if (!TryGetStringProperty(ruleElement, "title", out var title)
					|| string.IsNullOrWhiteSpace(title))
				{
					return RuleJsonValidationResult.Invalid("Rule JSON must include a non-empty 'title' property.");
				}

				return RuleJsonValidationResult.Valid();
			}
			catch (JsonException ex)
			{
				return RuleJsonValidationResult.Invalid(ex.Message);
			}
		}

		private static JsonElement GetRuleElement(JsonElement rootElement)
		{
			if (rootElement.ValueKind == JsonValueKind.Object
				&& TryGetProperty(rootElement, "rule", out var ruleElement)
				&& ruleElement.ValueKind == JsonValueKind.Object)
			{
				return ruleElement;
			}

			return rootElement;
		}

		private static bool TryGetStringProperty(JsonElement element, string propertyName, out string? value)
		{
			value = null;

			if (!TryGetProperty(element, propertyName, out var property)
				|| property.ValueKind != JsonValueKind.String)
			{
				return false;
			}

			value = property.GetString();
			return true;
		}

		private static bool TryGetProperty(JsonElement element, string propertyName, out JsonElement value)
		{
			foreach (var property in element.EnumerateObject())
			{
				if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
				{
					value = property.Value;
					return true;
				}
			}

			value = default;
			return false;
		}
	}
}
