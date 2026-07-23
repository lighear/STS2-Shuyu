using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Interfaces;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Powers;

[RegisterPower]
public class ShuiSeLiuHuoPower : ModPowerTemplate, IOnFragileConverted
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png"
    );

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<FragilePower>(),
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<DexterityPower>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<StrengthPower>(0),
        new PowerVar<DexterityPower>(0)
    ];

    public void AddStrength(decimal amount)
    {
        DynamicVars.Strength.BaseValue += amount;
    }

    public void AddDexterity(decimal amount)
    {
        DynamicVars.Dexterity.BaseValue += amount;
    }
    
    private class Data
    {
        public bool triggeredThisTurn;
    }

    private bool TriggeredThisTurn
    {
        get
        {
            return GetInternalData<Data>().triggeredThisTurn;
        }
        set
        {
            GetInternalData<Data>().triggeredThisTurn = value;
        }
    }
    
    protected override object? InitInternalData()
    {
        return new Data() { triggeredThisTurn = false };
    }
    
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side == Owner.Side)
        {
            TriggeredThisTurn = false;
        }
    }
    
    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (amount > 0 && applier == Owner && power is FragilePower && !TriggeredThisTurn)
        {
            Flash();
            TriggeredThisTurn = true;
            await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, DynamicVars.Strength.BaseValue, Owner, null);
        }
    }
    
    public async Task OnFragileConverted(PlayerChoiceContext choiceContext, Creature powerOwner, Creature? powerApplier)
    {
        Flash();
        await PowerCmd.Apply<DexterityPower>(choiceContext, Owner, DynamicVars.Dexterity.BaseValue, Owner, null);
    }
    
    
}