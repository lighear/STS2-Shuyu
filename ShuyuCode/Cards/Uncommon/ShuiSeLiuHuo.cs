using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Characters;
using Shuyu.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Cards
{
    [RegisterCard(typeof(ShuyuCardPool))]
    public class ShuiSeLiuHuo : ModCardTemplate
    {
        public ShuiSeLiuHuo() : base(
            baseCost: 1,
            CardType.Power,
            CardRarity.Uncommon,
            TargetType.Self)
        { }

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
            HoverTipFactory.FromPower<FragilePower>(),
            HoverTipFactory.Static(StaticHoverTip.Block),
            //HoverTipFactory.FromPower<StrengthPower>(),
            HoverTipFactory.FromPower<DexterityPower>()
        ];

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            //new PowerVar<StrengthPower>(1),
            new PowerVar<DexterityPower>(1),
            new BlockVar(8,ValueProp.Unpowered)
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            ShuiSeLiuHuoPower? power = await PowerCmd.Apply<ShuiSeLiuHuoPower>(choiceContext, Owner.Creature, DynamicVars.Dexterity.BaseValue, Owner.Creature, this);
            //power?.AddStrength(DynamicVars.Strength.BaseValue);
            power?.AddBlock(DynamicVars.Block.BaseValue);
            power?.AddDexterity(DynamicVars.Dexterity.BaseValue);
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Block.UpgradeValueBy(4);
            //DynamicVars.Dexterity.UpgradeValueBy(1);
        }
    }
}