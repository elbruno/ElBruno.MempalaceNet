using FluentAssertions;
using MemPalace.Core.Model;

namespace MemPalace.Tests.Model;

public sealed class WingRoomDrawerTests
{
    [Fact]
    public void Wing_WhenCreated_WithNameOnly_ExpectCorrectProperties()
    {
        var wing = new Wing("engineering");
        wing.Name.Should().Be("engineering");
        wing.Description.Should().BeNull();
    }

    [Fact]
    public void Wing_WhenCreated_WithNameAndDescription_ExpectCorrectProperties()
    {
        var wing = new Wing("engineering", "Engineering team workspace");
        wing.Name.Should().Be("engineering");
        wing.Description.Should().Be("Engineering team workspace");
    }

    [Fact]
    public void Wing_EqualityByValue_ExpectSamePropertiesMeansEqual()
    {
        var wing1 = new Wing("engineering", "Engineering workspace");
        var wing2 = new Wing("engineering", "Engineering workspace");
        wing1.Should().Be(wing2);
        (wing1 == wing2).Should().BeTrue();
        wing1.GetHashCode().Should().Be(wing2.GetHashCode());
    }

    [Fact]
    public void Room_WhenCreated_WithRequiredProperties_ExpectCorrectValues()
    {
        var room = new Room("planning", "engineering");
        room.Name.Should().Be("planning");
        room.Wing.Should().Be("engineering");
        room.Topic.Should().BeNull();
    }

    [Fact]
    public void Room_EqualityByValue_ExpectSamePropertiesMeansEqual()
    {
        var room1 = new Room("planning", "engineering", "Sprint planning");
        var room2 = new Room("planning", "engineering", "Sprint planning");
        room1.Should().Be(room2);
        (room1 == room2).Should().BeTrue();
    }

    [Fact]
    public void Drawer_WhenCreated_WithAllProperties_ExpectCorrectValues()
    {
        var timestamp = DateTimeOffset.UtcNow;
        var metadata = new Dictionary<string, object?> { { "source", "test" } };
        var drawer = new Drawer("drawer-1", "planning", "engineering", "Test content", metadata, timestamp);
        
        drawer.Id.Should().Be("drawer-1");
        drawer.Room.Should().Be("planning");
        drawer.Wing.Should().Be("engineering");
        drawer.Content.Should().Be("Test content");
        drawer.Metadata.Should().HaveCount(1);
    }

    [Fact]
    public void PalaceRef_WhenCreated_WithIdOnly_ExpectCorrectProperties()
    {
        var palaceRef = new PalaceRef("my-palace");
        palaceRef.Id.Should().Be("my-palace");
        palaceRef.LocalPath.Should().BeNull();
        palaceRef.Namespace.Should().BeNull();
    }

    [Fact]
    public void PalaceRef_EqualityByValue_ExpectSamePropertiesMeansEqual()
    {
        var ref1 = new PalaceRef("palace", "/path", "ns");
        var ref2 = new PalaceRef("palace", "/path", "ns");
        ref1.Should().Be(ref2);
        (ref1 == ref2).Should().BeTrue();
    }
}
