﻿using Darkages.Scripting;
using Darkages.Types;

namespace Darkages.Storage.locales.Scripts.Items
{
    [Script("wishingwell1", "Dean")]
    public class WishingWell : ItemScript
    {
        public WishingWell(Item item) : base(item)
        {
        }

        public override void Equipped(Sprite sprite, byte displayslot)
        {
        }

        public override void OnDropped(Sprite sprite, Position dropped_position, Area map)
        {
            var obj = GetObject<Item>(map,
                i => i.Position.X == dropped_position.X && i.Position.Y == dropped_position.Y
                                                        && i.Template.Name == Item.Template.Name &&
                                                        i.DisplayImage == Item.DisplayImage);

            if (obj != null) obj.Remove<Item>();
        }

        public override void OnUse(Sprite sprite, byte slot)
        {
        }

        public override void OnPickedUp(Sprite sprite, Position picked_position, Area map)
        {
        }

        public override void UnEquipped(Sprite sprite, byte displayslot)
        {
        }
    }
}