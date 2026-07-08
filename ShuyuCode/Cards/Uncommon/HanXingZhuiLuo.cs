using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Characters;
using Shuyu.Vfx;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Cards
{
    [RegisterCard(typeof(ShuyuCardPool))]
    public class HanXingZhuiLuo : ModCardTemplate
    {
        public HanXingZhuiLuo() : base(
            baseCost: 3,
            CardType.Attack,
            CardRarity.Uncommon,
            TargetType.AllEnemies)
        { }

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
            HoverTipFactory.FromCard<BingZhen>()
        ];

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new DamageVar(9, ValueProp.Move)
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {

            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay)
                .TargetingAllOpponents(CombatState!)
                .Execute(choiceContext);
            
            VFXUtil.PlaySimple("res://Shuyu/scenes/vfx/vfx_HanXingZhuiLuo.tscn", VfxCmd.GetSideCenter(CombatSide.Enemy,CombatState!)?? Vector2.Zero, 2f);

            await CardCmd.Discard(choiceContext, await CardSelectCmd.FromHandForDiscard(choiceContext, Owner, new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, 2), null, this));
            await BingZhen.CreateInHand(Owner, 2, CombatState!, false);
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Damage.UpgradeValueBy(4);
        }
    }
}