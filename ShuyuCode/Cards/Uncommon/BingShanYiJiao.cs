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
    public class BingShanYiJiao : ModCardTemplate
    {
        public BingShanYiJiao() : base(
            baseCost: 1,
            CardType.Skill,
            CardRarity.Uncommon,
            TargetType.AnyAlly)
        { }

        public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
            HoverTipFactory.FromPower<IceShieldPower>()
        ];
        
        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new PowerVar<IceShieldPower>(9),
        ];
        
        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

            IceShieldPower? iceShieldPower = await PowerCmd.Apply<IceShieldPower>(choiceContext, Owner.Creature, DynamicVars["IceShieldPower"].BaseValue,
                Owner.Creature, this);
            await PowerCmd.Apply<IceShieldPower>(choiceContext, cardPlay.Target, Owner.Creature.GetPowerAmount<IceShieldPower>()/3,
                Owner.Creature, this);
            iceShieldPower?.SetAmount(Owner.Creature.GetPowerAmount<IceShieldPower>() - Owner.Creature.GetPowerAmount<IceShieldPower>()/3);
        }
        
        protected override void OnUpgrade()
        {
            DynamicVars["IceShieldPower"].UpgradeValueBy(3);
        }
    }
}