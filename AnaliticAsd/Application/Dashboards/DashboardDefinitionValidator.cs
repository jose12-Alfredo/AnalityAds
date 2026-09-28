using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AnaliticAsd.Application.Dashboards;

public sealed class DashboardDefinitionValidator(IDashboardSourceAccess sourceAccess)
    : IDashboardDefinitionValidator
{
    public const int CurrentSchemaVersion = 1;
    public const int MaxDocumentBytes = 1_048_576;
    public const int MaxPages = 25;
    public const int MaxComponentsPerPage = 250;
    public const int MaxTotalComponents = 1500;
    private const int MaxPageNameLength = 120;
    private const int MaxSourceSlotLength = 100;
    private const int MinCanvasWidth = 320;
    private const int MaxCanvasWidth = 4096;
    private const int MinCanvasHeight = 320;
    private const int MaxCanvasHeight = 20_000;
    private const int MaxZIndex = 100_000;

    private static readonly JsonSerializerOptions JsonOptions = CreateOptions();
    private static readonly HashSet<string> ForbiddenFreeFormKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "token", "accessToken", "refreshToken", "clientSecret", "secret", "credential", "credentials",
        "password", "dataSourceId", "dataSourceIds", "sourceId"
    };

    public string CreateBlank() => Serialize(new DashboardDefinitionV1
    {
        SchemaVersion = CurrentSchemaVersion,
        Pages =
        [
            new DashboardPageV1
            {
                Id = Guid.NewGuid(),
                Name = "Página 1",
                Order = 0,
                Width = 1440,
                Height = 900
            }
        ]
    });

    public async Task<ValidatedDashboardDefinition> ValidateDashboardAsync(string json, Guid clientId,
        CancellationToken cancellationToken = default)
    {
        if (clientId == Guid.Empty) throw new ArgumentException("Client id is required.", nameof(clientId));
        var definition = ParseAndValidate(json, template: false);
        var sourceIds = SourceReferences(definition)
            .Where(reference => reference.DataSourceId is not null)
            .Select(reference => reference.DataSourceId!.Value)
            .Distinct()
            .ToArray();
        if (sourceIds.Length > 0 && !await sourceAccess.AllBelongToClientAsync(clientId, sourceIds, cancellationToken))
            throw new ArgumentException("The dashboard contains a data source that is not assigned to its client.");
        return new ValidatedDashboardDefinition(CurrentSchemaVersion, Serialize(definition));
    }

    public async Task<ValidatedDashboardDefinition> ValidateForPublicationAsync(string json, Guid clientId,
        CancellationToken cancellationToken = default)
    {
        var validated = await ValidateDashboardAsync(json, clientId, cancellationToken);
        var definition = ParseAndValidate(validated.Json, template: false);
        var unbound = SourceReferences(definition).FirstOrDefault(reference => reference.SourceSlot is not null);
        if (unbound is not null)
            throw new ArgumentException(
                $"Source slot '{unbound.SourceSlot}' must be bound to a client data source before publishing.");
        return validated;
    }

    public ValidatedDashboardDefinition ValidateTemplate(string json)
    {
        var definition = ParseAndValidate(json, template: true);
        return new ValidatedDashboardDefinition(CurrentSchemaVersion, Serialize(definition));
    }

    public async Task<ValidatedDashboardDefinition> BindTemplateAsync(string templateJson,
        IReadOnlyDictionary<string, Guid> sourceBindings, Guid clientId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceBindings);
        var definition = ParseAndValidate(templateJson, template: true);
        var usedSlots = SourceReferences(definition).Select(reference => reference.SourceSlot!)
            .ToHashSet(StringComparer.Ordinal);
        var unknownSlots = sourceBindings.Keys.Where(key => !usedSlots.Contains(key)).ToArray();
        if (unknownSlots.Length > 0)
            throw new ArgumentException($"Unknown template source slot '{unknownSlots[0]}'.", nameof(sourceBindings));
        foreach (var slot in usedSlots)
            if (!sourceBindings.TryGetValue(slot, out var sourceId) || sourceId == Guid.Empty)
                throw new ArgumentException($"A data source is required for template slot '{slot}'.",
                    nameof(sourceBindings));

        foreach (var reference in SourceReferences(definition))
        {
            reference.DataSourceId = sourceBindings[reference.SourceSlot!];
            reference.SourceSlot = null;
        }
        return await ValidateDashboardAsync(Serialize(definition), clientId, cancellationToken);
    }

    public async Task<ValidatedDashboardDefinition> PrepareDuplicateAsync(string dashboardJson, Guid sourceClientId,
        Guid destinationClientId, CancellationToken cancellationToken = default)
    {
        var validated = await ValidateDashboardAsync(dashboardJson, sourceClientId, cancellationToken);
        if (sourceClientId == destinationClientId) return validated;

        var definition = ParseAndValidate(validated.Json, template: false);
        var slotsBySource = new Dictionary<Guid, string>();
        var slotNumber = 1;
        foreach (var reference in SourceReferences(definition))
        {
            if (reference.DataSourceId is not { } sourceId) continue;
            if (!slotsBySource.TryGetValue(sourceId, out var slot))
            {
                slot = $"rebind-source-{slotNumber++}";
                slotsBySource.Add(sourceId, slot);
            }
            reference.DataSourceId = null;
            reference.SourceSlot = slot;
        }
        return await ValidateDashboardAsync(Serialize(definition), destinationClientId, cancellationToken);
    }

    private static DashboardDefinitionV1 ParseAndValidate(string json, bool template)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        if (Encoding.UTF8.GetByteCount(json) > MaxDocumentBytes)
            throw new ArgumentException($"Dashboard definition cannot exceed {MaxDocumentBytes} UTF-8 bytes.", nameof(json));

        DashboardDefinitionV1 definition;
        try
        {
            definition = JsonSerializer.Deserialize<DashboardDefinitionV1>(json, JsonOptions)
                ?? throw new ArgumentException("Dashboard definition must be a JSON object.", nameof(json));
        }
        catch (JsonException exception)
        {
            throw new ArgumentException($"Dashboard definition is invalid JSON: {exception.Message}", nameof(json));
        }

        if (definition.ExtensionData is { Count: > 0 })
            throw new ArgumentException($"Unknown dashboard property '{definition.ExtensionData.Keys.First()}'.");
        if (definition.SchemaVersion != CurrentSchemaVersion)
            throw new ArgumentException($"Dashboard schemaVersion must be {CurrentSchemaVersion}.");
        if (definition.Pages is null || definition.Pages.Count is < 1 or > MaxPages)
            throw new ArgumentException($"Dashboard must contain between 1 and {MaxPages} pages.");
        if (definition.Theme.ValueKind is not (JsonValueKind.Undefined or JsonValueKind.Null))
            ValidateFreeForm(definition.Theme, "theme");

        var pageIds = new HashSet<Guid>();
        var pageOrders = new HashSet<int>();
        var componentIds = new HashSet<Guid>();
        var totalComponents = 0;
        foreach (var page in definition.Pages)
        {
            if (page is null) throw new ArgumentException("Dashboard pages cannot contain null values.");
            if (page.ExtensionData is { Count: > 0 })
                throw new ArgumentException($"Unknown page property '{page.ExtensionData.Keys.First()}'.");
            if (page.Id == Guid.Empty || !pageIds.Add(page.Id))
                throw new ArgumentException("Every page must have a unique non-empty id.");
            ValidateText(page.Name, MaxPageNameLength, "Page name");
            if (page.Order < 0 || !pageOrders.Add(page.Order))
                throw new ArgumentException("Every page must have a unique non-negative order.");
            if (page.Width is < MinCanvasWidth or > MaxCanvasWidth
                || page.Height is < MinCanvasHeight or > MaxCanvasHeight)
                throw new ArgumentException("Page canvas size is outside the supported range.");
            if (page.Background is { Length: > 100 })
                throw new ArgumentException("Page background cannot exceed 100 characters.");
            if (page.Components is null)
                throw new ArgumentException("Page components must be an array.");
            if (page.Components.Count > MaxComponentsPerPage)
                throw new ArgumentException($"A page cannot contain more than {MaxComponentsPerPage} components.");
            totalComponents += page.Components.Count;
            if (totalComponents > MaxTotalComponents)
                throw new ArgumentException($"A dashboard cannot contain more than {MaxTotalComponents} components.");

            foreach (var component in page.Components)
            {
                if (component is null) throw new ArgumentException("Page components cannot contain null values.");
                ValidateComponent(component, page, componentIds, template);
            }
        }

        return definition;
    }

    private static void ValidateComponent(DashboardComponentV1 component, DashboardPageV1 page,
        HashSet<Guid> componentIds, bool template)
    {
        if (component.ExtensionData is { Count: > 0 })
            throw new ArgumentException($"Unknown component property '{component.ExtensionData.Keys.First()}'.");
        if (component.Id == Guid.Empty || !componentIds.Add(component.Id))
            throw new ArgumentException("Every component must have a unique non-empty id.");
        if (!Enum.IsDefined(component.Type)) throw new ArgumentException("Component type is not supported.");
        if (component.X < 0 || component.Y < 0 || component.Width <= 0 || component.Height <= 0
            || (long)component.X + component.Width > page.Width
            || (long)component.Y + component.Height > page.Height)
            throw new ArgumentException($"Component '{component.Id}' is outside its page bounds.");
        if (component.ZIndex is < 0 or > MaxZIndex)
            throw new ArgumentException($"Component '{component.Id}' has an invalid zIndex.");
        if (component.GroupId == Guid.Empty)
            throw new ArgumentException($"Component '{component.Id}' has an empty groupId.");
        if (component.Data is null)
            throw new ArgumentException($"Component '{component.Id}' data is required.");
        if (component.Data.ExtensionData is { Count: > 0 })
            throw new ArgumentException($"Unknown component data property '{component.Data.ExtensionData.Keys.First()}'.");
        if (component.Style.ValueKind is not (JsonValueKind.Undefined or JsonValueKind.Null))
            ValidateFreeForm(component.Style, $"component {component.Id} style");
        if (component.Data.Configuration.ValueKind is not (JsonValueKind.Undefined or JsonValueKind.Null))
            ValidateFreeForm(component.Data.Configuration, $"component {component.Id} configuration");

        if (component.Data.Sources is null)
            throw new ArgumentException($"Component '{component.Id}' sources must be an array.");
        foreach (var source in component.Data.Sources)
        {
            if (source is null) throw new ArgumentException("Source references cannot contain null values.");
            if (source.ExtensionData is { Count: > 0 })
                throw new ArgumentException($"Unknown source property '{source.ExtensionData.Keys.First()}'.");
            var hasId = source.DataSourceId is not null;
            var hasSlot = !string.IsNullOrWhiteSpace(source.SourceSlot);
            if (hasId == hasSlot)
                throw new ArgumentException("Each source reference must contain either dataSourceId or sourceSlot.");
            if (source.DataSourceId == Guid.Empty)
                throw new ArgumentException("Data source id cannot be empty.");
            if (template && hasId)
                throw new ArgumentException("Templates must use sourceSlot instead of client dataSourceId values.");
            if (hasSlot)
            {
                source.SourceSlot = source.SourceSlot!.Trim();
                ValidateSlot(source.SourceSlot);
            }
            if (source.Alias is not null) ValidateText(source.Alias, MaxSourceSlotLength, "Source alias");
        }
    }

    private static IEnumerable<DashboardSourceReferenceV1> SourceReferences(DashboardDefinitionV1 definition) =>
        definition.Pages.SelectMany(page => page.Components)
            .SelectMany(component => component.Data.Sources);

    private static void ValidateSlot(string slot)
    {
        ValidateText(slot, MaxSourceSlotLength, "Source slot");
        if (slot.Any(character => !(char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.')))
            throw new ArgumentException("Source slots may contain only letters, numbers, '.', '-' and '_'.");
    }

    private static void ValidateText(string value, int maxLength, string field)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > maxLength)
            throw new ArgumentException($"{field} is required and cannot exceed {maxLength} characters.");
    }

    private static void ValidateFreeForm(JsonElement element, string path)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    if (ForbiddenFreeFormKeys.Contains(property.Name))
                        throw new ArgumentException($"'{property.Name}' is not allowed inside {path}.");
                    ValidateFreeForm(property.Value, path);
                }
                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray()) ValidateFreeForm(item, path);
                break;
        }
    }

    private static string Serialize(DashboardDefinitionV1 definition) =>
        JsonSerializer.Serialize(definition, JsonOptions);

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            MaxDepth = 64
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
        return options;
    }
}
