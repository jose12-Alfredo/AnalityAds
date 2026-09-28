using AnaliticAsd.Domain.Dashboards;

namespace AnaliticAsd.Tests.Domain;

public sealed class DashboardTests
{
    [Fact]
    public void Dashboard_tracks_metadata_archive_and_publication_versions()
    {
        var now = DateTimeOffset.UtcNow;
        var dashboard = Dashboard.Create(Guid.NewGuid(), Guid.NewGuid(), null, " Informe ", " Resumen ",
            Guid.NewGuid(), now);
        var initialVersion = dashboard.Version;

        Assert.Equal("Informe", dashboard.Title);
        Assert.Equal("Resumen", dashboard.Description);
        Assert.Equal(1, dashboard.RegisterPublication(now.AddMinutes(1)));
        Assert.NotEqual(initialVersion, dashboard.Version);
        Assert.Equal(2, dashboard.RegisterPublication(now.AddMinutes(2)));

        dashboard.Archive(now.AddMinutes(3));
        Assert.True(dashboard.IsArchived);
        Assert.Throws<InvalidOperationException>(() => dashboard.UpdateDetails("Blocked", null, now));
        dashboard.Restore(now.AddMinutes(4));
        dashboard.UpdateDetails("Restored", null, now.AddMinutes(5));
        Assert.Equal("Restored", dashboard.Title);
    }

    [Fact]
    public void Draft_requires_the_current_revision_and_increments_it_on_save()
    {
        var now = DateTimeOffset.UtcNow;
        var draft = DashboardDraft.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1,
            "{\"schemaVersion\":1}", Guid.NewGuid(), now);

        draft.Replace(1, 1, "{\"schemaVersion\":1,\"pages\":[]}", Guid.NewGuid(), now.AddMinutes(1));

        Assert.Equal(2, draft.Revision);
        Assert.Throws<InvalidOperationException>(() => draft.Replace(1, 1, "{}", Guid.NewGuid(), now));
    }

    [Fact]
    public void Published_version_captures_definition_and_sha256_hash()
    {
        const string json = "{\"schemaVersion\":1,\"pages\":[]}";
        var version = DashboardVersion.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, 3, 1,
            json, Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.Equal(1, version.PublicationNumber);
        Assert.Equal(3, version.DraftRevision);
        Assert.Equal(json, version.DefinitionJson);
        Assert.Equal(64, version.DefinitionHash.Length);
        Assert.Equal(version.DefinitionHash, DashboardVersion.Create(version.DashboardId, version.AgencyId,
            version.ClientId, 2, 3, 1, json, version.PublishedByUserId, DateTimeOffset.UtcNow).DefinitionHash);
    }
}
