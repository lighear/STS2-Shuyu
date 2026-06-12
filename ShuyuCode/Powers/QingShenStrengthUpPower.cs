using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Shuyu.Cards;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Shuyu.Powers;

[RegisterPower]
public class QingShenStrengthUpPower : TemporaryStrengthPower
{
    public override AbstractModel OriginModel => ModelDb.Card<QingShen>();
    protected override bool IsPositive => true;
}