using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Cards
{
    [RegisterCard(typeof(ShuyuCardPool))]
    public class XiaoYun : ModCardTemplate
    {
        public XiaoYun() : base(
            baseCost: 1,
            CardType.Skill,
            CardRarity.Rare,
            TargetType.None)
        { }

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        protected override IEnumerable<IHoverTip> AdditionalHoverTips
        {
            get
            {
                if (IsUpgraded)
                {
                    return [HoverTipFactory.FromKeyword(CardKeyword.Retain),
                        HoverTipFactory.FromPower<StrengthPower>(),
                        HoverTipFactory.FromPower<DexterityPower>()];
                }
                else
                {
                    return [HoverTipFactory.FromKeyword(CardKeyword.Retain),
                        HoverTipFactory.FromPower<StrengthPower>()];
                }
            }
        }

        public override IEnumerable<CardKeyword> CanonicalKeywords => [
            CardKeyword.Exhaust
        ];

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new PowerVar<StrengthPower>(1),
            new PowerVar<DexterityPower>(1)
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            IEnumerable<CardModel> cards = PileType.Hand.GetPile(Owner).Cards.Where(c => c.Keywords.Contains(CardKeyword.Retain));
            int count = cards.Count();
            await PowerCmd.Apply<StrengthPower>(choiceContext, Owner.Creature, DynamicVars.Strength.BaseValue * count, Owner.Creature, this);
            if (IsUpgraded)
            {
                await PowerCmd.Apply<DexterityPower>(choiceContext, Owner.Creature, DynamicVars.Dexterity.BaseValue * count, Owner.Creature, this);
            }
            foreach (CardModel card in cards)
            {
                await CardCmd.Exhaust(choiceContext, card);
            }
        }
    }
}