using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CustomSlugcatUtils.Tools;
using HUD;
using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MoreSlugcats;
using RWCustom;
using SlugBase;
using SlugBase.Assets;
using SlugBase.Features;
using SlugBase.SaveData;
using UnityEngine;

namespace CustomSlugcatUtils.Hooks
{
    public static class CycleLimit
    {
        public static bool TryGetLimitCycles(this RainWorldGame game, out int cycle)
        {
            return (CycleLimitHooks.CycleLimit.TryGet(game, out cycle));
        }
    }

    internal static class CycleLimitHooks
    {
        internal static readonly GameFeature<int> CycleLimit = new ("cycle_limited", JsonUtils.ToInt);
        internal static readonly GameFeature<bool> CycleLimitForce = new ("cycle_limited_force", JsonUtils.ToBool);
        internal static readonly GameFeature<bool> LockAscend = new("lock_ascended_force", JsonUtils.ToBool);

        internal static readonly GameFeature<MenuScene.SceneID> CycleEndScene = new("select_menu_scene_cycle_end", JsonUtils.ToExtEnum<MenuScene.SceneID>);

        public static void OnModInit()
        {
            On.Menu.SlugcatSelectMenu.SlugcatPageContinue.ctor += SlugcatPageContinue_ctor;
            On.HUD.TextPrompt.AddMessage_string_int_int_bool_bool += TextPrompt_AddMessage_string_int_int_bool_bool;
            On.Menu.SlugcatSelectMenu.ContinueStartedGame += SlugcatSelectMenu_ContinueStartedGame;
            On.RainWorldGame.GoToRedsGameOver += RainWorldGame_GoToRedsGameOver;
            On.Menu.SlugcatSelectMenu.UpdateStartButtonText += SlugcatSelectMenu_UpdateStartButtonText;
            On.PlayerProgression.WipeSaveState += PlayerProgression_WipeSaveState;
            On.PlayerProgression.WipeAll += PlayerProgression_WipeAll;
            On.RainWorldGame.Win += RainWorldGame_Win;
            On.Menu.SlugcatSelectMenu.ctor += SlugcatSelectMenu_ctor;
            try
            {
                IL.Menu.SlugcatSelectMenu.SlugcatPage.AddImage += SlugcatPage_AddImage;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                ErrorTracker.TrackError(e, "IL Hook for SlugcatPage.AddImage Failed!");
            }

            new Hook(
                typeof(StoryGameSession).GetProperty("RedIsOutOfCycles", BindingFlags.Instance | BindingFlags.Public)
                    .GetGetMethod(), GetRedIsRedOutOfCycles);
            Plugin.Log("CycleLimit Hooks Loaded");

        }

        private static void SlugcatSelectMenu_ctor(On.Menu.SlugcatSelectMenu.orig_ctor orig, SlugcatSelectMenu self, ProcessManager manager)
        {
            var slugbase = manager.rainWorld.progression.miscProgressionData.GetSlugBaseData();
            slugbase.TryGet(CycleLimitedSave, out menuData);
            orig(self, manager);
        }

        private static void SlugcatPage_AddImage(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            c.GotoNext(MoveType.After, i => i.MatchStloc(0));
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc_0);
            c.EmitDelegate<Func<SlugcatSelectMenu.SlugcatPage, MenuScene.SceneID, MenuScene.SceneID>>((page, id) =>
            {
                if (menuData != null && menuData.deaths.Contains(page.slugcatNumber.value) &&
                    SlugBaseCharacter.TryGet(page.slugcatNumber, out var character) &&
                    CycleEndScene.TryGet(character, out var sceneId))
                {
                    if (CustomScene.Registry.TryGet(sceneId, out var customScene))
                    {
                        page.markOffset = (customScene.MarkPos ?? page.markOffset);
                        page.glowOffset = (customScene.GlowPos ?? page.glowOffset);
                        page.sceneOffset = (customScene.SelectMenuOffset ?? page.sceneOffset);
                        page.slugcatDepth = (customScene.SlugcatDepth ?? page.slugcatDepth);
                    }
                    return sceneId;
                }
                return id;

            });
            c.Emit(OpCodes.Stloc_0);
        }

        private static void RainWorldGame_Win(On.RainWorldGame.orig_Win orig, RainWorldGame self, bool malnourished)
        {
            if (CycleLimit.TryGet(self,out var cycleLimit) &&
                self.session is StoryGameSession session)
            {
                if (session.saveState.cycleNumber >= cycleLimit && CycleLimitForce.TryGet(self,out var force) && force && !self.rainWorld.ExpeditionMode)
                {
                    self.GoToRedsGameOver();
                    return;
                }
            }
            orig(self, malnourished);

        }

        private static void PlayerProgression_WipeAll(On.PlayerProgression.orig_WipeAll orig, PlayerProgression self)
        {
            orig(self);
            var data = self.miscProgressionData.GetSlugBaseData();
            if (data.TryGet(CycleLimitedSave, out MenuSaveData _))
                data.Remove(CycleLimitedSave);
            
        }

        private static void PlayerProgression_WipeSaveState(On.PlayerProgression.orig_WipeSaveState orig, PlayerProgression self, SlugcatStats.Name saveStateNumber)
        {
            orig(self, saveStateNumber);

            var data = self.miscProgressionData.GetSlugBaseData();
            if (data.TryGet($"{Plugin.ModId}_CycleLimited", out MenuSaveData dat))
            {
                dat.deaths.Remove(saveStateNumber.value);
                data.Set(CycleLimitedSave, dat);
            }

        }



        private static void RainWorldGame_GoToRedsGameOver(On.RainWorldGame.orig_GoToRedsGameOver orig, RainWorldGame self)
        {
            if (CycleLimit.TryGet(self, out _) && !self.rainWorld.ExpeditionMode)
            {
                if (self.manager.upcomingProcess != null) return;

                if (self.manager.musicPlayer != null)
                    self.manager.musicPlayer.FadeOutAllSongs(20f);

                var slugbase = self.manager.rainWorld.progression.miscProgressionData.GetSlugBaseData();
                var data = slugbase.ForceGet<MenuSaveData>(CycleLimitedSave);
                data.deaths.Add(self.StoryCharacter.value);
                slugbase.Set(CycleLimitedSave, data);
                self.manager.rainWorld.progression.SaveWorldStateAndProgression(false);
                self.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Statistics, 10f);
                Plugin.Log($"{self.StoryCharacter} DIE, Exit to Statistics");
                return;
            }
            orig(self);
        }

        private static void SlugcatSelectMenu_UpdateStartButtonText(On.Menu.SlugcatSelectMenu.orig_UpdateStartButtonText orig, SlugcatSelectMenu self)
        {
            if (menuData != null && menuData.deaths.Contains(self.slugcatPages[self.slugcatPageIndex].slugcatNumber.value) ||
                (self.GetSaveGameData(self.slugcatPageIndex) != null && self.GetSaveGameData(self.slugcatPageIndex).ascended && SlugBaseCharacter.TryGet(self.slugcatPages[self.slugcatPageIndex].slugcatNumber, out var character) &&
                 LockAscend.TryGet(character, out var bo) && bo))
                self.startButton.menuLabel.text = self.Translate("STATISTICS");
            else
                orig(self);
        }

        private static void SlugcatSelectMenu_ContinueStartedGame(On.Menu.SlugcatSelectMenu.orig_ContinueStartedGame orig, SlugcatSelectMenu self, SlugcatStats.Name storyGameCharacter)
        {
            if ((menuData) != null && menuData.deaths.Contains(self.slugcatPages[self.slugcatPageIndex].slugcatNumber.value) ||
                (self.GetSaveGameData(self.slugcatPageIndex).ascended && SlugBaseCharacter.TryGet(storyGameCharacter,out var character) &&
                    LockAscend.TryGet(character,out var bo) && bo))
            {
                self.redSaveState = self.manager.rainWorld.progression.GetOrInitiateSaveState(storyGameCharacter, null, self.manager.menuSetup, false);
                self.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Statistics);
                self.PlaySound(SoundID.MENU_Switch_Page_Out);
                return;
            }
            orig(self, storyGameCharacter);

        }

        private static bool GetRedIsRedOutOfCycles(Func<StoryGameSession, bool> orig, StoryGameSession self)
        {
            if (CycleLimit.TryGet(self.game, out var limit) && self.saveState.cycleNumber > limit &&
                !self.game.rainWorld.ExpeditionMode)
            {
                return true;
            }

            return orig(self);
        }


        private static void SlugcatPageContinue_ctor(On.Menu.SlugcatSelectMenu.SlugcatPageContinue.orig_ctor orig, Menu.SlugcatSelectMenu.SlugcatPageContinue self, Menu.Menu menu, Menu.MenuObject owner, int pageIndex, SlugcatStats.Name slugcatNumber)
        {
            orig(self, menu, owner, pageIndex, slugcatNumber);
            if (SlugBaseCharacter.TryGet(slugcatNumber, out var character) &&
                CycleLimit.TryGet(character, out var limit))
            {
                string text = "";
                if (self.saveGameData.shelterName is { Length: > 2 })
                {
                    text = Region.GetRegionFullName(self.saveGameData.shelterName.Substring(0, 2), slugcatNumber);
                    if (text.Length > 0)
                    {
                        text = menu.Translate(text);
                        text = string.Concat(new[]
                        {
                            text,
                            " - ",
                            menu.Translate("Cycle"),
                            " ",
                            (limit - self.saveGameData.cycle).ToString()
                        });
                        if (ModManager.MMF)
                        {
                            TimeSpan timeSpan = TimeSpan.FromSeconds(self.saveGameData.gameTimeAlive + self.saveGameData.gameTimeDead);
                            text = text + " (" + SpeedRunTimer.TimeFormat(timeSpan) + ")";
                        }
                    }
                }
                self.regionLabel.text = text;
            }
        }

        private static void TextPrompt_AddMessage_string_int_int_bool_bool(On.HUD.TextPrompt.orig_AddMessage_string_int_int_bool_bool orig, TextPrompt self, string text, int wait, int time, bool darken, bool hideHud)
        {

            if (!self.hud.rainWorld.ExpeditionMode && (self.hud.rainWorld.processManager.currentMainLoop is RainWorldGame game) &&
                game.IsStorySession && CycleLimit.TryGet(game,out var limit) && text.Contains(Custom.rainWorld.inGameTranslator.Translate("Cycle")) && text.Contains(" ~ "))
            {
                text = Custom.rainWorld.inGameTranslator.Translate("Cycle")
                       + (limit - game.GetStorySession.saveState.cycleNumber) + " ~" + text.Split('~')[1];
            }
            orig(self, text, wait, time, darken, hideHud);
        }

        private static MenuSaveData menuData;
        private const string CycleLimitedSave = $"{Plugin.ModId}_CycleLimited";
    }

    internal static class SaveTool
    {
        public static T ForceGet<T>(this SlugBaseSaveData self, string name) where T : class, new()
        {
            if (!self.TryGet(name, out T data))
                data = new T();
            return data;
        }
    }

    internal class MenuSaveData
    {
        public HashSet<string> deaths = new();
    }
}
