using System.Text.Json;
using RulesWorkbench.Contracts;
using UKHO.Search.Ingestion.Requests;

namespace RulesWorkbench.Services
{
    public sealed class EvaluationPayloadMapper
    {
        private static readonly StringComparer TokenComparer = StringComparer.Ordinal;

        public EvaluationPayloadValidationResult Validate(EvaluationPayloadDto payload)
        {
            var errors = new List<string>();

            if (payload is null)
            {
                errors.Add("Payload is required.");
                return new EvaluationPayloadValidationResult(false, errors);
            }

            if (string.IsNullOrWhiteSpace(payload.Id))
            {
                errors.Add("Id is required.");
            }

            if (payload.SecurityTokens is null || payload.SecurityTokens.Count == 0)
            {
                errors.Add("SecurityTokens must be non-empty.");
            }
            else if (payload.SecurityTokens.Any(t => string.IsNullOrWhiteSpace(t)))
            {
                errors.Add("SecurityTokens cannot contain empty tokens.");
            }
            else
            {
                var duplicates = payload.SecurityTokens.Where(t => !string.IsNullOrWhiteSpace(t))
                    .GroupBy(t => t, TokenComparer)
                    .FirstOrDefault(g => g.Count() > 1);

                if (duplicates is not null)
                {
                    errors.Add($"SecurityTokens contains duplicate token '{duplicates.Key}'.");
                }
            }

            foreach (var property in payload.Properties ?? Enumerable.Empty<EvaluationPayloadPropertyDto>())
            {
                if (string.IsNullOrWhiteSpace(property.Name))
                {
                    errors.Add("Properties contain an item with missing Name.");
                    continue;
                }

                if (string.Equals(property.Name, "Id", StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add("Properties cannot contain a property named 'Id'. Id is a first-class field.");
                }

                if (!TryParseType(property.Type, out _))
                {
                    errors.Add($"Properties['{property.Name}'] has unsupported Type '{property.Type}'.");
                }
            }

            var duplicateNames = (payload.Properties ?? Enumerable.Empty<EvaluationPayloadPropertyDto>())
                .Where(p => !string.IsNullOrWhiteSpace(p.Name))
                .GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(g => g.Count() > 1);

            if (duplicateNames is not null)
            {
                errors.Add($"Properties contains duplicate Name '{duplicateNames.Key}'. Names are case-insensitive.");
            }

            foreach (var file in payload.Files ?? Enumerable.Empty<EvaluationPayloadFileDto>())
            {
                if (string.IsNullOrWhiteSpace(file.Filename))
                {
                    errors.Add("Files contain an item with missing Filename.");
                }

                if (file.Size < 0)
                {
                    errors.Add("Files contain an item with Size < 0.");
                }

                if (string.IsNullOrWhiteSpace(file.MimeType))
                {
                    errors.Add("Files contain an item with missing MimeType.");
                }
            }

            return errors.Count == 0
                ? EvaluationPayloadValidationResult.Success()
                : new EvaluationPayloadValidationResult(false, errors);
        }

        public (IndexRequest? Request, EvaluationPayloadValidationResult Validation) TryMapToIndexRequest(EvaluationPayloadDto payload)
        {
            var validation = Validate(payload);
            if (!validation.IsValid)
            {
                return (null, validation);
            }

            var properties = new IngestionPropertyList();
            foreach (var property in payload.Properties)
            {
                _ = TryParseType(property.Type, out var propertyType);

                properties.Add(new IngestionProperty
                {
                    Name = property.Name,
                    Type = propertyType,
                    Value = property.Value,
                });
            }

            var files = new IngestionFileList(payload.Files.Select(f => new IngestionFile
            {
                Filename = f.Filename,
                Size = f.Size,
                Timestamp = f.Timestamp ?? DateTimeOffset.UtcNow,
                MimeType = f.MimeType,
            }));

            var timestamp = payload.Timestamp ?? DateTimeOffset.UtcNow;

            try
            {
                var request = new IndexRequest(payload.Id, properties, payload.SecurityTokens.ToArray(), timestamp, files);
                return (request, EvaluationPayloadValidationResult.Success());
            }
            catch (JsonException ex)
            {
                return (null, EvaluationPayloadValidationResult.Failed(ex.Message));
            }
        }

        private static bool TryParseType(string? type, out IngestionPropertyType parsed)
        {
            if (string.Equals(type, nameof(IngestionPropertyType.String), StringComparison.OrdinalIgnoreCase))
            {
                parsed = IngestionPropertyType.String;
                return true;
            }

            if (string.Equals(type, nameof(IngestionPropertyType.Text), StringComparison.OrdinalIgnoreCase))
            {
                parsed = IngestionPropertyType.Text;
                return true;
            }

            if (string.Equals(type, nameof(IngestionPropertyType.Integer), StringComparison.OrdinalIgnoreCase))
            {
                parsed = IngestionPropertyType.Integer;
                return true;
            }

            if (string.Equals(type, nameof(IngestionPropertyType.Double), StringComparison.OrdinalIgnoreCase))
            {
                parsed = IngestionPropertyType.Double;
                return true;
            }

            if (string.Equals(type, nameof(IngestionPropertyType.Decimal), StringComparison.OrdinalIgnoreCase))
            {
                parsed = IngestionPropertyType.Decimal;
                return true;
            }

            if (string.Equals(type, nameof(IngestionPropertyType.Boolean), StringComparison.OrdinalIgnoreCase))
            {
                parsed = IngestionPropertyType.Boolean;
                return true;
            }

            if (string.Equals(type, nameof(IngestionPropertyType.DateTime), StringComparison.OrdinalIgnoreCase) ||
                string.Equals(type, "DateTimeOffset", StringComparison.OrdinalIgnoreCase))
            {
                parsed = IngestionPropertyType.DateTime;
                return true;
            }

            if (string.Equals(type, nameof(IngestionPropertyType.StringArray), StringComparison.OrdinalIgnoreCase))
            {
                parsed = IngestionPropertyType.StringArray;
                return true;
            }

            parsed = default;
            return false;
        }
    }
}
