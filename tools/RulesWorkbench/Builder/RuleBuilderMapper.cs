using System.Text.Json.Nodes;

namespace RulesWorkbench.Builder
{
	public sealed class RuleBuilderMapper : IRuleBuilderMapper
	{
		public RuleBuilderMappingResult<BuilderRule> TryParse(JsonNode ruleJson)
		{
			// v1: builder only supports constructing new rules and editing a narrow subset.
			// For existing rules, we currently treat JSON->builder mapping as unsupported.
			return RuleBuilderMappingResult<BuilderRule>.Unsupported("JSON to Builder mapping is not implemented for v1.");
		}

		public JsonObject ToJson(BuilderRule rule)
		{
			var ifObject = new JsonObject();
			var conditions = new JsonArray();
			foreach (var c in rule.If.Conditions)
			{
				conditions.Add(ConditionToJson(c));
			}

			ifObject[rule.If.Composition == CompositionType.All ? "all" : "any"] = conditions;

			var thenObject = new JsonObject();
			foreach (var action in rule.Then)
			{
				ApplyThenAction(thenObject, action);
			}

			return new JsonObject
			{
				["id"] = rule.Id,
				["description"] = rule.Description,
				["if"] = ifObject,
				["then"] = thenObject,
			};
		}

		private static JsonObject ConditionToJson(BuilderCondition condition)
		{
			return condition.Type switch
			{
				ConditionType.PropertiesEqualsString => new JsonObject
				{
					[$"properties[\"{condition.PropertyName}\"]"] = condition.Value,
				},
				ConditionType.PropertiesExists => new JsonObject
				{
					["path"] = $"properties[\"{condition.PropertyName}\"]",
					["exists"] = true,
				},
				ConditionType.FilesMimeTypeEquals => new JsonObject
				{
					["path"] = "files[*].mimeType",
					["equals"] = condition.Value,
				},
				_ => new JsonObject(),
			};
		}

		private static void ApplyThenAction(JsonObject thenObject, BuilderThenAction action)
		{
			static JsonObject Ensure(JsonObject root, string name)
			{
				if (root[name] is JsonObject obj)
				{
					return obj;
				}

				obj = new JsonObject();
				root[name] = obj;
				return obj;
			}

			static JsonArray ValuesToJsonArray(IReadOnlyList<string> values)
			{
				var array = new JsonArray();
				foreach (var v in values)
				{
					array.Add(v);
				}

				return array;
			}

			switch (action.Type)
			{
				case ThenActionType.KeywordsAdd:
					Ensure(thenObject, "keywords")["add"] = ValuesToJsonArray(action.Values);
					break;
				case ThenActionType.SearchTextAdd:
					Ensure(thenObject, "searchText")["add"] = ValuesToJsonArray(action.Values);
					break;
				case ThenActionType.ContentAdd:
					Ensure(thenObject, "content")["add"] = ValuesToJsonArray(action.Values);
					break;
				case ThenActionType.CategoryAdd:
					Ensure(thenObject, "category")["add"] = ValuesToJsonArray(action.Values);
					break;
				case ThenActionType.SeriesAdd:
					Ensure(thenObject, "series")["add"] = ValuesToJsonArray(action.Values);
					break;
				case ThenActionType.InstanceAdd:
					Ensure(thenObject, "instance")["add"] = ValuesToJsonArray(action.Values);
					break;
				case ThenActionType.AuthorityAdd:
					Ensure(thenObject, "authority")["add"] = ValuesToJsonArray(action.Values);
					break;
				case ThenActionType.RegionAdd:
					Ensure(thenObject, "region")["add"] = ValuesToJsonArray(action.Values);
					break;
				case ThenActionType.MajorVersionSet:
					Ensure(thenObject, "majorVersion")["set"] = ValuesToJsonArray(action.Values);
					break;
				case ThenActionType.MinorVersionSet:
					Ensure(thenObject, "minorVersion")["set"] = ValuesToJsonArray(action.Values);
					break;
				default:
					break;
			}
		}
	}
}
