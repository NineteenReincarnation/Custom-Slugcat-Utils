using SlugBase.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil.Cil;
using SlugBase;

namespace CustomSlugcatUtils.Hooks
{

    internal class CustomGrabability
    {
        public CustomType type;
        public Player.ObjectGrabability grabability;
    }



    internal static class CustomGrababilityHooks
    {
        private static readonly PlayerFeature<CustomGrabability[]> CustomGrabability = new("custom_grabability", (any) =>
        {
            var re = new List<CustomGrabability>();
            if (any.TryList() is { } list)
            {
                foreach (var item in list)
                {
                    var obj = item.AsObject();
                    re.Add(new CustomGrabability
                    {
                        grabability = JsonUtils.ToEnum<Player.ObjectGrabability>(obj.Get("grabability")),
                        type = CustomEdibleHooks.ToCustomType(obj.Get("type"))
                    });
                }
            }
            else
            {
                var obj = any.AsObject();
                re.Add(new CustomGrabability
                {
                    grabability = JsonUtils.ToEnum<Player.ObjectGrabability>(obj.Get("grabability")),
                    type = CustomEdibleHooks.ToCustomType(obj.Get("type"))
                });
            }

            return re.ToArray();
        });


        public static void OnModsInit()
        {
            On.Player.Grabability += Player_Grabability;
            On.Player.CanMaulCreature += Player_CanMaulCreature;
        }

        private static bool Player_CanMaulCreature(On.Player.orig_CanMaulCreature orig, Player self, Creature crit)
        {
            if (CustomGrabability.TryGet(self, out var grabList))
            {
                foreach (var grab in grabList)
                    if (CustomEdibleHooks.IsSame(grab.type, crit))
                        return false;

            }
            return orig(self, crit);
        }

        private static Player.ObjectGrabability Player_Grabability(On.Player.orig_Grabability orig, Player self, PhysicalObject obj)
        {
            var re = orig(self, obj);
            if (CustomGrabability.TryGet(self, out var grabList))
            {
                foreach(var grab in grabList)
                    if (CustomEdibleHooks.IsSame(grab.type, obj))
                        return grab.grabability;
                    
            }

            return re;
        }
    }
}
