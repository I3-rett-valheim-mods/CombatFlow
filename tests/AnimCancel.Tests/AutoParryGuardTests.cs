using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Xunit;

namespace AnimCancel.Tests;

/// <summary>
/// Guards for Combat / Auto perfect parry (AutoParryPatch), checked against the decompiled
/// Character.RPC_Damage, Humanoid.BlockAttack / IsBlocking / UpdateBlock. Private members are read
/// through the publicizer, so only their shape is asserted.
/// </summary>
public class AutoParryGuardTests
{
    private static string[] Operands(MethodDefinition m) =>
        m.Body.Instructions.Select(i => i.Operand?.ToString() ?? "").ToArray();

    [Fact]
    public void Character_RPC_Damage_is_a_void_taking_long_and_HitData_hit_and_not_overridden()
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        MethodDefinition method = GameAssembly.Method("Character", "RPC_Damage", "Int64", "HitData");

        Assert.False(method.IsStatic);
        Assert.Equal("System.Void", method.ReturnType.FullName);
        Assert.Equal("hit", method.Parameters[1].Name);
        Assert.Single(GameAssembly.Type("Character").Methods, m => m.Name == "RPC_Damage");
        Assert.DoesNotContain(GameAssembly.Type("Humanoid").Methods, m => m.Name == "RPC_Damage");
        Assert.DoesNotContain(GameAssembly.Type("Player").Methods, m => m.Name == "RPC_Damage");
    }

    [Fact]
    public void RPC_Damage_still_blocks_through_IsBlocking_then_BlockAttack_after_the_dodge_return()
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        string[] ops = Operands(GameAssembly.Method("Character", "RPC_Damage", "Int64", "HitData"));
        int dodge = System.Array.FindIndex(ops, o => o.Contains("Character::IsDodgeInvincible"));
        int blockable = System.Array.FindIndex(ops, o => o.Contains("HitData::m_blockable"));
        int isBlocking = System.Array.FindIndex(ops, o => o.Contains("Character::IsBlocking"));
        int block = System.Array.FindIndex(ops, o => o.Contains("Character::BlockAttack"));

        Assert.True(dodge >= 0 && blockable > dodge && isBlocking > blockable && block > isBlocking,
            $"order changed: dodge={dodge} blockable={blockable} isBlocking={isBlocking} block={block}");
        // The forced value must reach only the block check: one IsBlocking call in RPC_Damage.
        Assert.Single(ops, o => o.Contains("::IsBlocking"));
    }

    [Fact]
    public void Humanoid_BlockAttack_overrides_and_keeps_the_direction_blocker_and_perfect_window_rules()
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        MethodDefinition method = GameAssembly.Method("Humanoid", "BlockAttack", "HitData", "Character");
        Assert.True(method.IsVirtual);
        Assert.False(method.IsNewSlot, "Humanoid.BlockAttack no longer overrides Character.BlockAttack.");
        Assert.DoesNotContain(GameAssembly.Type("Player").Methods, m => m.Name == "BlockAttack");

        Instruction[] ins = method.Body.Instructions.ToArray();
        string[] ops = Operands(method);
        Assert.Contains(ops, o => o.Contains("Vector3::Dot"));
        Assert.Contains(ops, o => o.Contains("Humanoid::GetCurrentBlocker"));
        Assert.Contains(ops, o => o.Contains("Humanoid::m_blockTimer"));
        Assert.Contains(ops, o => o.Contains("SharedData::m_timedBlockBonus"));
        Assert.Contains(ops, o => o.Contains("Character::m_staggerWhenBlocked"));
        Assert.Contains(ins, i => i.OpCode == OpCodes.Ldc_R4 && (float)i.Operand == 0.25f);
    }

    [Fact]
    public void Humanoid_m_blockTimer_is_an_instance_float_written_only_by_UpdateBlock()
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        FieldDefinition field = GameAssembly.Field("Humanoid", "m_blockTimer");
        Assert.False(field.IsStatic);
        Assert.Equal("System.Single", field.FieldType.FullName);

        var writers = GameAssembly.Type("Humanoid").Methods.Concat(GameAssembly.Type("Player").Methods)
            .Concat(GameAssembly.Type("Character").Methods)
            .Where(m => m.HasBody && m.Body.Instructions.Any(i =>
                i.OpCode == OpCodes.Stfld && (i.Operand?.ToString() ?? "").Contains("Humanoid::m_blockTimer")))
            .Select(m => m.Name)
            .Distinct()
            .ToArray();
        Assert.Equal(new[] { "UpdateBlock" }, writers.Where(n => n != ".ctor").ToArray());
    }

    [Fact]
    public void Humanoid_GetCurrentBlocker_returns_ItemData_without_parameters()
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        MethodDefinition method = GameAssembly.Method("Humanoid", "GetCurrentBlocker");
        Assert.False(method.IsStatic);
        Assert.Equal("ItemData", method.ReturnType.Name);
    }

    [Theory]
    [InlineData("Player", "InDodge")]
    [InlineData("Player", "IsDodgeInvincible")]
    [InlineData("Player", "InPlaceMode")]
    [InlineData("Player", "IsEncumbered")]
    [InlineData("Player", "InMinorAction")]
    public void Dodge_queries_are_Player_overrides(string typeName, string methodName)
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        MethodDefinition method = GameAssembly.Method(typeName, methodName);
        Assert.True(method.IsPublic);
        Assert.True(method.IsVirtual);
        Assert.False(method.IsNewSlot);
        Assert.Equal("System.Boolean", method.ReturnType.FullName);
    }

    [Fact]
    public void Read_only_queries_exist()
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        Assert.Equal("System.Single", GameAssembly.Method("Character", "GetStaggerPercentage").ReturnType.FullName);
        Assert.Equal("System.Boolean", GameAssembly.Method("Player", "HaveStamina", "Single").ReturnType.FullName);
        Assert.Equal("System.String", GameAssembly.Method("Player", "GetPlayerName").ReturnType.FullName);
        Assert.Equal("Character", GameAssembly.Method("HitData", "GetAttacker").ReturnType.Name);
        Assert.Equal("System.Single", GameAssembly.Method("HitData", "GetTotalDamage").ReturnType.FullName);
    }

    [Theory]
    [InlineData("HitData", "m_blockable", "System.Boolean")]
    [InlineData("HitData", "m_dir", "UnityEngine.Vector3")]
    [InlineData("Character", "m_staggerWhenBlocked", "System.Boolean")]
    public void Fields_exist_with_expected_type(string typeName, string fieldName, string fieldType)
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
    public void ItemData_SharedData_m_timedBlockBonus_is_a_float()
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        TypeDefinition itemData = GameAssembly.NestedType("ItemDrop", "ItemData");
        TypeDefinition shared = itemData.NestedTypes.First(t => t.Name == "SharedData");
        Assert.Equal("System.Single", shared.Fields.First(f => f.Name == "m_timedBlockBonus").FieldType.FullName);
        Assert.Equal("SharedData", itemData.Fields.First(f => f.Name == "m_shared").FieldType.Name);
    }

    [Fact]
    public void Humanoid_IsBlocking_still_has_the_vanilla_rules_the_forced_value_mirrors()
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        // m_blocking && !InAttack() && !InDodge() && !InPlaceMode() && !IsEncumbered() && !InMinorAction()
        // ? !IsStaggering() : false; the forced value drops only the first two. Order matters (short-circuit).
        string[] ops = Operands(GameAssembly.Method("Humanoid", "IsBlocking"));
        string[] calls =
        {
            "::m_blocking", "::InAttack()", "::InDodge()", "::InPlaceMode()", "::IsEncumbered()",
            "::InMinorAction()", "::IsStaggering()",
        };
        int last = -1;
        foreach (string c in calls)
        {
            int i = System.Array.FindIndex(ops, o => o.Contains(c));
            Assert.True(i > last, $"Humanoid.IsBlocking: {c} missing or out of order ({i} after {last}).");
            last = i;
        }

        Assert.Equal(calls.Length, ops.Count(o => calls.Any(o.Contains)));
        Assert.Contains(ops, o => o.Contains("ZNetView::IsOwner"));
        Assert.Contains(ops, o => o.Contains("ZDOVars::s_isBlockingHash"));
    }

    [Fact]
    public void Character_m_nview_is_a_ZNetView()
    {
        if (!GameAssembly.IsAvailable)
        {
            return;
        }

        Assert.Equal("ZNetView", GameAssembly.Field("Character", "m_nview").FieldType.Name);
        Assert.True(GameAssembly.Method("ZNetView", "IsValid").IsPublic);
        Assert.True(GameAssembly.Method("ZNetView", "IsOwner").IsPublic);
    }
}
