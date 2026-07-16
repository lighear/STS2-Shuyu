using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Interfaces;
using Shuyu.Vfx;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
namespace Shuyu.Powers;

[RegisterPower]
public class ChillPower : ModPowerTemplate
{
    // 类型，Buff或Debuff
    public override PowerType Type => PowerType.Debuff;
    // 叠加类型，Counter表示可叠加，Single表示不可叠加
    public override PowerStackType StackType => PowerStackType.Single;

    // 自定义图标路径。1:1即可。原版游戏大图256x256，小图64x64。
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png"
    );

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(6, ValueProp.Unblockable | ValueProp.Unpowered)
    ];

    private class Data
    {
        public bool selfApplied;
    }

    private bool SelfApplied
    {
        get
        {
            return GetInternalData<Data>().selfApplied;
        }
        set
        {
            GetInternalData<Data>().selfApplied = value;
        }
    }

    protected override object? InitInternalData()
    {
        return new Data() { selfApplied = false };
    }

    private decimal ChillDamage
    {
        get
        {
            decimal damage = 6;
            foreach (IModifyChillDamage ip in CombatState.IterateHookListeners().OfType<IModifyChillDamage>())
            {
                damage = ip.ModifyChillDamage(damage);
            }
            DynamicVars.Damage.BaseValue = damage;
            return damage;
        }
    }

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(Owner))
        {
            return;
        }

        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), Owner, ChillDamage, ValueProp.Unblockable | ValueProp.Unpowered, Owner);
        if (Owner.IsAlive)
        {
            await PowerCmd.Remove(this);
        }
        else
        {
            await Cmd.CustomScaledWait(0.1f, 0.25f);
        }
    }

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (amount != 0 && power.GetTypeForAmount(amount) == PowerType.Debuff && power.Owner == Owner && power is not ITemporaryPower)
        {
            if (amount > 0)
            {
                await (Owner.GetPower<FragilePower>()?.ConvertIfThresholdMet(choiceContext, applier) ?? Task.CompletedTask);
                if (!Owner.IsAlive)
                {
                    return;
                }
            }

            if (power is ChillPower)
            {
                if (SelfApplied)
                {
                    SelfApplied = false;
                    return;
                }
                Flash();
                await CreatureCmd.Damage(choiceContext, Owner, ChillDamage * 2, ValueProp.Unblockable | ValueProp.Unpowered, Owner);
            }
            else
            {
                Flash();
                await CreatureCmd.Damage(choiceContext, Owner, ChillDamage, ValueProp.Unblockable | ValueProp.Unpowered, Owner);
            }
        }
    }

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        SelfApplied = true;
        UpdateDescription();

        var creatureVisuals = NCombatRoom.Instance?.GetCreatureNode(Owner)?.Visuals;
        var creatureBounds = creatureVisuals?.Bounds;
        if (creatureVisuals != null && creatureBounds != null && creatureBounds.GetNodeOrNull<Node2D>("VfxChillPowerParticle") == null)
        {
            Vector2 visualCenter = creatureBounds.Size * 0.5f;
            if (creatureVisuals.GetCurrentBody() is Sprite2D sprite && sprite.Texture != null)
            {
                visualCenter = creatureBounds.GetGlobalTransform().AffineInverse() * sprite.GlobalPosition;
            }

            string particlePath = "res://Shuyu/scenes/vfx_ChillPower_particle.tscn";
            Node2D vfxChillPowerParticle = VFXUtil.GenVFXNode<Node2D>(particlePath);
            creatureBounds.AddChildSafely(vfxChillPowerParticle);

            vfxChillPowerParticle.Position = visualCenter;
            GpuParticles2D snowflake = vfxChillPowerParticle.GetNodeOrNull<GpuParticles2D>("snowflake");
            if (snowflake != null)
            {
                float width = creatureBounds.Size.X;
                float height = creatureBounds.Size.Y;

                snowflake.Position = new Vector2(0, -height * 0.3f);

                ParticleProcessMaterial mat = (ParticleProcessMaterial)snowflake.ProcessMaterial.Duplicate();
                mat.EmissionBoxExtents = new Vector3(width * 0.5f, height * 0.5f, 1);
                mat.InitialVelocityMin = height * 0.15f;
                mat.InitialVelocityMax = height * 0.35f;
                mat.Gravity = new Vector3(0, height * 0.2f, 0);

                snowflake.ProcessMaterial = mat;
            }


            string backgroundPath = "res://Shuyu/scenes/vfx_ChillPower_background.tscn";
            ColorRect vfxChillPowerBackground = VFXUtil.GenVFXNode<ColorRect>(backgroundPath);
            creatureBounds.AddChildSafely(vfxChillPowerBackground);

            Vector2 expandSize = creatureBounds.Size * 0.2f;
            Vector2 backgroundSize = creatureBounds.Size + expandSize * 2f;
            vfxChillPowerBackground.AnchorLeft = 0f;
            vfxChillPowerBackground.AnchorTop = 0f;
            vfxChillPowerBackground.AnchorRight = 0f;
            vfxChillPowerBackground.AnchorBottom = 0f;
            vfxChillPowerBackground.Size = backgroundSize;
            vfxChillPowerBackground.Position = visualCenter - backgroundSize * 0.5f;
        }
    }

    public void UpdateDescription()
    {
        DynamicVars.Damage.BaseValue = ChillDamage;
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        var creatureBounds = NCombatRoom.Instance?.GetCreatureNode(oldOwner)?.Visuals.Bounds;
        creatureBounds?.GetNodeOrNull<Node2D>("VfxChillPowerParticle")?.QueueFree();
        creatureBounds?.GetNodeOrNull<ColorRect>("VfxChillPowerBackground")?.QueueFree();
    }
}
