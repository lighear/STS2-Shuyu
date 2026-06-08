using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using Shuyu.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shuyu.Afflications
{
    [RegisterAffliction]
    public class Frozen : ModAfflictionTemplate
    {
        public override void AfterApplied()
        {
            base.AfterApplied();
            ShuyuMechanismHelper.FreezeCard(base.Card);
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
