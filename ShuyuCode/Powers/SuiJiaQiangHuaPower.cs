using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Interfaces;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Powers;

[RegisterPower]
public class SuiJiaQiangHuaPower : ModPowerTemplate, IOnFragileConverted
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png"
    );

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<FragilePower>(),
        HoverTipFactory.FromPower<WeakPower>()
    ];

    public async Task OnFragileConverted(PlayerChoiceContext choiceContext, Creature powerOwner, Creature? powerApplier)
    {
        await PowerCmd.Apply<WeakPower>(choiceContext, powerOwner, 3, powerApplier, null);
        await CreatureCmd.Damage(choiceContext, powerOwner, Amount, ValueProp.Unpowered, powerApplier ?? Owner);
    }
}