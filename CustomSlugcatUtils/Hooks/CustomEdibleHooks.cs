﻿using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using On.Menu;
using SlugBase;
using SlugBase.Features;
using UnityEngine;
using UnityEngine.Diagnostics;

namespace CustomSlugcatUtils.Hooks
{



    internal static class CustomEdibleHooks
    {
        private static readonly PlayerFeature<CustomEdibleData> CustomEdibles = new("custom_edibles",
            (json) =>
            {

                var re = new CustomEdibleData();
                var list = json.AsList();
                foreach (var data in list)
                {
                    if (data.TryObject() is { } obj)
                    {
                        if (obj.TryGet("type") is { } any)
                        {
                            float foodPoint = -2;
                            if (obj.TryGet("food_point") is { } food && food.TryFloat() is { } toFood)
                                foodPoint = toFood;
                            re.edibleDatas.Add(new CustomEdibleData.FoodData(
                                JsonUtils.ToExtEnum<AbstractPhysicalObject.AbstractObjectType>(any),
                                Mathf.FloorToInt(foodPoint), Mathf.FloorToInt((foodPoint - Mathf.FloorToInt(foodPoint)) * 4)));
                        }
                        else if (obj.TryGet("forbiddenType") is { } any2)
                        {
                            re.edibleDatas.Add(new CustomEdibleData.FoodData(
                                JsonUtils.ToExtEnum<AbstractPhysicalObject.AbstractObjectType>(any2), -1, -1));
                        }
                    }
                }

                return re;

            });

        public static void OnModInit()
        {
            IL.Player.GrabUpdate += Player_GrabUpdate_EdibleIL;
            On.Player.BiteEdibleObject += Player_BiteEdibleObject;
        }


        private static void Player_GrabUpdate_EdibleIL(ILContext il)
        {
            try
            {
                ILCursor c = new ILCursor(il);
                ILLabel label = c.DefineLabel();
                ILLabel label2 = c.DefineLabel();
                c.GotoNext(MoveType.Before, i => i.MatchLdarg(0),
                                           i => i.MatchCall<Creature>("get_grasps"),
                                           i => i.MatchLdloc(13),
                                           i => i.MatchLdelemRef(),
                                           i => i.MatchLdfld<Creature.Grasp>("grabbed"),
                                           i => i.MatchIsinst<IPlayerEdible>());
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloc_S, (byte)13);
                c.EmitDelegate<Func<Player, int, bool>>(EdibleForCat);
                c.Emit(OpCodes.Brtrue_S, label);

                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloc_S, (byte)13);
                c.EmitDelegate<Func<Player, int, bool>>((self, index) =>
                {
                    if (CustomEdibles.TryGet(self, out var data) &&
                        data.edibleDatas.Any(i => i.forbidType == self.grasps[index].grabbed.abstractPhysicalObject.type))
                        return false;
                    return true;
                });
                c.Emit(OpCodes.Brfalse_S, label2);
                c.GotoNext(MoveType.Before, i => i.MatchLdloc(13),
                                            i => i.MatchStloc(6),
                                            i => i.MatchLdloc(13));
                c.MarkLabel(label);
                c.GotoNext(MoveType.After, i => i.MatchStloc(6));
                c.MarkLabel(label2);

            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        static bool EdibleForCat(Player player, int index)
        {
            if (!CustomEdibles.TryGet(player, out var data))
                return false;
            var grasp = player.grasps[index];

            if (grasp != null)
            {
                if (data.edibleDatas.
                    Any(i => i.edibleType == grasp.grabbed.abstractPhysicalObject.type))
                    return true;
            }

            return false;
        }
        private static void Player_BiteEdibleObject(On.Player.orig_BiteEdibleObject orig, Player self, bool eu)
        {
            bool canBitOther = self.grasps.All(i => !(i?.grabbed is IPlayerEdible));
            orig(self, eu);
            if (canBitOther && CustomEdibles.TryGet(self, out var customEdibleData))
            {
                for (int i = 0; i < 2; i++)
                {
                    if (self.grasps[i] != null && customEdibleData.edibleDatas.
                            Any(d => d.edibleType == self.grasps[i].grabbed.abstractPhysicalObject.type))
                    {
                        var data = customEdibleData.edibleDatas.
                            First(d => d.edibleType == self.grasps[i].grabbed.abstractPhysicalObject.type);
                        if (self.SessionRecord != null)
                        {
                            self.SessionRecord.AddEat(self.grasps[i].grabbed);
                        }
                        (self.graphicsModule as PlayerGraphics)?.BiteFly(i);
                        self.AddFood(data.food);
                        for (int j = 0; j < data.qFood; j++)
                            self.AddQuarterFood();
                        var obj = self.grasps[i].grabbed;
                        self.grasps[i].Release();
                        obj.Destroy();
                    }
                }
            }
        }

    }
    public class CustomEdibleData
    {

        public readonly List<FoodData> edibleDatas = new();


        public class FoodData
        {
            public AbstractPhysicalObject.AbstractObjectType edibleType;
            public AbstractPhysicalObject.AbstractObjectType forbidType;
            public int food;
            public int qFood;



            /// <summary>
            /// 添加新的可食用项
            /// </summary>
            /// <param name="edibleType">可食用的物体类型</param>
            /// <param name="food">食用回复的整数饱食度</param>
            /// <param name="quarterFood">食用回复的小数饱食度(1/4格)</param>
            public FoodData(AbstractPhysicalObject.AbstractObjectType edibleType, int food, int quarterFood)
            {
                if (food < 0)
                {
                    forbidType = edibleType;
                    return;
                }
                this.edibleType = edibleType;
                this.food = food;
                this.qFood = quarterFood;
            }
        }
    }

}


