using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Powers;

[RegisterPower]
public class ZhaMaoPower : ModPowerTemplate, IPowerExtraIconAmountLabelSpecsProvider
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png"
    );

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<IceThornsPower>(),
        HoverTipFactory.FromPower<FragilePower>(),
        HoverTipFactory.FromPower<VulnerablePower>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<FragilePower>(0)
    ];

    public void AddFragilePowerAmount(decimal amount)
    {
        DynamicVars["FragilePower"].BaseValue += amount;
        InvokeDisplayAmountChanged();
    }

    public decimal GetFragilePowerAmount()
    {
        return DynamicVars["FragilePower"].BaseValue;
    }

    public IReadOnlyList<ExtraIconAmountLabelSpec> GetPowerExtraIconAmountLabelSpecs()
    {
        return
        [
            ExtraIconAmountLabelSpec.Plain(ExtraIconAmountLabelCorner.TopRight, DynamicVars["FragilePower"].BaseValue.ToString())
        ];
    }
}
