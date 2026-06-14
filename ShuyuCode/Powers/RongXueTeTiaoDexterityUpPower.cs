using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Shuyu.Cards;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Shuyu.Powers;

[RegisterPower]
public class RongXueTeTiaoDexterityUpPower : TemporaryDexterityPower
{
    public override AbstractModel OriginModel => ModelDb.Card<RongXueTeTiao>();
    protected override bool IsPositive => true;
}