using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using Shuyu.Cards;
using Shuyu.Interfaces;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using System.Runtime.CompilerServices;

namespace Shuyu.Afflictions
{
    [RegisterAffliction]
    public class Frozen : ModAfflictionTemplate
    {
        public override bool CanAfflictUnplayableCards => true;
        public override AfflictionAssetProfile AssetProfile => new(OverlayScenePath: $"{Entry.ResPath}/scenes/afflictions/{GetType().Name}.tscn");
    }
}
