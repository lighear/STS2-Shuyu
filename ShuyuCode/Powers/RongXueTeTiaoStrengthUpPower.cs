using MegaCrit.Sts2.Core.Models.Powers;
using Shuyu.Cards;
using STS2RitsuLib.Combat.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Powers;

[RegisterPower]
public class RongXueTeTiaoStrengthUpPower : ModTemporaryAppliedPowerTemplate<RongXueTeTiao, StrengthPower>
{
    protected override bool IsPositive => true;
    protected override bool UntilEndOfOtherSideTurn => false;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png"
    );
}