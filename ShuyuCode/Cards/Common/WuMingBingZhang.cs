using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Characters;
using Shuyu.Interfaces;
using Shuyu.Vfx;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Cards
{
    [RegisterCard(typeof(ShuyuCardPool))]
    public class WuMingBingZhang : ModCardTemplate, IFrostforged
    {
        public WuMingBingZhang() : base(
            baseCost: 2,
            CardType.Attack,
            CardRarity.Common,
            TargetType.AnyEnemy)
        { }

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        public override IEnumerable<CardKeyword> CanonicalKeywords => [
            CardKeyword.Retain,
            ShuyuKeywords.Frostforged
        ];

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new DamageVar(12, ValueProp.Move),
            new DynamicVar("ExtraDamage", 8)
        ];

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            Node2D? vfxNode = VFXUtil.PlaySimple(SceneHelper.GetScenePath("vfx/vfx_attack_blunt"), NCombatRoom.Instance?.GetCreatureNode(cardPlay.Target)?.VfxSpawnPosition ?? Vector2.Zero, 2f);
            if (vfxNode != null)
            {
                vfxNode.Modulate = new Color("#00d7d7");
            }
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay)
                .Targeting(cardPlay.Target!)
                .Execute(choiceContext);
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Damage.UpgradeValueBy(4);
            DynamicVars["ExtraDamage"].UpgradeValueBy(2);
        }

        private decimal ExtraDamageFromFrozen;

        public async Task FrostforgedEffect()
        {
            foreach (WuMingBingZhang item in base.Owner.PlayerCombatState!.AllCards.OfType<WuMingBingZhang>())
            {
                decimal extraDamage = DynamicVars["ExtraDamage"].BaseValue;
                item.DynamicVars.Damage.BaseValue += extraDamage;
                item.ExtraDamageFromFrozen += extraDamage;
            }
        }

        protected override void AfterDowngraded()
        {
            base.AfterDowngraded();
            DynamicVars.Damage.BaseValue += ExtraDamageFromFrozen;
        }
    }
}
