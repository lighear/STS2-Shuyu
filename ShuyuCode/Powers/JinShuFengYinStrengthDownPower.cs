using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Shuyu.Cards;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Shuyu.Powers;

[RegisterPower]
public class JinShuFengYinStrengthDownPower : TemporaryStrengthPower
{
    public override AbstractModel OriginModel => ModelDb.Card<JinShuFengYin>();
    protected override bool IsPositive => false;
}