using Mono.Cecil;
using Xunit;

namespace AnimCancel.Tests;

/// <summary>
/// One guard per game member the plugin binds to. Each was verified against the
/// decompiled assembly_valheim.dll. If one goes red after a game update, the plugin
/// needs attention at that exact member. Private members are not asserted public
/// because the plugin reads them through the publicizer.
/// </summary>
public class GameApiGuardTests
{
    // ----- instance fields: declaring type, name, field type full name -----

    [Theory]
    [InlineData("Humanoid", "m_currentAttack", "Attack")]
    [InlineData("Humanoid", "m_currentAttackIsSecondary", "System.Boolean")]
    [InlineData("Attack", "m_attackAnimation", "System.String")]
    [InlineData("Attack", "m_attackChainLevels", "System.Int32")]
    [InlineData("Attack", "m_currentAttackCainLevel", "System.Int32")] // game's own spelling
    [InlineData("Character", "m_animator", "UnityEngine.Animator")]
    [InlineData("Character", "m_animEvent", "CharacterAnimEvent")]
    [InlineData("CharacterAnimEvent", "m_pauseTimer", "System.Single")]
    [InlineData("CharacterAnimEvent", "m_pauseSpeed", "System.Single")]
    [InlineData("Character", "m_attackHold", "System.Boolean")]
    [InlineData("Character", "m_secondaryAttackHold", "System.Boolean")]
    public void Instance_field_exists_with_expected_type(string typeName, string fieldName, string fieldType)
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        FieldDefinition field = GameAssembly.Field(typeName, fieldName);

        Assert.False(field.IsStatic);
        Assert.Equal(fieldType, field.FieldType.FullName);
    }

    [Fact]
    public void Player_m_localPlayer_is_a_static_Player_field()
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        FieldDefinition field = GameAssembly.Field("Player", "m_localPlayer");

        Assert.True(field.IsStatic);
        Assert.Equal("Player", field.FieldType.FullName);
    }

    // ----- methods -----------------------------------------------------------

    [Theory]
    [InlineData("Humanoid", "OnAttackTrigger")]
    [InlineData("Player", "InAttack")]
    public void Method_is_an_override_declared_on_the_type(string typeName, string methodName)
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        MethodDefinition method = GameAssembly.Method(typeName, methodName);

        Assert.Equal(typeName, method.DeclaringType.Name);
        Assert.False(method.IsStatic);
        Assert.True(method.IsVirtual);
        Assert.False(method.IsNewSlot, $"{typeName}.{methodName} no longer overrides a base method.");
    }

    [Fact]
    public void Character_InEmote_is_virtual()
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        MethodDefinition method = GameAssembly.Method("Character", "InEmote");

        Assert.False(method.IsStatic);
        Assert.True(method.IsVirtual);
        Assert.Equal("System.Boolean", method.ReturnType.FullName);
    }

    [Fact]
    public void Player_OnDodgeMortal_is_public_instance()
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        MethodDefinition method = GameAssembly.Method("Player", "OnDodgeMortal");

        Assert.True(method.IsPublic);
        Assert.False(method.IsStatic);
    }
}
