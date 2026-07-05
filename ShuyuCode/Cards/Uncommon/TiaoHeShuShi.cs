using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Characters;
using Shuyu.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Cards
{
    [RegisterCard(typeof(ShuyuCardPool))]
    public class TiaoHeShuShi : ModCardTemplate
    {
        public TiaoHeShuShi() : base(
            baseCost: 1,
            CardType.Skill,
            CardRarity.Uncommon,
            TargetType.AllAllies)
        { }

        public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");
        
        
        protected override IEnumerable<IHoverTip> AdditionalHoverTips
        {
            get
            {
                if (IsUpgraded)
                {
                    return [
                        HoverTipFactory.FromCard<SanXiangDian>(true),
                        HoverTipFactory.FromKeyword(CardKeyword.Retain),
                        ..HoverTipFactory.FromEnchantment<Sown>()
                    ];
                }
                else
                {
                    return [
                        HoverTipFactory.FromCard<SanXiangDian>(true),
                        HoverTipFactory.FromKeyword(CardKeyword.Retain)
                    ];
                }
            }
        }
        
        
        public override IEnumerable<CardKeyword> CanonicalKeywords => [
            CardKeyword.Exhaust
        ];
        

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            IEnumerable<Creature> enumerable = CombatState!.GetTeammatesOf(Owner.Creature).Where(c => c.IsAlive && c.IsPlayer);
            foreach (Creature creature in enumerable)
            {
                CardModel card = CombatState!.CreateCard<SanXiangDian>(creature.Player);
                CardCmd.Upgrade(card);
                CardCmd.ApplyKeyword(card,CardKeyword.Retain);
                if (base.IsUpgraded)
                {
                    CardCmd.Enchant<Sown>(card, 1);
                }
                await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner);
            }
        }
    }
}