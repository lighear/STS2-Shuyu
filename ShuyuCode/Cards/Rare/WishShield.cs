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
    public class WishShield : ModCardTemplate, JiuChanXuYuanShu.IChoosable
    {
        public WishShield() : base(
            baseCost: 0,
            CardType.Skill,
            CardRarity.Token,
            TargetType.None)
        { }

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
            HoverTipFactory.FromPower<IceShieldPower>()
        ];

        public override IEnumerable<CardKeyword> CanonicalKeywords => [
            CardKeyword.Exhaust
        ];

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new PowerVar<IceShieldPower>(6)
        ];

        public override int MaxUpgradeLevel => 99;
        
        public async Task OnChosen()
        {
            foreach (Creature ally in Owner.Creature.CombatState!.GetTeammatesOf(Owner.Creature).Where(c => c != null && c.IsAlive && c.IsPlayer))
            {
                foreach (JiuChanXuYuanShu item in ally.Player!.PlayerCombatState!.AllCards.OfType<JiuChanXuYuanShu>())
                { 
                    item.UpgradeShield();
                }
            }
            
            await PowerCmd.Apply<IceShieldPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, DynamicVars["IceShieldPower"].BaseValue, Owner.Creature, null);
        }

        protected override void OnUpgrade()
        { 
            DynamicVars["IceShieldPower"].UpgradeValueBy(1);
        }
    }
}
