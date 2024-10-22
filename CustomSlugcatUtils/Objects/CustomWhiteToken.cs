using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using MoreSlugcats;
using RWCustom;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CustomSlugcatUtils.Objects
{


    internal class CustomWhiteToken : UpdatableAndDeletable, IDrawable
    {
        public int LightSprite => 0;

        public int MainSprite => 1;

        public int TrailSprite => 2;

        public int LineSprite(int line)
        {
            return 3 + line;
        }

        public int GoldSprite => 7;

        public int TotalSprites => 8;

        public CustomWhiteToken(Room room, PlacedObject placedObj)
        {
            this.placedObj = placedObj;
            this.room = room;
            underWaterMode = (room.GetTilePosition(placedObj.pos).y < room.defaultWaterLevel);
            stalk = new CustomWhiteStalk(room, placedObj.pos, placedObj.pos + (placedObj.data as CustomWhiteToken.CollectTokenData).handlePos, this);
            room.AddObject(stalk);
            pos = placedObj.pos;
            hoverPos = pos;
            lastPos = pos;
            lines = new Vector2[4, 4];
            for (int i = 0; i < lines.GetLength(0); i++)
            {
                lines[i, 0] = pos;
                lines[i, 1] = pos;
            }
            lines[0, 2] = new Vector2(-7f, 0f);
            lines[1, 2] = new Vector2(0f, 11f);
            lines[2, 2] = new Vector2(7f, 0f);
            lines[3, 2] = new Vector2(0f, -11f);
            trail = new Vector2[5];
            for (int j = 0; j < trail.Length; j++)
            {
                trail[j] = pos;
            }
            soundLoop = new StaticSoundLoop(SoundID.Token_Idle_LOOP, pos, room, 0f, 1f);
            glitchLoop = new StaticSoundLoop(SoundID.Token_Upset_LOOP, pos, room, 0f, 1f);
        }

        public override void Update(bool eu)
        {
            if ((ModManager.MMF && !AvailableToPlayer()) )
            {
                stalk.Destroy();
                Destroy();
            }
            sinCounter += Random.value * power;
            sinCounter2 += (1f + Mathf.Lerp(-10f, 10f, Random.value) * glitch) * power;
            float num = Mathf.Sin(sinCounter2 / 20f);
            num = Mathf.Pow(Mathf.Abs(num), 0.5f) * Mathf.Sign(num);
            soundLoop.Update();
            soundLoop.pos = pos;
            soundLoop.pitch = 1f + 0.25f * num * glitch;
            soundLoop.volume = Mathf.Pow(power, 0.5f) * Mathf.Pow(1f - glitch, 0.5f);
            glitchLoop.Update();
            glitchLoop.pos = pos;
            glitchLoop.pitch = Mathf.Lerp(0.75f, 1.25f, glitch) - 0.25f * num * glitch;
            glitchLoop.volume = Mathf.Pow(Mathf.Sin(Mathf.Clamp(glitch, 0f, 1f) * 3.1415927f), 0.1f) * Mathf.Pow(power, 0.1f);
            lastPos = pos;
            for (int i = 0; i < lines.GetLength(0); i++)
            {
                lines[i, 1] = lines[i, 0];
            }
            lastGlitch = glitch;
            lastExpand = expand;
            for (int j = trail.Length - 1; j >= 1; j--)
            {
                trail[j] = trail[j - 1];
            }
            trail[0] = lastPos;
            lastPower = power;
            power = Custom.LerpAndTick(power, poweredOn ? 1f : 0f, 0.07f, 0.025f);
            glitch = Mathf.Max(glitch, 1f - power);
            pos += vel;
            for (int k = 0; k < lines.GetLength(0); k++)
            {
                if (stalk != null)
                {
                    lines[k, 0] += stalk.head - stalk.lastHead;
                }
                if (Mathf.Pow(Random.value, 0.1f + glitch * 5f) > lines[k, 3].x)
                {
                    lines[k, 0] = Vector2.Lerp(lines[k, 0], pos + new Vector2(lines[k, 2].x * num, lines[k, 2].y), Mathf.Pow(Random.value, 1f + lines[k, 3].x * 17f));
                }
                if (Random.value < Mathf.Pow(lines[k, 3].x, 0.2f) && Random.value < Mathf.Pow(glitch, 0.8f - 0.4f * lines[k, 3].x))
                {
                    lines[k, 0] += Custom.RNV() * 17f * lines[k, 3].x * power;
                    lines[k, 3].y = Mathf.Max(lines[k, 3].y, glitch);
                }
                lines[k, 3].x = Custom.LerpAndTick(lines[k, 3].x, lines[k, 3].y, 0.01f, 0.033333335f);
                lines[k, 3].y = Mathf.Max(0f, lines[k, 3].y - 0.014285714f);
                if (Random.value < 1f / Mathf.Lerp(210f, 20f, glitch))
                {
                    lines[k, 3].y = Mathf.Max(glitch, (Random.value < 0.5f) ? generalGlitch : Random.value);
                }
            }
            vel *= 0.995f;
            vel += Vector2.ClampMagnitude(hoverPos + new Vector2(0f, Mathf.Sin(sinCounter / 15f) * 7f) - pos, 15f) / 81f;
            vel += Custom.RNV() * Random.value * Random.value * Mathf.Lerp(0.06f, 0.4f, glitch);
            pos += Custom.RNV() * Mathf.Pow(Random.value, 7f - 6f * generalGlitch) * Mathf.Lerp(0.06f, 1.2f, glitch);
            if (expandAroundPlayer != null)
            {
                expandAroundPlayer.Blink(5);
                if (!contract)
                {
                    expand += 0.033333335f;
                    if (expand > 1f)
                    {
                        expand = 1f;
                        contract = true;
                    }
                    generalGlitch = 0f;
                    glitch = Custom.LerpAndTick(glitch, expand * 0.5f, 0.07f, 0.06666667f);
                    float num2 = Custom.SCurve(Mathf.InverseLerp(0.35f, 0.55f, expand), 0.4f);
                    Vector2 b = Vector2.Lerp(expandAroundPlayer.mainBodyChunk.pos + new Vector2(0f, 40f), Vector2.Lerp(expandAroundPlayer.bodyChunks[1].pos, expandAroundPlayer.mainBodyChunk.pos + Custom.DirVec(expandAroundPlayer.bodyChunks[1].pos, expandAroundPlayer.mainBodyChunk.pos) * 10f, 0.65f), expand);
                    for (int l = 0; l < lines.GetLength(0); l++)
                    {
                        Vector2 b2 = Vector2.Lerp(lines[l, 2] * (2f + 5f * Mathf.Pow(expand, 0.5f)), Custom.RotateAroundOrigo(lines[l, 2] * (2f + 2f * Mathf.Pow(expand, 0.5f)), Custom.AimFromOneVectorToAnother(expandAroundPlayer.bodyChunks[1].pos, expandAroundPlayer.mainBodyChunk.pos)), num2);
                        lines[l, 0] = Vector2.Lerp(lines[l, 0], Vector2.Lerp(pos, b, Mathf.Pow(num2, 2f)) + b2, Mathf.Pow(expand, 0.5f));
                        lines[l, 3] *= 1f - expand;
                    }
                    hoverPos = Vector2.Lerp(hoverPos, b, Mathf.Pow(expand, 2f));
                    pos = Vector2.Lerp(pos, b, Mathf.Pow(expand, 2f));
                    vel *= 1f - expand;
                }
                else
                {
                    generalGlitch *= 1f - expand;
                    glitch = 0.15f;
                    expand -= 1f / Mathf.Lerp(60f, 2f, expand);
                    Vector2 a = Vector2.Lerp(expandAroundPlayer.bodyChunks[1].pos, expandAroundPlayer.mainBodyChunk.pos + Custom.DirVec(expandAroundPlayer.bodyChunks[1].pos, expandAroundPlayer.mainBodyChunk.pos) * 10f, Mathf.Lerp(1f, 0.65f, expand));
                    for (int m = 0; m < lines.GetLength(0); m++)
                    {
                        Vector2 b3 = Custom.RotateAroundOrigo(Vector2.Lerp((Random.value > expand) ? lines[m, 2] : lines[Random.Range(0, 4), 2], lines[Random.Range(0, 4), 2], Random.value * (1f - expand)) * (4f * Mathf.Pow(expand, 0.25f)), Custom.AimFromOneVectorToAnother(expandAroundPlayer.bodyChunks[1].pos, expandAroundPlayer.mainBodyChunk.pos)) * Mathf.Lerp(Random.value, 1f, expand);
                        lines[m, 0] = a + b3;
                        lines[m, 3] *= 1f - expand;
                    }
                    pos = a;
                    hoverPos = a;
                    if (expand < 0f)
                    {
                        Destroy();
                        int num3 = 0;
                        while ((float)num3 < 20f)
                        {
                            room.AddObject(new CollectToken.TokenSpark(pos + Custom.RNV() * 2f, Custom.RNV() * 16f * Random.value, Color.Lerp(TokenColor, new Color(1f, 1f, 1f), Random.value), underWaterMode));
                            num3++;
                        }
                        room.PlaySound(SoundID.Token_Collected_Sparks, pos);
                        //if (anythingUnlocked && room.game.cameras[0].hud != null && room.game.cameras[0].hud.textPrompt != null)
                        //{
                            //TODO: UNLOCKED MESSAGE
                        //    room.game.cameras[0].hud.textPrompt.AddMessage(room.game.manager.rainWorld.inGameTranslator.Translate("New arenas unlocked"), 20, 160, true, true);
                            
                        //}
                    }
                }
            }
            else
            {
                generalGlitch = Mathf.Max(0f, generalGlitch - 0.008333334f);
                if (Random.value < 0.0027027028f)
                {
                    generalGlitch = Random.value;
                }
                if (!Custom.DistLess(pos, hoverPos, 11f))
                {
                    pos += Custom.DirVec(hoverPos, pos) * (11f - Vector2.Distance(pos, hoverPos)) * 0.7f;
                }
                float f = Mathf.Sin(Mathf.Clamp(glitch, 0f, 1f) * 3.1415927f);
                if (Random.value < 0.05f + 0.35f * Mathf.Pow(f, 0.5f) && Random.value < power)
                {
                    room.AddObject(new CollectToken.TokenSpark(pos + Custom.RNV() * 6f * glitch, Custom.RNV() * Mathf.Lerp(2f, 9f, Mathf.Pow(f, 2f)) * Random.value, GoldCol(glitch), underWaterMode));
                }
                glitch = Custom.LerpAndTick(glitch, generalGlitch / 2f, 0.01f, 0.033333335f);
                if (Random.value < 1f / Mathf.Lerp(360f, 10f, generalGlitch))
                {
                    glitch = Mathf.Pow(Random.value, 1f - 0.85f * generalGlitch);
                }
                float num4 = float.MaxValue;
                bool flag = AvailableToPlayer();
                if (RainWorld.lockGameTimer)
                {
                    flag = false;
                }
                float num5 = 140f;

                //TODO : UK num5
                //devToken
                //num5 = 2000f;
                
                for (int n = 0; n < room.game.session.Players.Count; n++)
                {
                    if (room.game.session.Players[n].realizedCreature != null && room.game.session.Players[n].realizedCreature.Consious && (room.game.session.Players[n].realizedCreature as Player).dangerGrasp == null && room.game.session.Players[n].realizedCreature.room == room)
                    {
                        num4 = Mathf.Min(num4, Vector2.Distance(room.game.session.Players[n].realizedCreature.mainBodyChunk.pos, pos));
                        if (flag)
                        {
                            if (Custom.DistLess(room.game.session.Players[n].realizedCreature.mainBodyChunk.pos, pos, 18f))
                            {
                                Pop(room.game.session.Players[n].realizedCreature as Player);
                                break;
                            }
                            if (Custom.DistLess(room.game.session.Players[n].realizedCreature.mainBodyChunk.pos, pos, num5))
                            {
                                if (Custom.DistLess(pos, hoverPos, 80f))
                                {
                                    pos += Custom.DirVec(pos, room.game.session.Players[n].realizedCreature.mainBodyChunk.pos) * Custom.LerpMap(Vector2.Distance(pos, room.game.session.Players[n].realizedCreature.mainBodyChunk.pos), 40f, num5, 2.2f, 0f, 0.5f) * Random.value;
                                }
                                if (Random.value < 0.05f && Random.value < Mathf.InverseLerp(num5, 40f, Vector2.Distance(pos, room.game.session.Players[n].realizedCreature.mainBodyChunk.pos)))
                                {
                                    glitch = Mathf.Max(glitch, Random.value * 0.5f);
                                }
                            }
                        }
                    }
                }
                if (!flag && poweredOn)
                {
                    lockdownCounter++;
                    if (Random.value < 0.016666668f || num4 < num5 - 40f || lockdownCounter > 30)
                    {
                        locked = true;
                    }
                    if (Random.value < 0.14285715f)
                    {
                        glitch = Mathf.Max(glitch, Random.value * Random.value * Random.value);
                    }
                }
                if (poweredOn && (locked || (expand == 0f && !contract && Random.value < Mathf.InverseLerp(num5 + 160f, num5 + 460f, num4))))
                {
                    poweredOn = false;
                    room.PlaySound(SoundID.Token_Turn_Off, pos);
                }
                else if (!poweredOn && !locked && Random.value < Mathf.InverseLerp(num5 + 60f, num5 - 20f, num4))
                {
                    poweredOn = true;
                    room.PlaySound(SoundID.Token_Turn_On, pos);
                }
            }
            base.Update(eu);
        }

        private bool AvailableToPlayer()
        {
            return true;
        }

        public void Pop(Player player)
        {
            if (expand > 0f || !(player.room.game.session is StoryGameSession session))
                return;
            expandAroundPlayer = player;
            expand = 0.01f;
            room.PlaySound(SoundID.Token_Collect, pos);
            var id = new ChatlogData.ChatlogID((placedObj.data as CollectTokenData).chatlogId);
            if (!session.saveState.deathPersistentSaveData.chatlogsRead.Contains(id))
            
                player.InitChatLog(id);


            for (int i = 0; i < 10f; i++)
                room.AddObject(new CollectToken.TokenSpark(pos + Custom.RNV() * 2f, Custom.RNV() * 11f * Random.value +
                    Custom.DirVec(player.mainBodyChunk.pos, pos) * 5f * Random.value,
                    GoldCol(glitch), underWaterMode));
        }

        public Color GoldCol(float g)
        {
            
            //TODO : GoldCol
            return Color.Lerp(TokenColor, new Color(1f, 1f, 1f), 0.4f + 0.4f * Mathf.Max(contract ? 0.5f : (expand * 0.5f), Mathf.Pow(g, 0.5f)));
            
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[TotalSprites];
            sLeaser.sprites[LightSprite] = new FSprite("Futile_White");
            sLeaser.sprites[LightSprite].shader = rCam.game.rainWorld.Shaders[underWaterMode ? "UnderWaterLight" : "FlatLight"];
            sLeaser.sprites[GoldSprite] = new FSprite("Futile_White");


            // TODO : GOLD GLOW
            sLeaser.sprites[GoldSprite].color = Color.Lerp(new Color(0f, 0f, 0f), RainWorld.GoldRGB, 0.2f);
            sLeaser.sprites[GoldSprite].shader = rCam.game.rainWorld.Shaders["FlatLight"];

            //sLeaser.sprites[GoldSprite].shader = rCam.game.rainWorld.Shaders["GoldenGlow"];

            sLeaser.sprites[MainSprite] = new FSprite("JetFishEyeA");
            sLeaser.sprites[MainSprite].shader = rCam.game.rainWorld.Shaders["Hologram"];
            sLeaser.sprites[TrailSprite] = new FSprite("JetFishEyeA");
            sLeaser.sprites[TrailSprite].shader = rCam.game.rainWorld.Shaders["Hologram"];
            for (int i = 0; i < 4; i++)
            {
                sLeaser.sprites[LineSprite(i)] = new FSprite("pixel");
                sLeaser.sprites[LineSprite(i)].anchorY = 0f;
                sLeaser.sprites[LineSprite(i)].shader = rCam.game.rainWorld.Shaders["Hologram"];
            }
            AddToContainer(sLeaser, rCam, null);
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            Vector2 vector = Vector2.Lerp(lastPos, pos, timeStacker);
            float num = Mathf.Lerp(lastGlitch, glitch, timeStacker);
            float num2 = Mathf.Lerp(lastExpand, expand, timeStacker);
            float num3 = Mathf.Lerp(lastPower, power, timeStacker);
            if (room != null && !AvailableToPlayer())
            {
                num = Mathf.Lerp(num, 1f, Random.value);
                num3 *= 0.3f + 0.7f * Random.value;
            }
            sLeaser.sprites[GoldSprite].x = vector.x - camPos.x;
            sLeaser.sprites[GoldSprite].y = vector.y - camPos.y;

            //TODO : ALPHA
            //blueToken || greenToken || redToken
            sLeaser.sprites[GoldSprite].alpha = 0.75f * Mathf.Lerp(Mathf.Lerp(0.8f, 0.5f, Mathf.Pow(num, 0.6f + 0.2f * Random.value)), 0.7f, num2) * num3;
            //sLeaser.sprites[GoldSprite].alpha = Mathf.Lerp(Mathf.Lerp(0.8f, 0.5f, Mathf.Pow(num, 0.6f + 0.2f * Random.value)), 0.7f, num2) * num3;
            

            //TODO : SCALE
            sLeaser.sprites[GoldSprite].scale = Mathf.Lerp(100f, 300f, num2) / 16f;
            Color color = GoldCol(num);
            sLeaser.sprites[MainSprite].color = color;
            sLeaser.sprites[MainSprite].x = vector.x - camPos.x;
            sLeaser.sprites[MainSprite].y = vector.y - camPos.y;
            sLeaser.sprites[MainSprite].alpha = (1f - num) * Mathf.InverseLerp(0.5f, 0f, num2) * num3 * (underWaterMode ? 0.5f : 1f);
            sLeaser.sprites[MainSprite].isVisible = (!contract && num3 > 0f);
            sLeaser.sprites[TrailSprite].color = color;
            sLeaser.sprites[TrailSprite].x = Mathf.Lerp(trail[trail.Length - 1].x, trail[trail.Length - 2].x, timeStacker) - camPos.x;
            sLeaser.sprites[TrailSprite].y = Mathf.Lerp(trail[trail.Length - 1].y, trail[trail.Length - 2].y, timeStacker) - camPos.y;
            sLeaser.sprites[TrailSprite].alpha = 0.75f * (1f - num) * Mathf.InverseLerp(0.5f, 0f, num2) * num3 * (underWaterMode ? 0.5f : 1f);
            sLeaser.sprites[TrailSprite].isVisible = (!contract && num3 > 0f);
            sLeaser.sprites[TrailSprite].scaleX = ((Random.value < num) ? (1f + 20f * Random.value * glitch) : 1f);
            sLeaser.sprites[TrailSprite].scaleY = ((Random.value < num) ? (1f + 2f * Random.value * Random.value * glitch) : 1f);
            sLeaser.sprites[LightSprite].x = vector.x - camPos.x;
            sLeaser.sprites[LightSprite].y = vector.y - camPos.y;
            if (underWaterMode)
            {
                sLeaser.sprites[LightSprite].alpha = Mathf.Pow(0.9f * (1f - num) * Mathf.InverseLerp(0.5f, 0f, num2) * num3, 0.5f);
                sLeaser.sprites[LightSprite].scale = Mathf.Lerp(60f, 120f, num) / 16f;
            }
            else
            {
                sLeaser.sprites[LightSprite].alpha = 0.9f * (1f - num) * Mathf.InverseLerp(0.5f, 0f, num2) * num3;
                sLeaser.sprites[LightSprite].scale = Mathf.Lerp(20f, 40f, num) / 16f;
            }
          
            sLeaser.sprites[LightSprite].color = Color.Lerp(TokenColor, color, 0.4f);
           
            sLeaser.sprites[LightSprite].isVisible = (!contract && num3 > 0f);
            for (int i = 0; i < 4; i++)
            {
                Vector2 vector2 = Vector2.Lerp(lines[i, 1], lines[i, 0], timeStacker);
                int num4 = (i == 3) ? 0 : (i + 1);
                Vector2 vector3 = Vector2.Lerp(lines[num4, 1], lines[num4, 0], timeStacker);
                float num5 = 1f - (1f - Mathf.Max(lines[i, 3].x, lines[num4, 3].x)) * (1f - num);
                num5 = Mathf.Pow(num5, 2f);
                num5 *= 1f - num2;
                if (Random.value < num5)
                {
                    vector3 = Vector2.Lerp(vector2, vector3, Random.value);
                    if (stalk != null)
                    {
                        vector2 = stalk.EyePos(timeStacker);
                    }
                    if (expandAroundPlayer != null && (Random.value < expand || contract))
                    {
                        vector2 = Vector2.Lerp(expandAroundPlayer.mainBodyChunk.lastPos, expandAroundPlayer.mainBodyChunk.pos, timeStacker);
                    }
                }
                sLeaser.sprites[LineSprite(i)].x = vector2.x - camPos.x;
                sLeaser.sprites[LineSprite(i)].y = vector2.y - camPos.y;
                sLeaser.sprites[LineSprite(i)].scaleY = Vector2.Distance(vector2, vector3);
                sLeaser.sprites[LineSprite(i)].rotation = Custom.AimFromOneVectorToAnother(vector2, vector3);
                sLeaser.sprites[LineSprite(i)].alpha = (1f - num5) * num3 * (underWaterMode ? 0.2f : 1f);
                sLeaser.sprites[LineSprite(i)].color = color;
                sLeaser.sprites[LineSprite(i)].isVisible = (num3 > 0f);
            }
            if (base.slatedForDeletetion || room != rCam.room)
            {
                sLeaser.CleanSpritesAndRemove();
            }
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            if (newContatiner == null)
                newContatiner = rCam.ReturnFContainer(layerName: "Water");

            foreach (var sprite in sLeaser.sprites)
                sprite.RemoveFromContainer();

            newContatiner.AddChild(sLeaser.sprites[GoldSprite]);
            
            for (int j = 0; j < GoldSprite; j++)
            {
              
                if (ModManager.MMF)
                {
                    for (int k = 0; k < 4; k++)
                    {
                        if (j == LineSprite(k))
                        {
                            newContatiner.AddChild(sLeaser.sprites[j]);
                            break;
                        }
                    }
                }

            }
            if (ModManager.MMF)
                for (int l = 0; l < 4; l++)
                    rCam.ReturnFContainer("GrabShaders").AddChild(sLeaser.sprites[LineSprite(l)]);
                
            
        }



        //TODO : TOKEN COLOR
        public Color TokenColor => Color.white;

        public Vector2 hoverPos;

        public Vector2 pos;

        public Vector2 lastPos;

        public Vector2 vel;

        public float sinCounter;

        public float sinCounter2;

        public Vector2[] trail;

        public float expand;

        private float lastExpand;

        public bool contract;

        public Vector2[,] lines;

        public bool underWaterMode;

        public Player expandAroundPlayer;

        public float glitch;

        private float lastGlitch;

        private float generalGlitch;

        public PlacedObject placedObj;

        public CustomWhiteStalk stalk;

        private bool poweredOn;

        public float power;

        private float lastPower;

        private StaticSoundLoop soundLoop;

        private StaticSoundLoop glitchLoop;

        public bool locked;

        private int lockdownCounter;
        

        public class CollectTokenData : PlacedObject.ResizableObjectData
        {
            
            public CollectTokenData(PlacedObject owner) : base(owner)
            {
                
            }

            public override void FromString(string s)
            {
                string[] array = Regex.Split(s, "~");
                handlePos.x = float.Parse(array[0], NumberStyles.Any, CultureInfo.InvariantCulture);
                handlePos.y = float.Parse(array[1], NumberStyles.Any, CultureInfo.InvariantCulture);
                panelPos.x = float.Parse(array[2], NumberStyles.Any, CultureInfo.InvariantCulture);
                panelPos.y = float.Parse(array[3], NumberStyles.Any, CultureInfo.InvariantCulture);
                chatlogId = array[4];
            }

            public override string ToString()
            {
          
             
                string text2 = $"{handlePos.x}~{handlePos.y}~{panelPos.x}~{panelPos.y}~{chatlogId}";
                text2 = SaveState.SetCustomData(this, text2);
                return SaveUtils.AppendUnrecognizedStringAttrs(text2, "~", unrecognizedAttributes);
            }

           

            public Vector2 panelPos;

            public string chatlogId = "";

        }

    }
}
