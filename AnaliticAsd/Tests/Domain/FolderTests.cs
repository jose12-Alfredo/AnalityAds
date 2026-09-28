using AnaliticAsd.Domain.Folders;

namespace AnaliticAsd.Tests.Domain;

public sealed class FolderTests
{
    [Fact]
    public void Folder_normalizes_name_and_updates_its_concurrency_version()
    {
        var folder = Folder.Create(Guid.NewGuid(), Guid.NewGuid(), null, " Root ", 0, DateTimeOffset.UtcNow);
        var initialVersion = folder.Version;

        folder.Rename(" Renamed ", DateTimeOffset.UtcNow.AddMinutes(1));

        Assert.Equal("Renamed", folder.Name);
        Assert.NotEqual(initialVersion, folder.Version);
    }

    [Fact]
    public void Folder_cannot_be_its_own_parent_or_use_a_negative_order()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Folder.Create(Guid.NewGuid(), Guid.NewGuid(), null,
            "Folder", -1, DateTimeOffset.UtcNow));
        var folder = Folder.Create(Guid.NewGuid(), Guid.NewGuid(), null, "Folder", 0, DateTimeOffset.UtcNow);
        Assert.Throws<ArgumentException>(() => folder.Move(folder.Id, 0, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Archived_folder_must_be_restored_before_editing()
    {
        var folder = Folder.Create(Guid.NewGuid(), Guid.NewGuid(), null, "Folder", 0, DateTimeOffset.UtcNow);
        folder.Archive(DateTimeOffset.UtcNow.AddMinutes(1));

        Assert.True(folder.IsArchived);
        Assert.Throws<InvalidOperationException>(() => folder.Rename("Blocked", DateTimeOffset.UtcNow));
        folder.Restore(DateTimeOffset.UtcNow.AddMinutes(2));
        folder.Rename("Allowed", DateTimeOffset.UtcNow.AddMinutes(3));
        Assert.Equal("Allowed", folder.Name);
    }
}
