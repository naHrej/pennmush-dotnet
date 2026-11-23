namespace PennMUSH.Core.Tests.Database;

using PennMUSH.Core.Attributes;
using PennMUSH.Core.Database;
using PennMUSH.Core.Types;

public class AttributeCacheTests
{
    [Fact]
    public void InheritedAttributesFollowParentEditsAndParentListChanges()
    {
        var db = new ObjectDatabase();
        var owner = new Dbref(1);
        var parent = db.CreateObject("Parent", ObjectType.Thing, owner);
        var other = db.CreateObject("Other", ObjectType.Thing, owner);
        var child = db.CreateObject("Child", ObjectType.Thing, owner);
        var grand = db.CreateObject("Grand", ObjectType.Thing, owner);
        parent.SetAttribute("DESC", "from parent", owner);
        other.SetAttribute("COLOR", "red", owner);
        child.AddParent(parent.Id, db);
        grand.AddParent(child.Id, db);

        Assert.Equal("from parent", child.GetAttribute("DESC", db)!.Value);
        Assert.Equal("from parent", grand.GetAttribute("desc", db)!.Value);
        Assert.NotNull(child.GetAttributeWithSource("DESC", db, out var source));
        Assert.Equal(parent.Id, source!.Id);

        parent.SetAttribute("DESC", "changed", owner);
        Assert.Equal("changed", child.GetAttribute("DESC", db)!.Value);
        Assert.Equal("changed", grand.GetAttribute("DESC", db)!.Value);

        child.SetAttribute("DESC", "mine", owner);
        Assert.Equal("mine", child.GetAttribute("DESC", db)!.Value);
        Assert.Equal("mine", grand.GetAttribute("DESC", db)!.Value);
        child.ClearAttribute("DESC");
        Assert.Equal("changed", child.GetAttribute("DESC", db)!.Value);

        parent.ClearAttribute("DESC");
        Assert.Null(child.GetAttribute("DESC", db));
        Assert.Null(grand.GetAttribute("DESC", db));

        Assert.Null(child.GetAttribute("COLOR", db));
        child.AddParent(other.Id, db);
        Assert.Equal("red", child.GetAttribute("COLOR", db)!.Value);
        child.RemoveParent(other.Id);
        Assert.Null(child.GetAttribute("COLOR", db));
    }

    [Fact]
    public void PrivateParentAttributesStayHiddenAfterACachedMiss()
    {
        var db = new ObjectDatabase();
        var owner = new Dbref(1);
        var parent = db.CreateObject("Parent", ObjectType.Thing, owner);
        var child = db.CreateObject("Child", ObjectType.Thing, owner);
        parent.SetAttribute("SECRET", "nope", owner, AttributeFlags.Private);
        child.AddParent(parent.Id, db);

        Assert.Null(child.GetAttribute("SECRET", db));
        parent.SetAttribute("SECRET", "visible", owner);
        Assert.Equal("visible", child.GetAttribute("SECRET", db)!.Value);
        parent.AddAttributeFlags("SECRET", AttributeFlags.Private);
        Assert.Null(child.GetAttribute("SECRET", db));
    }
}
