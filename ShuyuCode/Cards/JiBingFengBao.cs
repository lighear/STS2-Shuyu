using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Cards
{
    [RegisterCard(typeof(ShuyuCardPool))]
    public class JiBingFengBao : ModCardTemplate
    {
        public JiBingFengBao() : base(
        baseCost: 1,
        CardType.Attack,
        CardRarity.Ancient,
        TargetType.AllEnemies)
        { }

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new DamageVar(10, ValueProp.Move)
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .TargetingAllOpponents(base.CombatState!)
                .Execute(choiceContext);

            foreach (CardModel card in PileType.Hand.GetPile(base.Owner).Cards.Where(c => c.IsFrozen()).ToList())
            {
                ((FrozenCardModel)card).SetIcyDamageTargets(cardPlay.Target!);
                ((FrozenCardModel)card).SetIcyDamageCount(2);
                await CardCmd.Discard(choiceContext, card);
            }
        }

        protected override void OnUpgrade()
        {
            EnergyCost.UpgradeBy(-1);
        }
    }
}
