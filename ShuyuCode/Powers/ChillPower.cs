using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Interfaces;
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

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        SelfApplied = true;
        UpdateDescription();
        return base.AfterApplied(applier, cardSource);
    }

    public void UpdateDescription()
    {
        DynamicVars.Damage.BaseValue = ChillDamage;
    }
}