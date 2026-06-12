using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Shuyu.Cards;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Shuyu.Powers;

[RegisterPower]
public class LinZhiTongStrengthDownPower : TemporaryStrengthPower
{
    public override AbstractModel OriginModel => ModelDb.Card<LinZhiTong>();
    protected override bool IsPositive => false;
}