using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;
using CustomSlugcatUtils.Tools;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using SlugBase;
using SlugBase.Features;
using UnityEngine;

namespace CustomSlugcatUtils.Hooks
{
    internal static class CraftHooks
    {

        internal static readonly PlayerFeature<bool> CanSingleCraft = new("can_craft_single", JsonUtils.ToBool);

        internal static readonly PlayerFeature<CraftData[]> CraftFeature = new("craft", (json) =>
        {
            List<CraftData> datas = new List<CraftData>();
            var list = json.AsList();

            foreach (var any in list)
            {
                var craftData = new CraftData();
                var obj = any.AsObject();
                var items = obj.Get("craft_items");
                if (items.TryList() is { } itemList)
                {
                    craftData.crafts.Add(ToCraftItemData(itemList.Get(0).AsObject()));
                    if (itemList.Count >= 2)
                        craftData.crafts.Add(ToCraftItemData(itemList.Get(1).AsObject()));
                }
                else
                    craftData.crafts.Add(ToCraftItemData(items.AsObject()));
                

                craftData.craftResult = ToCraftItemData(obj.Get("craft_result").AsObject());
                if (obj.TryGet("craft_cost") is { } cost)
                    craftData.costFood = JsonUtils.ToInt(cost);
                datas.Add(craftData);
            }
            return datas.ToArray();

        });


        internal class CraftData
        {
            public List<IconSymbol.IconSymbolData> crafts = new();
            public IconSymbol.IconSymbolData craftResult;
            public int costFood;
        }

        private static IconSymbol.IconSymbolData ToCraftItemData(JsonObject json)
        {
            var re = new IconSymbol.IconSymbolData();
            re.critType = CreatureTemplate.Type.StandardGroundCreature;
            re.itemType = JsonUtils.ToExtEnum<AbstractPhysicalObject.AbstractObjectType>(json.Get("type"));
            if (json.TryGet("data") is { } data)
                re.intData = data.AsInt();
            return re;
        }

        public static void OnModsInit()
        {
            On.Player.GraspsCanBeCrafted += Player_GraspsCanBeCrafted;
            On.Player.SpitUpCraftedObject += Player_SpitUpCraftedObject;
            try
            {
                IL.Player.GrabUpdate += Player_GrabUpdateIL;

            }
            catch (Exception e)
            {
                Debug.LogException(e);
                ErrorTracker.TrackError(e, "IL Hook for Player.GrabUpdate Failed!");
            }
        }

        private static void Player_GrabUpdateIL(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            c.GotoNext(i => i.MatchBrfalse(out _),
                i => i.MatchLdarg(0),
                i => i.MatchCall<Player>("GraspsCanBeCrafted"));
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<bool, Player, bool>>((re, self) =>
                    re || (CanSingleCraft.TryGet(self, out var value) && value)
            );
        }

        private static void Player_SpitUpCraftedObject(On.Player.orig_SpitUpCraftedObject orig, Player self)
        {
            if (GetCraftResult(self, out var cost) is { } result)
            {
                for (int i = 0; i < 2; i++)
                {
                    if (self.grasps[i] == null)
                        continue;
                    self.grasps[i].grabbed.Destroy();
                    self.ReleaseGrasp(i);
                }
                SandboxGameSession sandBox = (SandboxGameSession)FormatterServices.GetUninitializedObject(typeof(SandboxGameSession));
                sandBox.game = (RainWorldGame)FormatterServices.GetUninitializedObject(typeof(RainWorldGame));

                sandBox.game.overWorld = (OverWorld)FormatterServices.GetUninitializedObject(typeof(OverWorld));
                sandBox.game.overWorld.activeWorld = (World)FormatterServices.GetUninitializedObject(typeof(World));
                sandBox.game.world.abstractRooms = new AbstractRoom[1];
                sandBox.game.world.abstractRooms[0] = (AbstractRoom)FormatterServices.GetUninitializedObject(typeof(AbstractRoom));
                sandBox.game.world.abstractRooms[0].entities = new List<AbstractWorldEntity>();
                sandBox.SpawnItems(result, self.abstractCreature.pos, self.abstractCreature.world.game.GetNewID());

                var obj = sandBox.game.world.abstractRooms[0].entities.Pop() as AbstractPhysicalObject;
                obj.world = self.abstractCreature.world;
                obj.pos = self.abstractCreature.pos;
                self.abstractCreature.Room.AddEntity(obj);
                obj.RealizeInRoom();
                self.SubtractFood(cost);
                if (self.CanIPickThisUp(obj.realizedObject))
                    self.SlugcatGrab(obj.realizedObject, self.FreeHand());
                else
                {
                    foreach (var chunk in obj.realizedObject.bodyChunks)
                        chunk.HardSetPosition(self.DangerPos);
                }


                return;
            }
            orig(self);
        }

        private static bool Player_GraspsCanBeCrafted(On.Player.orig_GraspsCanBeCrafted orig, Player self)
        {
            return orig(self) || GetCraftResult(self, out _) != null;
        }

        private static ItemSymbol.IconSymbolData? GetCraftResult(Player self,out int cost)
        {
            cost = 0;
            if (CraftFeature.TryGet(self, out var craft))
            {
                foreach (var item in craft)
                {
                    if(self.FoodInStomach < item.costFood)
                        continue;
                    if(item.crafts.Count != self.grasps.Count(i => i is { grabbed: { } }) || item.crafts.Count > self.grasps.Count(i => i is { grabbed: { } }))
                        continue;
                    if (item.crafts.Count == 1 && self.grasps.Any(i => i != null && ItemSymbol.SymbolDataFromItem(i.grabbed.abstractPhysicalObject) == item.crafts[0]))
                    {
                        cost = item.costFood;
                        return item.craftResult;
                    }
                    else if (item.crafts.Count == 2 && ((ItemSymbol.SymbolDataFromItem(self.grasps[0].grabbed.abstractPhysicalObject) == item.crafts[0] && 
                                               ItemSymbol.SymbolDataFromItem(self.grasps[1].grabbed.abstractPhysicalObject) == item.crafts[1]) ||
                             (ItemSymbol.SymbolDataFromItem(self.grasps[0].grabbed.abstractPhysicalObject) == item.crafts[1] && 
                              ItemSymbol.SymbolDataFromItem(self.grasps[1].grabbed.abstractPhysicalObject) == item.crafts[0])))
                    {
                        cost = item.costFood;
                        return item.craftResult;
                    }
                }
            }

            return null;
        }
    }
}
