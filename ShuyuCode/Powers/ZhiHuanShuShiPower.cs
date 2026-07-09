using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Interfaces;
using STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Powers;

[RegisterPower]
public class ZhiHuanShuShiPower : ModPowerTemplate, IPowerExtraIconAmountLabelSpecsProvider, IOnFragileConverted
{
    public override PowerType Type => PowerType.Debuff;
    
    public override PowerStackType StackType => PowerStackType.Counter;
    
    public override PowerInstanceType InstanceType => PowerInstanceType.InstancedPerApplier;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png"
    );

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<FragilePower>(),
    ];
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new EnergyVar(0),
        new CardsVar(0)
    ];
    
    public void AddExtraEnergy(decimal energy)
    {
        DynamicVars.Energy.BaseValue += energy;
        InvokeDisplayAmountChanged();
    }
    
    public void AddExtraCards(decimal cards)
    {
        DynamicVars.Cards.BaseValue += cards;
        InvokeDisplayAmountChanged();
    }
    
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(base.Owner))
        {
            await PowerCmd.Remove(this);
        }
    }
    
    public async Task OnFragileConverted(PlayerChoiceContext choiceContext, Creature powerOwner, Creature? powerApplier)
    {
        if (powerOwner == Owner)
        {
            if (powerApplier != null &&  powerApplier.IsPlayer)
            {
                Flash();
                await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, powerApplier.Player);
                await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, powerApplier.Player);
            }
        }
    }
    
    public IReadOnlyList<ExtraIconAmountLabelSpec> GetPowerExtraIconAmountLabelSpecs()
    {
        return
        [
            ExtraIconAmountLabelSpec.Plain(ExtraIconAmountLabelCorner.TopRight, DynamicVars.Cards.IntValue.ToString())
        ];
    }
}