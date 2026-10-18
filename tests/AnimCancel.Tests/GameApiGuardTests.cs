using System.Linq;
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
    [InlineData("Attack", "m_damageMultiplier", "System.Single")]          // combo lab: swing multiplier
    [InlineData("Attack", "m_lastChainDamageMultiplier", "System.Single")]
    [InlineData("Character", "m_animator", "UnityEngine.Animator")]
    [InlineData("Character", "m_animEvent", "CharacterAnimEvent")]
    [InlineData("CharacterAnimEvent", "m_pauseTimer", "System.Single")]
    [InlineData("CharacterAnimEvent", "m_pauseSpeed", "System.Single")]
    [InlineData("Character", "m_attackHold", "System.Boolean")]
    [InlineData("Character", "m_secondaryAttackHold", "System.Boolean")]
    [InlineData("CharacterAnimEvent", "m_character", "Character")]
    [InlineData("CharacterAnimEvent", "m_animator", "UnityEngine.Animator")]
    [InlineData("Character", "m_zanim", "ZSyncAnimation")]          // combo lab: trigger owner check
    [InlineData("Character", "m_name", "System.String")]            // combo lab: hit target
    [InlineData("Player", "m_cachedFrame", "System.Int32")]         // combo lab: InAttack cache, read only
    [InlineData("Player", "m_cachedAttack", "System.Boolean")]
    [InlineData("Humanoid", "m_timeSinceLastAttack", "System.Single")] // combo lab: chain reset > 0.2 s
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
    public void Humanoid_StartAttack_is_a_bool_override_taking_Character_and_bool()
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        MethodDefinition method = GameAssembly.Method("Humanoid", "StartAttack", "Character", "Boolean");

        Assert.True(method.IsPublic);
        Assert.True(method.IsVirtual);
        Assert.False(method.IsNewSlot, "Humanoid.StartAttack no longer overrides Character.StartAttack.");
        Assert.Equal("System.Boolean", method.ReturnType.FullName);
    }

    [Fact]
    public void Player_does_not_override_StartAttack_or_OnAttackTrigger()
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        // The patches sit on Humanoid; a Player override would bypass them.
        Assert.DoesNotContain(GameAssembly.Type("Player").Methods,
            m => m.Name == "StartAttack" || m.Name == "OnAttackTrigger");
    }

    [Fact]
    public void Character_IsStaggering_is_a_public_instance_bool()
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        MethodDefinition method = GameAssembly.Method("Character", "IsStaggering");

        Assert.True(method.IsPublic);
        Assert.False(method.IsStatic);
        Assert.Equal("System.Boolean", method.ReturnType.FullName);
    }

    [Fact]
    public void Humanoid_GetCurrentWeapon_is_public_instance()
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        MethodDefinition method = GameAssembly.Method("Humanoid", "GetCurrentWeapon");

        Assert.True(method.IsPublic);
        Assert.False(method.IsStatic);
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

    [Fact]
    public void Player_SetControls_is_public_with_the_expected_hold_parameters()
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        MethodDefinition method = GameAssembly.Type("Player").Methods.First(m => m.Name == "SetControls");

        Assert.True(method.IsPublic);
        Assert.False(method.IsStatic);
        Assert.Equal("System.Void", method.ReturnType.FullName);
        Assert.Equal("attackHold", method.Parameters[2].Name);
        Assert.Equal("System.Boolean", method.Parameters[2].ParameterType.FullName);
        Assert.Equal("secondaryAttackHold", method.Parameters[4].Name);
        Assert.Equal("System.Boolean", method.Parameters[4].ParameterType.FullName);
    }

    // ----- MessageHud (toggle feedback) --------------------------------------

    [Fact]
    public void MessageHud_instance_is_a_public_static_getter()
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        PropertyDefinition property = GameAssembly.Type("MessageHud").Properties.First(p => p.Name == "instance");

        Assert.NotNull(property.GetMethod);
        Assert.True(property.GetMethod.IsStatic);
        Assert.True(property.GetMethod.IsPublic);
        Assert.Equal("MessageHud", property.PropertyType.FullName);
    }

    [Fact]
    public void MessageHud_ShowMessage_takes_MessageType_and_string_first()
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        MethodDefinition method = GameAssembly.Type("MessageHud").Methods
            .First(m => m.Name == "ShowMessage" && m.Parameters.Count >= 2);

        Assert.True(method.IsPublic);
        Assert.False(method.IsStatic);
        Assert.Equal("MessageType", method.Parameters[0].ParameterType.Name);
        Assert.Equal("System.String", method.Parameters[1].ParameterType.FullName);
        Assert.All(method.Parameters.Skip(2), p => Assert.True(p.IsOptional, $"{p.Name} is no longer optional."));
    }

    [Fact]
    public void MessageHud_MessageType_has_TopLeft()
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        Assert.Contains(GameAssembly.NestedType("MessageHud", "MessageType").Fields, f => f.Name == "TopLeft");
    }

    // ----- InputGate (toggle key must not fire while typing) -----------------

    [Theory]
    [InlineData("Console")]
    [InlineData("TextInput")]
    [InlineData("Menu")]
    [InlineData("InventoryGui")]
    public void IsVisible_is_public_and_static(string typeName)
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        MethodDefinition method = GameAssembly.Method(typeName, "IsVisible");

        Assert.True(method.IsPublic);
        Assert.True(method.IsStatic);
    }

    [Fact]
    public void Chat_instance_is_a_public_static_property()
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        MethodDefinition getter = GameAssembly.Method("Chat", "get_instance");

        Assert.True(getter.IsPublic);
        Assert.True(getter.IsStatic);
        Assert.Equal("Chat", getter.ReturnType.Name);
    }

    [Fact]
    public void Chat_HasFocus_is_public()
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        Assert.True(GameAssembly.Method("Chat", "HasFocus").IsPublic);
    }

    [Fact]
    public void Player_m_localPlayerExists_is_a_public_static_field()
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        FieldDefinition field = GameAssembly.Field("Player", "m_localPlayerExists");

        Assert.True(field.IsPublic);
        Assert.True(field.IsStatic);
    }

    [Fact]
    public void CharacterAnimEvent_Speed_is_a_public_void_taking_one_float()
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        // The Speed animation event the prefix skips; it only writes m_animator.speed.
        MethodDefinition method = GameAssembly.Method("CharacterAnimEvent", "Speed", "Single");

        Assert.True(method.IsPublic);
        Assert.False(method.IsStatic);
        Assert.Equal("System.Void", method.ReturnType.FullName);
        Assert.Equal("speedScale", method.Parameters[0].Name);
    }

    // ----- Combo lab log (measurement only) ---------------------------------

    [Fact]
    public void MonoUpdaters_UpdateCount_is_a_public_static_int()
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        // Player.InAttack caches on this counter; the lab logs it as ff.
        PropertyDefinition property = GameAssembly.Type("MonoUpdaters").Properties.First(p => p.Name == "UpdateCount");

        Assert.True(property.GetMethod.IsStatic);
        Assert.True(property.GetMethod.IsPublic);
        Assert.Equal("System.Int32", property.PropertyType.FullName);
    }

    [Fact]
    public void Player_InAttack_still_caches_on_m_cachedFrame_and_MonoUpdaters_UpdateCount()
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        // ComboLab.InAttackNoCache mirrors this cache; if the game drops it the mirror is wrong.
        MethodDefinition method = GameAssembly.Method("Player", "InAttack");
        string[] operands = method.Body.Instructions.Select(i => i.Operand?.ToString() ?? "").ToArray();

        Assert.Contains(operands, o => o.Contains("MonoUpdaters::get_UpdateCount"));
        Assert.Contains(operands, o => o.Contains("Player::m_cachedFrame"));
        Assert.Contains(operands, o => o.Contains("Player::m_cachedAttack"));
    }

    [Fact]
    public void ZSyncAnimation_SetTrigger_is_a_single_public_void_taking_string_name()
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        Assert.Single(GameAssembly.Type("ZSyncAnimation").Methods, m => m.Name == "SetTrigger");
        MethodDefinition method = GameAssembly.Method("ZSyncAnimation", "SetTrigger", "String");

        Assert.True(method.IsPublic);
        Assert.False(method.IsStatic);
        Assert.Equal("System.Void", method.ReturnType.FullName);
        Assert.Equal("name", method.Parameters[0].Name);
    }

    [Fact]
    public void Humanoid_IsBlocking_is_a_bool_override_and_Player_does_not_override_it()
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        MethodDefinition method = GameAssembly.Method("Humanoid", "IsBlocking");

        Assert.True(method.IsPublic);
        Assert.True(method.IsVirtual);
        Assert.False(method.IsNewSlot, "Humanoid.IsBlocking no longer overrides Character.IsBlocking.");
        Assert.Equal("System.Boolean", method.ReturnType.FullName);
        Assert.DoesNotContain(GameAssembly.Type("Player").Methods, m => m.Name == "IsBlocking");
    }

    [Fact]
    public void Character_Damage_is_a_public_void_taking_HitData_hit()
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        MethodDefinition method = GameAssembly.Method("Character", "Damage", "HitData");

        Assert.True(method.IsPublic);
        Assert.False(method.IsStatic);
        Assert.Equal("System.Void", method.ReturnType.FullName);
        Assert.Equal("hit", method.Parameters[0].Name);
        Assert.DoesNotContain(GameAssembly.Type("Player").Methods, m => m.Name == "Damage");
        Assert.DoesNotContain(GameAssembly.Type("Humanoid").Methods, m => m.Name == "Damage");
    }

    [Theory]
    [InlineData("GetAttacker", "Character")]
    [InlineData("GetTotalDamage", "System.Single")]
    public void HitData_getter_is_public_instance(string methodName, string returnType)
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        MethodDefinition method = GameAssembly.Method("HitData", methodName);

        Assert.True(method.IsPublic);
        Assert.False(method.IsStatic);
        Assert.Equal(returnType, method.ReturnType.FullName);
    }

    [Fact]
    public void Player_HaveQueuedChain_is_a_bool_override()
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        MethodDefinition method = GameAssembly.Method("Player", "HaveQueuedChain");

        Assert.True(method.IsVirtual);
        Assert.False(method.IsNewSlot, "Player.HaveQueuedChain no longer overrides Humanoid.HaveQueuedChain.");
        Assert.Equal("System.Boolean", method.ReturnType.FullName);
    }

    [Theory]
    [InlineData(1, "attack")]
    [InlineData(2, "attackHold")]
    [InlineData(3, "secondaryAttack")]
    [InlineData(4, "secondaryAttackHold")]
    [InlineData(5, "block")]
    [InlineData(6, "blockHold")]
    public void Player_SetControls_button_parameters_keep_their_names(int index, string name)
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        // Harmony binds the lab prefix arguments by name.
        MethodDefinition method = GameAssembly.Type("Player").Methods.Single(m => m.Name == "SetControls");

        Assert.Equal(name, method.Parameters[index].Name);
        Assert.Equal("System.Boolean", method.Parameters[index].ParameterType.FullName);
    }

    [Fact]
    public void Humanoid_StartAttack_second_parameter_is_secondaryAttack()
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        MethodDefinition method = GameAssembly.Method("Humanoid", "StartAttack", "Character", "Boolean");

        Assert.Equal("secondaryAttack", method.Parameters[1].Name);
    }
}
