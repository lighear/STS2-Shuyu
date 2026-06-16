using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace Shuyu.Interfaces
{
    public interface IModifyDamageFinal
    {
        public decimal ModifyDamageFinal(ICombatState combatState, Creature? target, Creature? dealer, ValueProp props, decimal modifiedAmount, ref IEnumerable<AbstractModel> modifiers);
    }
}
