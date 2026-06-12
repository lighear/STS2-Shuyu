using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Characters;
using Shuyu.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Cards
{
    [RegisterCard(typeof(ShuyuCardPool))]
    public class NingShuangJuXiang : ModCardTemplate
    {
        public NingShuangJuXiang() : base(
            baseCost: 3,
            CardType.Skill,
            CardRarity.Uncommon,
            TargetType.Self)
        { }

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
            HoverTipFactory.FromPower<ChillPower>(),
            HoverTipFactory.FromKeyword(CardKeyword.Retain)
        ];

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new BlockVar(9, ValueProp.Move)
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
            await PowerCmd.Apply<NingShuangJuXiangPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
        }

        public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
        {
            if (card == this)
            {
                modifiedCost = originalCost - PileType.Hand.GetPile(Owner).Cards.Count(c => c.Keywords.Contains(CardKeyword.Retain));
                return true;
            }
            else
            {
                modifiedCost = originalCost;
                return false;
            }
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Block.UpgradeValueBy(3);
        }
    }
}