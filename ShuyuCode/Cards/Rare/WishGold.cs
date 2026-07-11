using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Cards
{
    [RegisterCard(typeof(TokenCardPool))]
    public class WishGold : ModCardTemplate, JiuChanXuYuanShu.IChoosable
    {
        public WishGold() : base(
            baseCost: -1,
            CardType.Status,
            CardRarity.Status,
            TargetType.None)
        { }

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        public override IEnumerable<CardKeyword> CanonicalKeywords => [
            
        ];

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new GoldVar(20)
        ];

        public override int MaxUpgradeLevel => 99;

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await PowerCmd.Apply<RoyaltiesPower>(choiceContext, Owner.Creature, DynamicVars.Gold.BaseValue, Owner.Creature, this);
        }
        
        public async Task OnChosen()
        {
            foreach (Creature ally in Owner.Creature.CombatState!.GetTeammatesOf(Owner.Creature).Where(c => c != null && c.IsAlive && c.IsPlayer))
            {
                foreach (JiuChanXuYuanShu item in ally.Player!.PlayerCombatState!.AllCards.OfType<JiuChanXuYuanShu>())
                { 
                    item.UpgradeGold();
                }
            }
            
            await PowerCmd.Apply<RoyaltiesPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, DynamicVars.Gold.BaseValue, Owner.Creature, this);
        }

        protected override void OnUpgrade()
        { 
            DynamicVars.Gold.UpgradeValueBy(2);
        }
    }
}
