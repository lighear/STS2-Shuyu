using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Characters;
using Shuyu.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Cards
{
    [RegisterCard(typeof(ShuyuCardPool))]
    public class WuQingChuanCi : ModCardTemplate
    {
        public WuQingChuanCi() : base(
            baseCost: 1,
            CardType.Attack,
            CardRarity.Common,
            TargetType.AnyEnemy)
        { }

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
            HoverTipFactory.FromPower<FragilePower>(),
            HoverTipFactory.FromKeyword(ShuyuKeywords.Break)
            
        ];

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new DamageVar(7, ValueProp.Move),
            new PowerVar<FragilePower>(2)
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            if (cardPlay.Target!.Powers.Count(ShouldCountPower) > 0)
            {
                ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
                NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(NThinSliceVfx.Create(cardPlay.Target));
                await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
                    .FromCard(this, cardPlay)
                    .Targeting(cardPlay.Target!)
                    .Execute(choiceContext);
                await PowerCmd.Apply<FragilePower>(choiceContext, cardPlay.Target!, DynamicVars["FragilePower"].BaseValue, Owner.Creature, this);
            }
            
            ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
            NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(NThinSliceVfx.Create(cardPlay.Target));
            await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay)
                .Targeting(cardPlay.Target!)
                .Execute(choiceContext);
            await PowerCmd.Apply<FragilePower>(choiceContext, cardPlay.Target!, DynamicVars["FragilePower"].BaseValue, Owner.Creature, this);
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Damage.UpgradeValueBy(2);
            //DynamicVars["FragilePower"].UpgradeValueBy(1);
        }
        
        private static bool ShouldCountPower(PowerModel power)
        {
            if (power.TypeForCurrentAmount == PowerType.Debuff)
            {
                return !(power is ITemporaryPower);
            }
            return false;
        }
        
        
    }
}
