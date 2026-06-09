using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using Shuyu.Cards;
using Shuyu.Interfaces;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Afflictions
{
    [RegisterAffliction]
    public class Frozen : ModAfflictionTemplate
    {
        public override bool CanAfflictUnplayableCards => true;

        public override void AfterApplied()
        {
            base.AfterApplied();
            if (ShuyuMechanismHelper.FreezeCardInternal(base.Card))
            {
                foreach (IAfterFreezingCard ip in CombatState.IterateHookListeners().OfType<IAfterFreezingCard>())
                {
                    ip.AfterFreezingCard(base.Card);
                }
            }
        }

        public override void BeforeRemoved()
        {
            base.BeforeRemoved();
            if (base.Card is FrozenCardModel card)
            {
                ShuyuMechanismHelper.UnfreezeCard(card);
            }
        }
    }
}
