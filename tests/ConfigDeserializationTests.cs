using FluentAssertions;
using Xunit;

namespace PepperDash.Essentials.Plugins.Crestron.IpTable_Editor.Tests;

public class ConfigDeserializationTests
{
    private static Type GetConfigType(string name) =>
        AssemblyFixture.PluginAssembly.GetTypes().Single(t => t.Name == name);

    [Theory]
    [InlineData("IpTableEditorConfigObject")]
    [InlineData("IpTableObjectBase")]
    [InlineData("IpTableChangesConfigObject")]
    public void Config_Class_Exists(string className)
    {
        AssemblyFixture.PluginAssembly.GetTypes()
            .Should().Contain(t => t.Name == className);
    }

    [Theory]
    [InlineData("IpTableEditorConfigObject")]
    [InlineData("IpTableChangesConfigObject")]
    public void Config_Has_Parameterless_Constructor(string className)
    {
        var type = GetConfigType(className);
        type.GetConstructor(Type.EmptyTypes).Should().NotBeNull();
    }

    [Theory]
    [InlineData("IpTableEditorConfigObject", "IpTableChanges", "ipTableChanges")]
    [InlineData("IpTableEditorConfigObject", "RunAtStartup", "runAtStartup")]
    [InlineData("IpTableEditorConfigObject", "Control", "control")]
    [InlineData("IpTableEditorConfigObject", "PersistentEntry", "persistentEntry")]
    [InlineData("IpTableEditorConfigObject", "SelectableEntries", "selectableEntries")]
    [InlineData("IpTableObjectBase", "IpId", "ipId")]
    [InlineData("IpTableObjectBase", "IpAddress", "ipAddress")]
    [InlineData("IpTableObjectBase", "RoomId", "roomId")]
    [InlineData("IpTableChangesConfigObject", "Name", "name")]
    [InlineData("IpTableChangesConfigObject", "DevId", "devId")]
    [InlineData("IpTableChangesConfigObject", "IpPort", "ipPort")]
    [InlineData("IpTableChangesConfigObject", "ProgramNumber", "programNumber")]
    public void Config_Property_Has_JsonPropertyAttribute(string className, string propertyName, string jsonName)
    {
        var type = GetConfigType(className);
        var property = type.GetProperty(propertyName);
        property.Should().NotBeNull($"{className} should declare property {propertyName}");

        var hasAttribute = property!.CustomAttributes.Any(a =>
            a.AttributeType.Name == "JsonPropertyAttribute"
            && a.ConstructorArguments.Any(arg =>
                string.Equals(arg.Value?.ToString(), jsonName, StringComparison.Ordinal)));

        hasAttribute.Should().BeTrue(
            $"{className}.{propertyName} should have [JsonProperty(\"{jsonName}\")]");
    }
}
