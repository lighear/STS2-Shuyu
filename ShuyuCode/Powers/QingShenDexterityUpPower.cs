using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models;
using Shuyu.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace Shuyu.Powers;

[RegisterPower]
public class QingShenDexterityUpPower : TemporaryDexterityPower, IModPowerAssetOverrides
{
    public override AbstractModel OriginModel => ModelDb.Card<QingShen>();

    protected override bool IsPositive => true;

    public PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png"
    );

    public string? CustomIconPath => AssetProfile.IconPath;
    public string? CustomBigIconPath => AssetProfile.BigIconPath;
}
