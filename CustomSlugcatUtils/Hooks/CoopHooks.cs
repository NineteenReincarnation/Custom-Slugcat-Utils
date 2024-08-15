using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JollyCoop.JollyMenu;
using Mono.Cecil.Cil;

namespace CustomSlugcatUtils.Hooks
{
    internal static class CoopHooks
    {
        public static void OnModsInit()
        {
            IL.JollyCoop.JollyMenu.JollyPlayerSelector.ctor += JollyPlayerSelector_ctor;
            On.JollyCoop.JollyMenu.JollyPlayerSelector.GetPupButtonOffName += JollyPlayerSelector_GetPupButtonOffName;

        }
        private static void JollyPlayerSelector_ctor(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            while (c.TryGotoNext(MoveType.After, i => i.MatchLdstr("pup_on")))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<string, JollyPlayerSelector, string>>((re, self) =>
                {
                    var id = (self.JollyOptions(self.index).playerClass ?? SlugcatStats.Name.White).value;
                    if (File.Exists(AssetManager.ResolveFilePath($"Illustrations/{id}_on.png")))
                        return $"{id}_on";
                    return re;
                });
            }
        }

        private static string JollyPlayerSelector_GetPupButtonOffName(On.JollyCoop.JollyMenu.JollyPlayerSelector.orig_GetPupButtonOffName orig, JollyCoop.JollyMenu.JollyPlayerSelector self)
        {
            var re = orig(self);
            var id = (self.JollyOptions(self.index).playerClass ?? SlugcatStats.Name.White).value;

            if (self.pupButton != null)
                self.pupButton.symbolNameOn = File.Exists(AssetManager.ResolveFilePath($"Illustrations/{id}_on.png"))
                    ? $"{id}_on"
                    : "pup_on";
            return File.Exists(AssetManager.ResolveFilePath($"Illustrations/{id}_off.png")) ? $"{id}_off" : re;

        }
    }
}
