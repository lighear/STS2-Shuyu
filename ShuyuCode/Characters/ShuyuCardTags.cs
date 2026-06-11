using MegaCrit.Sts2.Core.Entities.Cards;
using Shuyu;
using STS2RitsuLib.CardTags;
using STS2RitsuLib.Content;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Shuyu.Characters;

[RegisterOwnedCardTag(nameof(Taboo))]
public class ShuyuCardTags
{
    public static readonly CardTag Taboo = ModContentRegistry.GetQualifiedCardTagId(Entry.ModId, nameof(Taboo)).GetModCardTag();
}